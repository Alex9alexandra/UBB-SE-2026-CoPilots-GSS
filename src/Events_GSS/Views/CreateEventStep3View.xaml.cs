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

        string details;
        if (dto == null)
        {
            details = "Event creation cancelled.";
        }
        else
        {
            string quests = dto.SelectedQuests.Count == 0
                ? "None"
                : string.Join(", ", dto.SelectedQuests.Select(q => q.Name));
            string category = dto.Category?.Title ?? "None";
            string createdBy = dto.Admin != null 
                ? $"{dto.Admin.Name} (ID: {dto.Admin.UserId})" 
                : "Unknown";

            details =
                $"Event created successfully!\n\n" +
                $"Name: {dto.Name}\n" +
                $"Location: {dto.Location}\n" +
                $"Start: {dto.StartDateTime}\n" +
                $"End: {dto.EndDateTime}\n" +
                $"Public: {dto.IsPublic}\n" +
                $"Description: {dto.Description}\n" +
                $"Maximum People: {(dto.MaximumPeople.HasValue ? dto.MaximumPeople.Value.ToString() : "No limit")}\n" +
                $"Banner Path: {dto.EventBannerPath ?? "None"}\n" +
                $"Category: {category}\n" +
                $"Selected Quests: {quests}\n" +
                $"Created By: {createdBy}";
        }

        var dialog = new ContentDialog
        {
            Title = "Event Created",
            Content = new ScrollViewer { Content = new TextBlock { Text = details, TextWrapping = TextWrapping.Wrap } },
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
