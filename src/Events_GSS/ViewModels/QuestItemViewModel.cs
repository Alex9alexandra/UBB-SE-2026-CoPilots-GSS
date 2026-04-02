using CommunityToolkit.Mvvm.ComponentModel;

using Events_GSS.Data.Models;
namespace Events_GSS.ViewModels;

public partial class QuestItemViewModel(QuestMemory questMemory, bool isLocked, bool isAttending) : ObservableObject
{
    public QuestMemory QuestMemory { get; } = questMemory;
    public Quest Quest => QuestMemory.ForQuest;
    public bool IsLocked { get; } = isLocked;
    private bool _isAttending = isAttending;

    public string Name => IsLocked ? "???" : Quest.Name;
    public string Description => IsLocked ? "Complete the prerequisite to unlock." : Quest.Description;
    public string DifficultyStars => new string('★', Quest.Difficulty) + new string('☆', 5 - Quest.Difficulty);
    public string PrerequisiteHint => Quest.PrerequisiteQuest?.Name ?? "None";
    public bool HasPrerequisite => Quest.PrerequisiteQuest is not null;

    public QuestMemoryStatus Status => QuestMemory.ProofStatus;
    public string StatusLabel => Status switch
    {
        QuestMemoryStatus.Approved => "Completed",
        QuestMemoryStatus.Submitted => "Pending",
        QuestMemoryStatus.Rejected => "Rejected",
        QuestMemoryStatus.Incomplete => IsLocked ? "Locked" : "Available",
        _ => ""
    };

    public bool CanSubmit => _isAttending && !IsLocked && Status is QuestMemoryStatus.Incomplete or QuestMemoryStatus.Rejected;
    public bool CanDelete => _isAttending && Status is QuestMemoryStatus.Submitted or QuestMemoryStatus.Approved or QuestMemoryStatus.Rejected;
}