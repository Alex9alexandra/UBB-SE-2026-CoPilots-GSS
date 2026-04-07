using System;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.Storage.Pickers;

using Events_GSS.ViewModels;

namespace Events_GSS.Views;

public sealed partial class DiscussionControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(DiscussionViewModel),
            typeof(DiscussionControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public DiscussionViewModel? ViewModel
    {
        get => (DiscussionViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public DiscussionControl()
    {
        InitializeComponent();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DiscussionControl control && e.NewValue is DiscussionViewModel vm)
        {
            vm.Messages.CollectionChanged += (_, _) =>
            {
                control.UpdateEmptyState();
                control.ScrollToBottom();
            };
        }
    }

    // ── Mention Highlighting ─────────────────────────────────────────────────

    private void OnMessageTextLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not RichTextBlock rtb) return;
        if (rtb.DataContext is not DiscussionMessageItemViewModel item) return;

        rtb.Blocks.Clear();

        var paragraph = new Paragraph();

        foreach (var segment in item.MessageSegments)
        {
            var run = new Run { Text = segment.Text };

            if (segment.IsMention)
            {
                run.Foreground = new SolidColorBrush(Colors.DodgerBlue);
                run.FontWeight = FontWeights.SemiBold;
            }

            paragraph.Inlines.Add(run);
        }

        rtb.Blocks.Add(paragraph);
    }

    // ── Reply ────────────────────────────────────────────────────────────────

    private void OnReplyClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.Tag is DiscussionMessageItemViewModel item
            && ViewModel is not null)
        {
            ViewModel.SetReplyTargetCommand.Execute(item);
        }
    }

    private void OnReplyReferenceTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || ViewModel is null) return;

        // The Tag holds the original message's Id (int)
        if (fe.Tag is not int originalId) return;

        var originalItem = ViewModel.Messages.FirstOrDefault(m => m.Id == originalId);
        if (originalItem is not null)
        {
            MessagesListView.ScrollIntoView(originalItem, ScrollIntoViewAlignment.Leading);
        }
    }

    // ── Reactions ────────────────────────────────────────────────────────────

    private void OnEmojiClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string emoji || ViewModel is null)
            return;

        var item = FindAncestorDataContext<DiscussionMessageItemViewModel>(btn);
        if (item is not null)
        {
            ViewModel.ToggleReactionCommand.Execute(
                new DiscussionReactionPayload(item, emoji));
        }
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    private async void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe
            || fe.Tag is not DiscussionMessageItemViewModel item
            || ViewModel is null)
            return;

        var dialog = new ContentDialog
        {
            Title = "Delete message",
            Content = "Are you sure you want to delete this message? This cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.DeleteMessageCommand.Execute(item);
        }
    }

    // ── Mute (with custom duration) ──────────────────────────────────────────

    private async void OnMuteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe
            || fe.Tag is not DiscussionMessageItemViewModel item
            || ViewModel is null
            || !ViewModel.IsEventAdmin)
            return;

        var panel = new StackPanel { Spacing = 12 };

        var combo = new ComboBox
        {
            PlaceholderText = "Select duration",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "1 hour", "24 hours", "Custom", "Permanent" }
        };

        var customPanel = new StackPanel
        {
            Spacing = 8,
            Visibility = Visibility.Collapsed
        };

        var hoursBox = new NumberBox
        {
            Header = "Hours",
            Minimum = 0,
            Maximum = 720,
            Value = 0,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline
        };

        var minutesBox = new NumberBox
        {
            Header = "Minutes",
            Minimum = 0,
            Maximum = 59,
            Value = 30,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline
        };

        customPanel.Children.Add(hoursBox);
        customPanel.Children.Add(minutesBox);

        combo.SelectionChanged += (_, _) =>
        {
            customPanel.Visibility = combo.SelectedItem is string s && s == "Custom"
                ? Visibility.Visible
                : Visibility.Collapsed;
        };

        panel.Children.Add(combo);
        panel.Children.Add(customPanel);

        var dialog = new ContentDialog
        {
            Title = $"Mute {item.Author?.Name ?? "user"}",
            Content = panel,
            PrimaryButtonText = "Mute",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && combo.SelectedItem is string selected)
        {
            var expiry = ViewModel.CalculateMuteExpiry(selected, hoursBox.Value, minutesBox.Value);

            ViewModel.MuteUserCommand.Execute(
                new MutePayload(item.Author!.UserId, expiry));
        }
    }

    //  ── Unmute ─────────────────────────────────────────────────────

    private async void OnUnmuteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe
            || fe.Tag is not DiscussionMessageItemViewModel item
            || ViewModel is null
            || !ViewModel.IsEventAdmin
            || item.Author is null)
            return;

        var dialog = new ContentDialog
        {
            Title = $"Unmute {item.Author.Name}?",
            Content = $"This will lift any active mute for {item.Author.Name} in this event.",
            PrimaryButtonText = "Unmute",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.UnmuteUserCommand.Execute(item.Author.UserId);
        }
    }

    // ── Slow Mode Config ─────────────────────────────────────────────────────

    private async void OnConfigureSlowModeClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;

        var numberBox = new NumberBox
        {
            Header = "Seconds between messages per user",
            Minimum = 1,
            Maximum = 3600,
            Value = ViewModel.CurrentSlowModeSeconds ?? 30,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline
        };

        var dialog = new ContentDialog
        {
            Title = "Configure Slow Mode",
            Content = numberBox,
            PrimaryButtonText = "Enable",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            // Pass the raw double to the Command; let VM handle the casting/logic
            ViewModel.SetSlowModeCommand.Execute(numberBox.Value);
        }
    }

    private void OnDisableSlowModeClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.SetSlowModeCommand.Execute(null as int?);
    }

    // ── Media Attach ─────────────────────────────────────────────────────────

    private async void OnAttachMediaClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".gif");
        picker.FileTypeFilter.Add(".mp4");
        picker.FileTypeFilter.Add(".mov");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
            ((App)Application.Current).MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await ViewModel.HandleMediaFileAsync(file);
        }
    }

    private void OnClearMediaClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.MediaPath = null;
    }

    // ── Enter to Send ────────────────────────────────────────────────────────

    private void OnMessageInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter
            && ViewModel is not null
            && ViewModel.SendMessageCommand.CanExecute(null))
        {
            ViewModel.SendMessageCommand.Execute(null);
            e.Handled = true;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ScrollToBottom()
    {
        if (ViewModel is null || ViewModel.Messages.Count == 0) return;

        // Use Low priority so the ListView layout pass completes first
        DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () =>
            {
                if (ViewModel?.Messages.Count > 0)
                {
                    MessagesListView.ScrollIntoView(
                        ViewModel.Messages[^1],
                        ScrollIntoViewAlignment.Leading);
                }
            });
    }

    private void UpdateEmptyState()
    {
        EmptyStateText.Visibility =
            ViewModel?.Messages.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
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
}
