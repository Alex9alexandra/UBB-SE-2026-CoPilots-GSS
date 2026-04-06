using System;
using System.Collections.Generic;

using Microsoft.UI.Xaml.Controls;

namespace Events_GSS.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;

    // Maps page keys to page types
    private readonly Dictionary<string, Type> _pages = new();

    /// <summary>
    /// Call this once from ShellPage after the Frame is available.
    /// </summary>
    public void SetFrame(Frame frame)
    {
        _frame = frame;
    }

    /// <summary>
    /// Register a page key → page type mapping.
    /// Call during app startup for each navigable page.
    /// </summary>
    public void RegisterPage(string key, Type pageType)
    {
        _pages[key] = pageType;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void NavigateTo(string pageKey)
    {
        NavigateTo(pageKey, null!);
    }

    public void NavigateTo(string pageKey, object parameter)
    {
        if (_frame is null)
            throw new InvalidOperationException("NavigationService frame not set. Call SetFrame first.");

        if (!_pages.TryGetValue(pageKey, out var pageType))
            throw new ArgumentException($"Page '{pageKey}' is not registered.");

        _frame.Navigate(pageType, parameter);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }
}
