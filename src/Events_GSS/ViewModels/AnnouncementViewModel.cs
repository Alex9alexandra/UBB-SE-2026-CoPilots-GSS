using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.announcementServices;
using Events_GSS.Data.Services.Interfaces;

using Microsoft.UI.Xaml;

namespace Events_GSS.ViewModels;

public record AnnouncementReactionPayload(AnnouncementItemViewModel Announcement, string Emoji);

public partial class AnnouncementViewModel : ObservableObject
{
    private readonly IAnnouncementService _announcementService;
    private readonly Event _currentEvent;
    private readonly int _currentUserId;

    public IAnnouncementService GetAnnouncementService() => _announcementService;
    public int GetEventId() => _currentEvent.EventId;

    public AnnouncementViewModel(
        Event forEvent,
        IAnnouncementService service,
        int currentUserId,
        bool isAdmin)
    {
        _currentEvent = forEvent;
        _announcementService = service;
        _currentUserId = currentUserId;
        IsEventAdmin = isAdmin;

        Announcements = new ObservableCollection<AnnouncementItemViewModel>();
        ReadReceiptUsers = new ObservableCollection<AnnouncementReadReceipt>();
    }

    public ObservableCollection<AnnouncementItemViewModel> Announcements { get; }
    public ObservableCollection<AnnouncementReadReceipt> ReadReceiptUsers { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadReceiptSummary))]
    private int _readReceiptReadCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadReceiptSummary))]
    private int _readReceiptTotalCount;

    [ObservableProperty]
    private bool _isReadReceiptsLoading;

    private const double percentageMultiplier = 100.0;

    // Returns percentage of participants who have read the announcement
    public string ReadReceiptSummary
    {
        get
        {
            if (ReadReceiptTotalCount == 0)
            {
                return "No participants";
            }

            var percentageOfUsersWhoReadAnnouncement = (int)Math.Round(
                percentageMultiplier * ReadReceiptReadCount / ReadReceiptTotalCount);

            return $"{ReadReceiptReadCount} / {ReadReceiptTotalCount} read ({percentageOfUsersWhoReadAnnouncement}%)";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEventAdmin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorVisibility))]
    private string? _errorMessage;

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private string _newMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(CreateButtonText))]
    private AnnouncementItemViewModel? _editingAnnouncement;

    public bool IsNotLoading => !IsLoading;
    public bool HasError => ErrorMessage is not null;
    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;
    public bool IsEditing => EditingAnnouncement is not null;
    public string CreateButtonText => IsEditing ? "Save Edit" : "Post";

    public async Task InitializeAsync()
    {
        await RunGuardedAsync(LoadAnnouncementsAsync);
    }

    [RelayCommand]
    private async Task LoadAnnouncementsAsync()
    {
        var announcementsList = await _announcementService.GetAnnouncementsAsync(
            _currentEvent.EventId, _currentUserId);

        Announcements.Clear();
        foreach (var announcement in announcementsList)
        {
            Announcements.Add(new AnnouncementItemViewModel(announcement, _currentUserId, IsEventAdmin));
        }

        UpdateUnreadCount();
    }

    [RelayCommand]

    // Creates or updates an announcement
    private async Task SubmitAnnouncementAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMessage)) return;

        if (IsEditing)
        {
            await RunGuardedAsync(async () =>
            {
                await _announcementService.UpdateAnnouncementAsync(
                    EditingAnnouncement!.Id,
                    NewMessage.Trim(),
                    _currentUserId,
                    _currentEvent.EventId);

                EditingAnnouncement = null;
                NewMessage = string.Empty;
                await LoadAnnouncementsAsync();
            });
        }
        else
        {
            await RunGuardedAsync(async () =>
            {
                await _announcementService.CreateAnnouncementAsync(
                    NewMessage.Trim(),
                    _currentEvent.EventId,
                    _currentUserId);

                NewMessage = string.Empty;
                await LoadAnnouncementsAsync();
            });
        }
    }

    [RelayCommand]

    // When user clicks on edit button, this gets the announcement they're editing and pre-fills the box with the announcement's text
    private void StartEdit(AnnouncementItemViewModel? item)
    {
        if (item is null) return;
        EditingAnnouncement = item;
        NewMessage = item.Message;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingAnnouncement = null;
        NewMessage = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteAnnouncementAsync(AnnouncementItemViewModel? item)
    {
        if (item is null) return;

        await RunGuardedAsync(async () =>
        {
            await _announcementService.DeleteAnnouncementAsync(
                item.Id, _currentUserId, _currentEvent.EventId);

            Announcements.Remove(item);
            UpdateUnreadCount();
        });
    }

    [RelayCommand]
    private async Task PinAnnouncementAsync(AnnouncementItemViewModel? item)
    {
        if (item is null) return;

        await RunGuardedAsync(async () =>
        {
            await _announcementService.PinAnnouncementAsync(
                item.Id, _currentEvent.EventId, _currentUserId);

            await LoadAnnouncementsAsync();
        });
    }

    [RelayCommand]
    private async Task ToggleExpandAsync(AnnouncementItemViewModel? item)
    {
        if (item is null) return;

        item.IsExpanded = !item.IsExpanded;

        if (item.IsExpanded)
        {
            var wasMarked = await _announcementService.MarkAsReadIfNeededAsync(
                item.Id,
                _currentUserId,
                item.IsRead);

            if (wasMarked)
            {
                item.IsRead = true;
                UpdateUnreadCount();
            }
        }
    }

    [RelayCommand]
    // Returns a list with all the people who have read the current announcement (only for admins)
    private async Task LoadReadReceiptsAsync(AnnouncementItemViewModel? item)
    {
        if (item is null || !IsEventAdmin) return;

        IsReadReceiptsLoading = true;
        try
        {
            var (readers, total) = await _announcementService.GetReadReceiptsAsync(
                item.Id, _currentEvent.EventId, _currentUserId);

            ReadReceiptUsers.Clear();
            foreach (var reader in readers)
                ReadReceiptUsers.Add(reader);

            ReadReceiptReadCount = readers.Count;
            ReadReceiptTotalCount = total;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Read receipts failed: {exception.Message}");
        }
        finally
        {
            IsReadReceiptsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleReactionAsync(AnnouncementReactionPayload? payload)
    {
        if (payload is null) return;

        await RunGuardedAsync(async () =>
        {
            await _announcementService.ToggleReactionAsync(payload.Announcement.Id, _currentUserId, payload.Emoji);

            await LoadAnnouncementsAsync();
        });
    }

    private void UpdateUnreadCount()
    {
        UnreadCount = Announcements.Count(a => !a.IsRead);
    }

    // Wraps an async operation to manage loading state and handle exceptions consistently across the ViewModel
    private async Task RunGuardedAsync(Func<Task> action)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "You don't have permission for this action.";
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine("========== FULL EXCEPTION ==========");
            System.Diagnostics.Debug.WriteLine(exception.ToString());
            System.Diagnostics.Debug.WriteLine("=====================================");
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnIsLoadingChanged(bool value) => NotifyCommandsChanged();
    partial void OnNewMessageChanged(string value) => NotifyCommandsChanged();

    private void NotifyCommandsChanged()
    {
        SubmitAnnouncementCommand.NotifyCanExecuteChanged();
    }
}
