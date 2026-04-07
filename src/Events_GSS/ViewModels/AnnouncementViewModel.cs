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
    private readonly IAnnouncementService _service;
    private readonly Event _event;
    private readonly int _currentUserId;

    public IAnnouncementService GetAnnouncementService() => _service;
    public int GetEventId() => _event.EventId;

    public AnnouncementViewModel(
        Event forEvent,
        IAnnouncementService service,
        int currentUserId,
        bool isAdmin)
    {
        _event = forEvent;
        _service = service;
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

    public string ReadReceiptSummary
    {
        get
        {
            if (ReadReceiptTotalCount == 0) return "No participants";
            int pct = (int)Math.Round(100.0 * ReadReceiptReadCount / ReadReceiptTotalCount);
            return $"{ReadReceiptReadCount} / {ReadReceiptTotalCount} read ({pct}%)";
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
        var list = await _service.GetAnnouncementsAsync(
            _event.EventId, _currentUserId);

        Announcements.Clear();
        foreach (var a in list)
        {
            Announcements.Add(new AnnouncementItemViewModel(a, _currentUserId, IsEventAdmin));
        }

        UpdateUnreadCount();
    }

    [RelayCommand]
    private async Task SubmitAnnouncementAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMessage)) return;

        if (IsEditing)
        {
            await RunGuardedAsync(async () =>
            {
                await _service.UpdateAnnouncementAsync(
                    EditingAnnouncement!.Id,
                    NewMessage.Trim(),
                    _currentUserId,
                    _event.EventId);

                EditingAnnouncement = null;
                NewMessage = string.Empty;
                await LoadAnnouncementsAsync();
            });
        }
        else
        {
            await RunGuardedAsync(async () =>
            {
                await _service.CreateAnnouncementAsync(
                    NewMessage.Trim(),
                    _event.EventId,
                    _currentUserId);

                NewMessage = string.Empty;
                await LoadAnnouncementsAsync();
            });
        }
    }

    [RelayCommand]
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
            await _service.DeleteAnnouncementAsync(
                item.Id, _currentUserId, _event.EventId);

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
            await _service.PinAnnouncementAsync(
                item.Id, _event.EventId, _currentUserId);

            await LoadAnnouncementsAsync();
        });
    }

    [RelayCommand]
    private async Task ToggleExpandAsync(AnnouncementItemViewModel? item)
    {
        if (item is null) return;

        item.IsExpanded = !item.IsExpanded;

        if (item.IsExpanded && !item.IsRead)
        {
            try
            {
                await _service.MarkAsReadAsync(item.Id, _currentUserId);
                item.IsRead = true;
                UpdateUnreadCount();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mark as read failed: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task LoadReadReceiptsAsync(AnnouncementItemViewModel? item)
    {
        if (item is null || !IsEventAdmin) return;

        IsReadReceiptsLoading = true;
        try
        {
            var (readers, total) = await _service.GetReadReceiptsAsync(
                item.Id, _event.EventId, _currentUserId);

            ReadReceiptUsers.Clear();
            foreach (var r in readers)
                ReadReceiptUsers.Add(r);

            ReadReceiptReadCount = readers.Count;
            ReadReceiptTotalCount = total;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Read receipts failed: {ex.Message}");
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
            var currentEmoji = payload.Announcement.CurrentUserEmoji;

            if (currentEmoji == payload.Emoji)
                await _service.RemoveReactionAsync(payload.Announcement.Id, _currentUserId);
            else
                await _service.AddOrUpdateReactAsync(payload.Announcement.Id, _currentUserId, payload.Emoji);

            await LoadAnnouncementsAsync();
        });
    }

    private void UpdateUnreadCount()
    {
        UnreadCount = Announcements.Count(a => !a.IsRead);
    }

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("========== FULL EXCEPTION ==========");
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            System.Diagnostics.Debug.WriteLine("=====================================");
            ErrorMessage = ex.Message;
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
