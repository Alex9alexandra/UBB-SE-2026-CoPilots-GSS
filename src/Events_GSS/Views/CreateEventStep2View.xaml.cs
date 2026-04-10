using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Events_GSS.ViewModels;
using WinRT.Interop;
using System.Linq;

namespace Events_GSS.Views;

/// <summary>
/// Represents the second step view for creating an event.
/// </summary>
public sealed partial class CreateEventStep2View : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventStep2View"/> class.
    /// </summary>
    public CreateEventStep2View()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Gets the handle of the currently active window.
    /// </summary>
    /// <returns>The handle of the active window.</returns>
    [DllImport("user32.dll")]
    private static extern nint GetActiveWindow();

    /// <summary>
    /// Gets or sets the view model for this view.
    /// </summary>
    public CreateEventViewModel ViewModel { get; set; } = null!;


    /// <summary>
    /// Handles the BeforeTextChanging event to allow only digit input for maximum attendees.
    /// </summary>
    /// <param name="sender">The TextBox that triggered the event.</param>
    /// <param name="args">Event arguments containing the new text.</param>
    private void MaximumAttendees_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        // Only allow digits
        if (!args.NewText.All(char.IsDigit))
        {
            args.Cancel = true;
        }
    }

    /// <summary>
    /// Handles the browse image button click to allow selecting an event banner image.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="e">Event arguments.</param>
    private async void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".bmp");

        var hwnd = GetActiveWindow();
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            this.ViewModel.EventBannerPath = file.Path;

            var bitmapImage = new BitmapImage(new Uri(file.Path));
            this.BannerPreview.Source = bitmapImage;
        }
    }

    /// <summary>
    /// Handles the cancel button click to confirm cancellation of event creation.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="e">Event arguments.</param>
    private async void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Cancel Event Creation",
            Content = "Are you sure you want to cancel? All changes will be lost.",
            PrimaryButtonText = "Yes, cancel",
            CloseButtonText = "No, go back",
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            this.ViewModel.CancelCommand.Execute(null);
        }
    }
}
