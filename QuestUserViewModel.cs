using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.ViewModelCore;
using Events_GSS.Services.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace Events_GSS.ViewModels;

public enum QuestFilter
{
    All,
    Submitted,
    Completed,
    Incomplete
}

public partial class QuestUserViewModel : ObservableObject
{
    private readonly IQuestApprovalService questApprovalService;
    private readonly IUserService userService;
    private readonly Event currentEvent;
    private readonly QuestUserCore core;

    public QuestUserViewModel(
        Event currentEvent,
        IQuestApprovalService questApprovalService,
        IUserService userService)
    {
        this.currentEvent = currentEvent;
        this.questApprovalService = questApprovalService;
        this.userService = userService;

        this.core = new QuestUserCore(questApprovalService);
    }

    public QuestUserViewModel(Event currentEvent)
        : this(currentEvent,
               App.Services.GetRequiredService<IQuestApprovalService>(),
               App.Services.GetRequiredService<IUserService>())
    {
        _ = InitializeAsync();
    }

    private bool isAttending;
    private List<QuestItemViewModel> allQuests = new List<QuestItemViewModel>();

    public ObservableCollection<QuestItemViewModel> Quests { get; } = new ObservableCollection<QuestItemViewModel>();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial QuestItemViewModel? SelectedQuest { get; set; }

    [ObservableProperty]
    public partial int SelectedFilterIndex { get; set; } = 0;

    partial void OnSelectedFilterIndexChanged(int value)
    {
        QuestFilter filter = value switch
        {
            1 => QuestFilter.Submitted,
            2 => QuestFilter.Completed,
            3 => QuestFilter.Incomplete,
            _ => QuestFilter.All
        };
        ApplyFilter(filter);
    }

    private async Task InitializeAsync()
    {
        isAttending = await userService.IsAttending(currentEvent);
        await LoadQuestsAsync();
    }

    [RelayCommand]
    public async Task LoadQuestsAsync()
    {
        IsLoading = true;
        HasError = false;
        StatusText = "Loading...";
        Quests.Clear();

        try
        {
            var questResults = await core.GetQuestsAsync(currentEvent, userService.GetCurrentUser());

            var approvedQuestIds = questResults
                .Where(questMemory => questMemory.ProofStatus == QuestMemoryStatus.Approved)
                .Select(questMemory => questMemory.ForQuest.Id)
                .ToHashSet();

            allQuests = questResults.Select(questMemory =>
                new QuestItemViewModel(
                    questMemory,
                    questMemory.ForQuest.PrerequisiteQuest is Quest prerequisite && !approvedQuestIds.Contains(prerequisite.Id),
                    isAttending))
            .ToList();

            ApplyFilter(QuestFilter.All);
            StatusText = $"{questResults.Count} quest(s) loaded.";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            HasError = true;
            StatusText = "Failed to load quests.";
            System.Diagnostics.Debug.WriteLine($"LOAD ERROR: {exception}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SubmitProofAsync(SubmitProofArgs args)
    {
        try
        {
            var proof = new Memory(args.photoPath, args.text, DateTime.UtcNow)
            {
                Event = currentEvent,
                Author = userService.GetCurrentUser()
            };
            await questApprovalService.SubmitProofAsync(args.quest.Quest, proof);
            await LoadQuestsAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            HasError = true;
        }
    }

    [RelayCommand]
    public async Task DeleteSubmissionAsync(QuestItemViewModel item)
    {
        try
        {
            await questApprovalService.DeleteSubmissionAsync(item.QuestMemory, userService.GetCurrentUser());
            await LoadQuestsAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            HasError = true;
        }
    }

    private void ApplyFilter(QuestFilter filter)
    {
        var filteredQuests = filter switch
        {
            QuestFilter.Submitted => allQuests.Where(questItem => questItem.Status == QuestMemoryStatus.Submitted),
            QuestFilter.Completed => allQuests.Where(questItem => questItem.Status == QuestMemoryStatus.Approved),
            QuestFilter.Incomplete => allQuests.Where(questItem => questItem.Status == QuestMemoryStatus.Incomplete),
            _ => allQuests.AsEnumerable()
        };

        Quests.Clear();
        foreach (var questItem in filteredQuests)
        {
            Quests.Add(questItem);
        }
    }
}

public record SubmitProofArgs(QuestItemViewModel quest, string? photoPath, string? text);