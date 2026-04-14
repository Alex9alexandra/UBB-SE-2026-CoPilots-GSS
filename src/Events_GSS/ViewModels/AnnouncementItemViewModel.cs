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
    private readonly AnnouncementItemViewModelCore _core;

    public AnnouncementItemViewModel(
        Announcement model,
        int currentUserId,
        bool isAdmin)
    {
        _core = new AnnouncementItemViewModelCore(model, currentUserId);

        Model = model;
        _isCurrentUserAdmin = isAdmin;
        _isRead = model.IsRead;
    }

    public Announcement Model { get; }

    public string PreviewText => _core.PreviewText;

    public bool HasFullContent => _core.HasFullContent;

    public List<ReactionGroup> ReactionGroups => _core.ReactionGroups;

    public string? CurrentUserEmoji => _core.CurrentUserEmoji;

    // UI stuff stays here
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isRead;
    public bool IsCurrentUserAdmin => this._isCurrentUserAdmin;
    public bool IsUnread => !this.IsRead;

    private readonly bool _isCurrentUserAdmin;
}
