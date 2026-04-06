using System;
using System.Diagnostics;
using System.IO;

using Windows.Storage;
using Windows.Storage.Pickers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Events_GSS.ViewModels;

namespace Events_GSS.Views;

public sealed partial class SubmitProofDialog : ContentDialog
{
    public SubmitProofArgs? Result { get; private set; }
    private readonly QuestItemViewModel _questItem;
    private StorageFile? _selectedFile;

    public SubmitProofDialog(QuestItemViewModel questItem)
    {
        this.InitializeComponent();
        _questItem = questItem;
        QuestNameText.Text = questItem.Name;
        Title = $"Submit Proof";
    }

    private async void OnPickPhotoClicked(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        var app = (App)Application.Current;
        var window = app.MainWindow;

        if (window == null)
        {
            Debug.WriteLine("MainWindow is not set, check there.");
            return;
        }

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");

        StorageFile file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            // Check size (10 MB = 10 * 1024 * 1024 bytes)
            var properties = await file.GetBasicPropertiesAsync();
            if (properties.Size > 10 * 1024 * 1024)
            {
                ValidationText.Text = "Photo exceeds 10 MB limit.";
                ValidationText.Visibility = Visibility.Visible;
                return;
            }

            _selectedFile = file;
            FileNameText.Text = $"Selected: {file.Name}";

            // Show preview
            using var stream = await file.OpenAsync(FileAccessMode.Read);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            ImagePreview.Source = bitmap;
            ImagePreview.Visibility = Visibility.Visible;
            ValidationText.Visibility = Visibility.Collapsed;
        }
    }

    private void OnSubmitClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var text = DescriptionBox.Text?.Trim();
        bool hasPhoto = _selectedFile != null;
        bool hasText = !string.IsNullOrWhiteSpace(text);

        // Validation: Mandatory photo if no text, and vice versa
        if (!hasPhoto && !hasText)
        {
            args.Cancel = true;
            ValidationText.Text = "Please provide a photo or a description.";
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        // Pass the StorageFile instead of a URL string
        Result = new SubmitProofArgs(_questItem, _selectedFile?.Path, text);
    }
}