using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Events_GSS.Data.Models;
using Events_GSS.ViewModels;

namespace Events_GSS.Views;

public sealed partial class CreateEventStep3View : UserControl
{
    public CreateEventViewModel ViewModel { get; set; } = null!;

    public CreateEventStep3View()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;
        this.Loaded += CreateEventStep3View_Loaded;
    }

    private void CreateEventStep3View_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.CloseRequested += OnEventCreated;
        }
    }

    private async void OnEventCreated(Events_GSS.Data.Models.CreateEventDto? dto)
    {
        // Hide the main content
        MainContent.Visibility = Visibility.Collapsed;

        string details = this.ViewModel.EventCreationDetailsText;

        var dialog = new ContentDialog
        {
            Title = details,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void QuestCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is Quest quest)
        {
            if (!ViewModel.SelectedQuests.Contains(quest))
                ViewModel.SelectedQuests.Add(quest);
        }
    }

    private void QuestCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is Quest quest)
        {
            if (ViewModel.SelectedQuests.Contains(quest))
                ViewModel.SelectedQuests.Remove(quest);
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
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.CancelCommand.Execute(null);
        }
    }

    private void RemoveQuest_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Quest quest)
        {
            if (ViewModel.SelectedQuests.Contains(quest))
                ViewModel.SelectedQuests.Remove(quest);
        }
    }
}
