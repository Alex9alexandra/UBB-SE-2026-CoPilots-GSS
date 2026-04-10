// ViewModels/QuestViewModel.cs
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace Events_GSS.ViewModels;

public partial class QuestAdminViewModel : ObservableObject
{
    private readonly IQuestService questService = App.Services.GetRequiredService<IQuestService>();
    private readonly Event currentEvent;

    public QuestAdminViewModel(Event forEvent)
    {
        currentEvent = forEvent;
        Quests = new ObservableCollection<Quest>();
        PresetQuests = new ObservableCollection<Quest>();

        _ = InitializeAsync();
    }

    public ObservableCollection<Quest> Quests { get; }
    public ObservableCollection<Quest> PresetQuests { get; }

    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; } = true;

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;

    [RelayCommand]
    private void ClearPrerequisite() => SelectedPrerequisiteQuest = null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    public partial string NewQuestName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    public partial string NewQuestDescription { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    public partial int NewQuestDifficulty { get; set; } = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPresetQuestCommand))]
    public partial Quest? SelectedPresetQuest { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteQuestCommand))]
    public partial Quest? SelectedQuest { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrerequisiteSelected))]
    public partial Quest? SelectedPrerequisiteQuest { get; set; }

    public bool HasPrerequisiteSelected => SelectedPrerequisiteQuest is not null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddPresetQuestCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteQuestCommand))]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorVisibility))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => ErrorMessage is not null;
    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;

    [RelayCommand(CanExecute = nameof(CanAddCustomQuest))]
    private async Task AddCustomQuestAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var quest = new Quest
            {
                Name = NewQuestName.Trim(),
                Description = NewQuestDescription.Trim(),
                Difficulty = NewQuestDifficulty,
                PrerequisiteQuest = SelectedPrerequisiteQuest
            };

            var newId = await questService.AddQuestAsync(currentEvent, quest);
            quest.Id = newId;
            Quests.Add(quest);

            NewQuestName = string.Empty;
            NewQuestDescription = string.Empty;
            NewQuestDifficulty = 1;
            SelectedPrerequisiteQuest = null;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Failed to add quest: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanAddCustomQuest() =>
        !string.IsNullOrWhiteSpace(NewQuestName) &&
        !string.IsNullOrWhiteSpace(NewQuestDescription) &&
        NewQuestDifficulty is >= 1 and <= 5 &&
        !IsLoading;

    [RelayCommand(CanExecute = nameof(CanAddPresetQuest))]
    private async Task AddPresetQuestAsync()
    {
        if (SelectedPresetQuest is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var newId = await questService.AddQuestAsync(currentEvent, SelectedPresetQuest);

            var addedQuest = new Quest
            {
                Id = newId,
                Name = SelectedPresetQuest.Name,
                Description = SelectedPresetQuest.Description,
                Difficulty = SelectedPresetQuest.Difficulty,
            };

            Quests.Add(addedQuest);
            SelectedPresetQuest = null;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Failed to add preset quest: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanAddPresetQuest() =>
        SelectedPresetQuest is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanDeleteQuest))]
    private async Task DeleteQuestAsync()
    {
        if (SelectedQuest is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await questService.DeleteQuestAsync(SelectedQuest);
            Quests.Remove(SelectedQuest);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Failed to delete quest: {exception.Message}";
        }
        finally
        {
            SelectedQuest = null;
            IsLoading = false;
        }
    }

    private bool CanDeleteQuest() =>
        SelectedQuest != null && !IsLoading;

    private async Task InitializeAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var presetQuests = await questService.GetPresetQuestsAsync();
            PresetQuests.Clear();
            foreach (var presetQuest in presetQuests)
            {
                PresetQuests.Add(presetQuest);
            }

            var eventQuests = await questService.GetQuestsAsync(currentEvent);
            Quests.Clear();
            foreach (var existingQuest in eventQuests)
            {
                Quests.Add(existingQuest);
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Failed to load quests: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}