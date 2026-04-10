using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Events_GSS.Data.Models;
using Events_GSS.ViewModels;

using System;

namespace Events_GSS.Views;

/// <summary>
/// Represents the third step in the event creation process where users can select or create quests for the event.
/// </summary>
public sealed partial class CreateEventStep3View : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventStep3View"/> class.
    /// </summary>
    public CreateEventStep3View()
    {
        this.InitializeComponent();
        this.DataContext = this.ViewModel;
        this.Loaded += this.CreateEventStep3View_Loaded;
    }

    /// <summary>
    /// Gets or sets the view model for this view.
    /// </summary>
    public CreateEventViewModel ViewModel { get; set; } = null!;


    private void CreateEventStep3View_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.ViewModel != null)
        {
            this.ViewModel.CloseRequested += this.OnEventCreated;
        }
    }

    private async void OnEventCreated(Events_GSS.Data.Models.CreateEventDto? dto)
    {
        // Hide the main content
        this.MainContent.Visibility = Visibility.Collapsed;

        string details = this.ViewModel.EventCreationDetailsText;

        var dialog = new ContentDialog
        {
            Title = details,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private void QuestCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is Quest quest)
        {
            if (!this.ViewModel.SelectedQuests.Contains(quest))
            {
                this.ViewModel.SelectedQuests.Add(quest);
            }
        }
    }

    private void QuestCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is Quest quest)
        {
            if (this.ViewModel.SelectedQuests.Contains(quest))
            {
                this.ViewModel.SelectedQuests.Remove(quest);
            }
        }
    }

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

    private void RemoveQuest_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Quest quest)
        {
            if (this.ViewModel.SelectedQuests.Contains(quest))
            {
                this.ViewModel.SelectedQuests.Remove(quest);
            }
        }
    }
}
