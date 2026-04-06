using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Events_GSS;
using Events_GSS.Data.Models;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;


namespace Events_GSS.ViewModels;

public enum QuestFilter { All, Submitted, Completed, Incomplete }

public partial class QuestUserViewModel : ObservableObject
{
    private readonly IQuestApprovalService _questService =App.Services.GetRequiredService<IQuestApprovalService>();
    private readonly IUserService _userService = App.Services.GetRequiredService<IUserService>();
    private readonly Event _currentEvent;
    private bool _isAttending;

    private List<QuestItemViewModel> _allQuests = [];
    public ObservableCollection<QuestItemViewModel> Quests { get; } = [];

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string StatusText { get; set; } = "";
    [ObservableProperty] public partial QuestItemViewModel? SelectedQuest { get; set; }

    [ObservableProperty]
    public partial int SelectedFilterIndex { get; set; } = 0;
    partial void OnSelectedFilterIndexChanged(int value) => ApplyFilter(value switch
    {
        1 => QuestFilter.Submitted,
        2 => QuestFilter.Completed,
        3 => QuestFilter.Incomplete,
        _ => QuestFilter.All
    });

    public QuestUserViewModel(Event currentEvent)
    {
        _currentEvent = currentEvent;
        InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _isAttending = await _userService.IsAttending(_currentEvent);
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
            var result = await _questService.GetQuestsWithStatus(_currentEvent, _userService.GetCurrentUser());
            var approvedIds = result
                .Where(qm => qm.ProofStatus == QuestMemoryStatus.Approved)
                .Select(qm => qm.ForQuest.Id)
                .ToHashSet();
            _allQuests = result.Select(qm =>
                new QuestItemViewModel(qm, qm.ForQuest.PrerequisiteQuest is { } p && !approvedIds.Contains(p.Id),_isAttending)
            ).ToList();
            ApplyFilter(QuestFilter.All);
            StatusText = $"{result.Count} quest(s) loaded.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
            StatusText = "Failed to load quests.";
            System.Diagnostics.Debug.WriteLine($"LOAD ERROR: {ex}");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task SubmitProofAsync(SubmitProofArgs args)
    {
        try
        {
            var proof = new Memory(args.PhotoPath, args.Text, DateTime.UtcNow)
            {
                Event = _currentEvent,
                Author = _userService.GetCurrentUser()
            };
            await _questService.SubmitProofAsync(args.Quest.Quest, proof);
            await LoadQuestsAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; HasError = true; }
    }

    [RelayCommand]
    public async Task DeleteSubmissionAsync(QuestItemViewModel item)
    {
        try
        {
            await _questService.DeleteSubmissionAsync(item.QuestMemory, _userService.GetCurrentUser());
            await LoadQuestsAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; HasError = true; }
    }

    private void ApplyFilter(QuestFilter filter)
    {
        var filtered = filter switch
        {
            QuestFilter.Submitted => _allQuests.Where(q => q.Status == QuestMemoryStatus.Submitted),
            QuestFilter.Completed => _allQuests.Where(q => q.Status == QuestMemoryStatus.Approved),
            QuestFilter.Incomplete => _allQuests.Where(q => q.Status == QuestMemoryStatus.Incomplete),
            _ => _allQuests.AsEnumerable()
        };
        Quests.Clear();
        foreach (var q in filtered) Quests.Add(q);
    }
}

public record SubmitProofArgs(QuestItemViewModel Quest, string? PhotoPath, string? Text);