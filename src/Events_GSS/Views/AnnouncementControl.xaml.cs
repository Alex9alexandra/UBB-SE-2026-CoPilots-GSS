using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Events_GSS.ViewModels;
using Events_GSS.Data.Models;

namespace Events_GSS.Views;

public sealed partial class AnnouncementControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(AnnouncementViewModel),
            typeof(AnnouncementControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public AnnouncementViewModel? ViewModel
    {
        get => (AnnouncementViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public AnnouncementControl()
    {
        InitializeComponent();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnnouncementControl control && e.NewValue is AnnouncementViewModel vm)
        {
            vm.Announcements.CollectionChanged += (_, _) =>
            {
                control.EmptyStateText.Visibility =
                    vm.Announcements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            };
        }
    }

    private void OnAnnouncementHeaderTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.Tag is AnnouncementItemViewModel item
            && ViewModel is not null)
        {
            ViewModel.ToggleExpandCommand.Execute(item);
        }
    }

    private void OnEmojiClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string emoji || ViewModel is null)
            return;

        var item = FindAncestorDataContext<AnnouncementItemViewModel>(btn);
        if (item is not null)
        {
            ViewModel.ToggleReactionCommand.Execute(
                new AnnouncementReactionPayload(item, emoji));
        }
    }

    private void OnEditClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.Tag is AnnouncementItemViewModel item
            && ViewModel is not null)
        {
            ViewModel.StartEditCommand.Execute(item);
        }
    }

    private async void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe
            || fe.Tag is not AnnouncementItemViewModel item
            || ViewModel is null)
            return;

        var dialog = new ContentDialog
        {
            Title = "Delete announcement",
            Content = "Are you sure? This will permanently remove this announcement and all its reactions and read records.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.DeleteAnnouncementCommand.Execute(item);
        }
    }

    private void OnPinClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.Tag is AnnouncementItemViewModel item
            && ViewModel is not null)
        {
            ViewModel.PinAnnouncementCommand.Execute(item);
        }
    }

    private static T? FindAncestorDataContext<T>(DependencyObject start) where T : class
    {
        var current = start;
        while (current is not null)
        {
            if (current is FrameworkElement fe && fe.DataContext is T found)
                return found;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private async void OnReadReceiptsClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe
            || fe.Tag is not AnnouncementItemViewModel item
            || ViewModel is null
            || !ViewModel.IsEventAdmin)
            return;

        // Load read receipts + all participants
        await ViewModel.LoadReadReceiptsCommand.ExecuteAsync(item);

        List<User> allParticipants;
        try
        {
            var service = ViewModel.GetAnnouncementService();
            allParticipants = await service.GetAllParticipantsAsync(ViewModel.GetEventId());
        }
        catch
        {
            allParticipants = new List<User>();
        }

        // Compute non-readers
        var readerIds = new HashSet<int>(
            ViewModel.ReadReceiptUsers.Select(r => r.User.UserId));
        var nonReaders = allParticipants
            .Where(p => !readerIds.Contains(p.UserId))
            .ToList();

        // Build dialog
        var panel = new StackPanel { Spacing = 8 };

        // Summary
        panel.Children.Add(new TextBlock
        {
            Text = ViewModel.ReadReceiptSummary,
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
        });

        // ── Readers section ──
        panel.Children.Add(new TextBlock
        {
            Text = $"Read ({ViewModel.ReadReceiptUsers.Count}):",
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            FontSize = 13,
            Margin = new Thickness(0, 12, 0, 4)
        });

        if (ViewModel.ReadReceiptUsers.Count > 0)
        {
            foreach (var receipt in ViewModel.ReadReceiptUsers)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                row.Children.Add(new FontIcon
                {
                    Glyph = "\uE73E", // Checkmark
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green),
                    VerticalAlignment = VerticalAlignment.Center
                });
                row.Children.Add(new TextBlock
                {
                    Text = receipt.User.Name,
                    Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
                });
                row.Children.Add(new TextBlock
                {
                    Text = receipt.ReadAt.ToString("MMM dd, HH:mm"),
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });
                panel.Children.Add(row);
            }
        }
        else
        {
            panel.Children.Add(new TextBlock
            {
                Text = "No one has read this announcement yet.",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
        }

        // ── Non-readers section ──
        panel.Children.Add(new TextBlock
        {
            Text = $"Not yet read ({nonReaders.Count}):",
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            FontSize = 13,
            Margin = new Thickness(0, 12, 0, 4)
        });

        if (nonReaders.Count > 0)
        {
            foreach (var user in nonReaders)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                row.Children.Add(new FontIcon
                {
                    Glyph = "\uE711", // X mark
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center
                });
                row.Children.Add(new TextBlock
                {
                    Text = user.Name,
                    Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                });
                panel.Children.Add(row);
            }
        }
        else
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Everyone has read this announcement!",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green)
            });
        }

        var dialog = new ContentDialog
        {
            Title = "Read Receipts",
            Content = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 400
            },
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
