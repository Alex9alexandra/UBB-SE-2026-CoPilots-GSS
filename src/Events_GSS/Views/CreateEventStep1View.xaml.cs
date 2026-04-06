using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Events_GSS.ViewModels;

namespace Events_GSS.Views;

public sealed partial class CreateEventStep1View : UserControl
{
    public CreateEventViewModel ViewModel { get; set; } = null!;

    public CreateEventStep1View()
    {
        this.InitializeComponent();
    }

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

    private void AttendeesTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        // Only allow digits
        if (!args.NewText.All(char.IsDigit))
        {
            args.Cancel = true;
        }
    }
}
