// <copyright file="AnnouncementItemViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

using Events_GSS.Data.Models;
using Events_GSS.Data.ViewModelsCore;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// ViewModel representing a single announcement item with user-specific state
/// such as read status, reactions, and admin permissions.
/// </summary>
public partial class AnnouncementItemViewModel : ObservableObject
{
    private readonly AnnouncementItemViewModelCore announcementItemViewModelCore;

    public AnnouncementItemViewModel(
        Announcement announcementModel,
        int currentUserId,
        bool isAdmin)
    {
        announcementItemViewModelCore = new AnnouncementItemViewModelCore(announcementModel, currentUserId);

        Model = announcementModel;
        _isCurrentUserAdmin = isAdmin;
        _isRead = announcementModel.IsRead;
    }

    public Announcement Model { get; }

    public string PreviewText => announcementItemViewModelCore.PreviewText;

    public bool HasFullContent => announcementItemViewModelCore.HasFullContent;

    public List<ReactionGroup> ReactionGroups => announcementItemViewModelCore.ReactionGroups;

    public string? CurrentUserEmoji => announcementItemViewModelCore.CurrentUserEmoji;

    // UI stuff stays here
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] public bool _isRead;
    public bool IsCurrentUserAdmin => this._isCurrentUserAdmin;
    public bool IsUnread => !this._isRead;

    private readonly bool _isCurrentUserAdmin;
}
