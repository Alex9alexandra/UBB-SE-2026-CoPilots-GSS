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

    // Handles ViewModel changes by subscribing to the Announcements collection and updates the UI
    // to show or hide the empty state message based on whether the collection is empty
    private static void OnViewModelChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is AnnouncementControl control &&
            args.NewValue is AnnouncementViewModel viewModel)
        {
            viewModel.Announcements.CollectionChanged += OnAnnouncementsCollectionChanged;

            void OnAnnouncementsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                control.EmptyStateText.Visibility =
                    viewModel.Announcements.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }
        }
    }

    // Handles tapping on an announcement header and toggles its expanded state
    private void OnAnnouncementHeaderTapped(object sender, TappedRoutedEventArgs eventArgs)
    {
        if (sender is FrameworkElement frameworkElement &&
            frameworkElement.Tag is AnnouncementItemViewModel announcementItem &&
            ViewModel is not null)
        {
            ViewModel.ToggleExpandCommand.Execute(announcementItem);
        }
    }

    private void OnEmojiClicked(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button button ||
            button.Tag is not string selectedEmoji ||
            ViewModel is null)
        {
            return;
        }

        var announcementItem = FindAncestorDataContext<AnnouncementItemViewModel>(button);

        if (announcementItem is not null)
        {
            var reactionPayload = new AnnouncementReactionPayload(
                announcementItem,
                selectedEmoji);

            ViewModel.ToggleReactionCommand.Execute(reactionPayload);
        }
    }

    private void OnEditClicked(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is FrameworkElement frameworkElement &&
            frameworkElement.Tag is AnnouncementItemViewModel announcementItem &&
            ViewModel is not null)
        {
            ViewModel.StartEditCommand.Execute(announcementItem);
        }
    }

    private async void OnDeleteClicked(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is not FrameworkElement frameworkElement ||
            frameworkElement.Tag is not AnnouncementItemViewModel announcementItem ||
            ViewModel is null)
        {
            return;
        }

        var confirmationDialog = new ContentDialog
        {
            Title = "Delete announcement",
            Content = "Are you sure? This will permanently remove this announcement and all its reactions and read records.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var dialogResult = await confirmationDialog.ShowAsync();

        if (dialogResult == ContentDialogResult.Primary)
        {
            ViewModel.DeleteAnnouncementCommand.Execute(announcementItem);
        }
    }

    private void OnPinClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement frameworkElement
            && frameworkElement.Tag is AnnouncementItemViewModel announcementItem
            && ViewModel is not null)
        {
            ViewModel.PinAnnouncementCommand.Execute(announcementItem);
        }
    }

    private static T? FindAncestorDataContext<T>(DependencyObject startingElement) where T : class
    {
        var currentElement = startingElement;

        while (currentElement is not null)
        {
            if (currentElement is FrameworkElement frameworkElement &&
                frameworkElement.DataContext is T matchingDataContext)
            {
                return matchingDataContext;
            }

            currentElement = VisualTreeHelper.GetParent(currentElement);
        }

        return null;
    }

    private async void OnReadReceiptsClicked(object sender, RoutedEventArgs eventArguments)
    {
        if (sender is not FrameworkElement frameworkElement
            || frameworkElement.Tag is not AnnouncementItemViewModel announcementItem
            || ViewModel is null
            || !ViewModel.IsEventAdmin)
            return;

        // Load read receipts + all participants
        await ViewModel.LoadReadReceiptsCommand.ExecuteAsync(announcementItem);

        // Compute non-readers
        var nonReaders = await ViewModel.GetNonReadersAsync(announcementItem.Id);

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
