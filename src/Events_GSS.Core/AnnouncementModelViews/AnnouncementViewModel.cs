// <copyright file="AnnouncementViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.ViewModels;

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.announcementServices;

using System.Diagnostics.CodeAnalysis;
public record AnnouncementReactionPayload(AnnouncementItemViewModel announcement, string emoji);

/// <summary>
/// AnnouncementViewModel responsible for managing announcements for a single event,
/// including CRUD operations, read receipts, pinning, reactions, and UI state.
/// </summary>
public partial class AnnouncementViewModel : ObservableObject
{
    private readonly IAnnouncementService _announcementService;
    private readonly Event _currentEvent;
    private readonly int _currentUserId;

    public AnnouncementViewModel(
        Event forEvent,
        IAnnouncementService service,
        int currentUserId,
        bool isAdmin)
    {
        this._currentEvent = forEvent;
        this._announcementService = service;
        this._currentUserId = currentUserId;
        this.IsEventAdmin = isAdmin;

        this.Announcements = new ObservableCollection<AnnouncementItemViewModel>();
        this.ReadReceiptUsers = new ObservableCollection<AnnouncementReadReceipt>();
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

    /// <summary>
    /// Gets percentage of participants who have read the announcement.
    /// </summary>
    public string ReadReceiptSummary
    {
        get
        {
            if (this.ReadReceiptTotalCount == 0)
            {
                return "No participants";
            }

            var percentageOfUsersWhoReadAnnouncement = (int)Math.Round(
                percentageMultiplier * this.ReadReceiptReadCount / this.ReadReceiptTotalCount);

            return $"{this.ReadReceiptReadCount} / {this.ReadReceiptTotalCount} read ({percentageOfUsersWhoReadAnnouncement}%)";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEventAdmin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private string _newMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(CreateButtonText))]
    private AnnouncementItemViewModel? _editingAnnouncement;

    [ExcludeFromCodeCoverage]
    public bool IsNotLoading => !this.IsLoading;

    [ExcludeFromCodeCoverage]
    public bool HasError => this.ErrorMessage is not null;

    public bool IsEditing => this.EditingAnnouncement is not null;

    [ExcludeFromCodeCoverage]
    public string CreateButtonText => this.IsEditing ? "Save Edit" : "Post";

    /// <summary>
    /// Initializes the ViewModel and loads announcements for the current event.
    /// </summary>
    public async Task InitializeAsync()
    {
        await this.RunGuardedAsync(this.LoadAnnouncementsAsync);
    }

    [RelayCommand]
    private async Task LoadAnnouncementsAsync()
    {
        var announcementsList = await this._announcementService.GetAnnouncementsAsync(
            this._currentEvent.EventId, this._currentUserId);

        this.Announcements.Clear();
        foreach (var announcement in announcementsList)
        {
            this.Announcements.Add(new AnnouncementItemViewModel(announcement, this._currentUserId, this.IsEventAdmin));
        }

        this.UpdateUnreadCount();
    }

    [RelayCommand]

    // Creates or updates an announcement
    private async Task SubmitAnnouncementAsync()
    {
        if (string.IsNullOrWhiteSpace(this.NewMessage))
        {
            return;
        }

        if (this.IsEditing)
        {
            await this.RunGuardedAsync(async () =>
            {
                await this._announcementService.UpdateAnnouncementAsync(
                    this.EditingAnnouncement!.Id,
                    this.NewMessage.Trim(),
                    this._currentUserId,
                    this._currentEvent.EventId);

                this.EditingAnnouncement = null;
                this.NewMessage = string.Empty;
                await this.LoadAnnouncementsAsync();
            });
        }
        else
        {
            await this.RunGuardedAsync(async () =>
            {
                await this._announcementService.CreateAnnouncementAsync(
                    this.NewMessage.Trim(),
                    this._currentEvent.EventId,
                    this._currentUserId);

                this.NewMessage = string.Empty;
                await this.LoadAnnouncementsAsync();
            });
        }
    }

    [RelayCommand]

    // When user clicks on edit button, this gets the announcement they're editing and pre-fills the box with the announcement's text
    private void StartEdit(AnnouncementItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        this.EditingAnnouncement = item;
        this.NewMessage = item.Message;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        this.EditingAnnouncement = null;
        this.NewMessage = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteAnnouncementAsync(AnnouncementItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await this.RunGuardedAsync(async () =>
        {
            await this._announcementService.DeleteAnnouncementAsync(
                item.Id, this._currentUserId, this._currentEvent.EventId);

            this.Announcements.Remove(item);
            this.UpdateUnreadCount();
        });
    }

    [RelayCommand]
    private async Task PinAnnouncementAsync(AnnouncementItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await this.RunGuardedAsync(async () =>
        {
            await this._announcementService.PinAnnouncementAsync(
                item.Id, this._currentEvent.EventId, this._currentUserId);

            await this.LoadAnnouncementsAsync();
        });
    }

    [RelayCommand]
    private async Task ToggleExpandAsync(AnnouncementItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        item.IsExpanded = !item.IsExpanded;

        if (item.IsExpanded)
        {
            var wasMarked = await this._announcementService.MarkAsReadIfNeededAsync(
                item.Id,
                this._currentUserId,
                item.IsRead);

            if (wasMarked)
            {
                item.IsRead = true;
                this.UpdateUnreadCount();
            }
        }
    }

    [RelayCommand]

    // Returns a list with all the people who have read the current announcement (only for admins)
    private async Task LoadReadReceiptsAsync(AnnouncementItemViewModel? item)
    {
        if (item is null || !this.IsEventAdmin)
        {
            return;
        }

        this.IsReadReceiptsLoading = true;
        try
        {
            var (readers, total) = await this._announcementService.GetReadReceiptsAsync(
                item.Id, this._currentEvent.EventId, this._currentUserId);

            this.ReadReceiptUsers.Clear();
            foreach (var reader in readers)
            {
                this.ReadReceiptUsers.Add(reader);
            }

            this.ReadReceiptReadCount = readers.Count;
            this.ReadReceiptTotalCount = total;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Read receipts failed: {exception.Message}");
        }
        finally
        {
            this.IsReadReceiptsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleReactionAsync(AnnouncementReactionPayload? payload)
    {
        if (payload is null)
        {
            return;
        }

        await this.RunGuardedAsync(async () =>
        {
            await this._announcementService.ToggleReactionAsync(payload.announcement.Id, this._currentUserId, payload.emoji);

            await this.LoadAnnouncementsAsync();
        });
    }

    private void UpdateUnreadCount()
    {
        this.UnreadCount = this.Announcements.Count(a => !a.IsRead);
    }

    // Wraps an async operation to manage loading state and handle exceptions consistently across the ViewModel
    private async Task RunGuardedAsync(Func<Task> action)
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        try
        {
            await action();
        }
        catch (UnauthorizedAccessException)
        {
            this.ErrorMessage = "You don't have permission for this action.";
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine("========== FULL EXCEPTION ==========");
            System.Diagnostics.Debug.WriteLine(exception.ToString());
            System.Diagnostics.Debug.WriteLine("=====================================");
            this.ErrorMessage = exception.Message;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    [ExcludeFromCodeCoverage]
    public async Task<List<User>> GetAllParticipantsAsync()
    {
        return await this._announcementService.GetAllParticipantsAsync(this._currentEvent.EventId);
    }

    [ExcludeFromCodeCoverage]
    public async Task<List<User>> GetNonReadersAsync(int announcementId)
    {
        return await this._announcementService.GetNonReadersAsync(
            announcementId,
            this._currentEvent.EventId);
    }

    partial void OnIsLoadingChanged(bool value) => NotifyCommandsChanged();

    partial void OnNewMessageChanged(string value) => NotifyCommandsChanged();

    private void NotifyCommandsChanged()
    {
        this.SubmitAnnouncementCommand.NotifyCanExecuteChanged();
    }
}
