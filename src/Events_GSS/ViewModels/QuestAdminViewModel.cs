// ViewModels/QuestViewModel.cs
using System.Collections.ObjectModel;
using System.Diagnostics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace Events_GSS.ViewModels;

public partial class QuestAdminViewModel : ObservableObject
{
    private readonly IQuestService _questService = App.Services.GetRequiredService<IQuestService>();
    private readonly Event _event;
    
    public QuestAdminViewModel(Event forEvent)
    {
        _event = forEvent;
        Quests = new ObservableCollection<Quest>();
        PresetQuests = new ObservableCollection<Quest>();

        _ = InitializeAsync();
    }

    public ObservableCollection<Quest> Quests { get; }
    public ObservableCollection<Quest> PresetQuests { get; }

    //aux
    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; } = true;
    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;

    [RelayCommand]
    private void ClearPrerequisite() => SelectedPrerequisiteQuest = null;

    //quest management
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

    // Computed — no [ObservableProperty], notified via IsLoading above
    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorVisibility))]
    public partial string? ErrorMessage { get; set; }

    // Computed — notified via ErrorMessage above
    public bool HasError => ErrorMessage is not null;
    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;

    // ─── Commands ─────────────────────────────────────────────────────────────

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

            var newId = await _questService.AddQuestAsync(_event, quest);
            quest.Id = newId;
            Quests.Add(quest);

            NewQuestName = string.Empty;
            NewQuestDescription = string.Empty;
            NewQuestDifficulty = 1;
            SelectedPrerequisiteQuest = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add quest: {ex.Message}";
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
        if (SelectedPresetQuest is null) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var newId = await _questService.AddQuestAsync(_event, SelectedPresetQuest);

            var added = new Quest
            {
                Id = newId,
                Name = SelectedPresetQuest.Name,
                Description = SelectedPresetQuest.Description,
                Difficulty = SelectedPresetQuest.Difficulty,
            };

            Quests.Add(added);
            SelectedPresetQuest = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add preset quest: {ex.Message}";
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
        if (SelectedQuest is null) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await _questService.DeleteQuestAsync(SelectedQuest);
            Quests.Remove(SelectedQuest);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete quest: {ex.Message}";
        }
        finally
        {
            SelectedQuest = null;
            IsLoading = false;
        }
    }

    private bool CanDeleteQuest()=>
        SelectedQuest!= null && !IsLoading;

    // ─── Internal ─────────────────────────────────────────────────────────────
    private async Task InitializeAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var presets = await _questService.GetPresetQuestsAsync();
            PresetQuests.Clear();
            foreach (var p in presets) PresetQuests.Add(p);

            var quests = await _questService.GetQuestsAsync(_event);
            Quests.Clear();
            foreach (var q in quests) Quests.Add(q);

            
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load quests: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}