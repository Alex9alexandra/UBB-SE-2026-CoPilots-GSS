using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Events_GSS.ViewModels;

using System;
using System.Linq;

namespace Events_GSS.Views;

/// <summary>
/// User control for the first step of event creation.
/// </summary>
public sealed partial class CreateEventStep1View : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventStep1View"/> class.
    /// </summary>
    public CreateEventStep1View()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the view model for the create event step 1 view.
    /// </summary>
    public CreateEventViewModel ViewModel { get; set; } = null!;

    /// <summary>
    /// Handles the cancel button click event and prompts the user for confirmation.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="e">The event arguments.</param>
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

    /// <summary>
    /// Handles text changes in the attendees text box to allow only numeric input.
    /// </summary>
    /// <param name="sender">The text box that triggered the event.</param>
    /// <param name="args">The event arguments containing the new text value.</param>
    private void AttendeesTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        // Only allow digits
        if (!args.NewText.All(char.IsDigit))
        {
            args.Cancel = true;
        }
    }
}
