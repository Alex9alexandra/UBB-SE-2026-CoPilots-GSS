
using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Events_GSS.ViewModels;
using WinRT.Interop;

namespace Events_GSS.Views;

public sealed partial class CreateEventStep2View : UserControl
{
    public CreateEventViewModel ViewModel { get; set; } = null!;

    public CreateEventStep2View()
    {
        this.InitializeComponent();
    }

    private void MaximumAttendees_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        // Only allow digits
        if (!args.NewText.All(char.IsDigit))
        {
            args.Cancel = true;
        }
    }

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
            ViewModel.EventBannerPath = file.Path;

            var bitmapImage = new BitmapImage(new Uri(file.Path));
            BannerPreview.Source = bitmapImage;
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetActiveWindow();

    private async void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Cancel Event Creation",
            Content = "Are you sure you want to cancel? All changes will be lost.",
            PrimaryButtonText = "Yes, cancel",
            CloseButtonText = "No, go back",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.CancelCommand.Execute(null);
        }
    }
}
