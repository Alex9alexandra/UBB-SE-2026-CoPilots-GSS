using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.ViewModels;

public class ReputationViewModel : INotifyPropertyChanged
{
    private readonly IUserService _userService;
    private readonly IReputationService _reputationService;

    private string _userName = string.Empty;
    public string UserName
    {
        get => _userName;
        private set { _userName = value; OnPropertyChanged(); }
    }

    private int _reputationPoints;
    public int ReputationPoints
    {
        get => _reputationPoints;
        private set { _reputationPoints = value; OnPropertyChanged(); }
    }

    private string _currentTier = "Newcomer";
    public string CurrentTier
    {
        get => _currentTier;
        private set { _currentTier = value; OnPropertyChanged(); }
    }

    private ObservableCollection<Achievement> _achievements = new();
    public ObservableCollection<Achievement> Achievements
    {
        get => _achievements;
        private set { _achievements = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(); }
    }

    public ReputationViewModel(IUserService userService, IReputationService reputationService)
    {
        _userService = userService;
        _reputationService = reputationService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var user = _userService.GetCurrentUser();
            UserName = user.Name;
            ReputationPoints = await _reputationService.GetReputationPointsAsync(user.UserId);
            CurrentTier = await _reputationService.GetTierAsync(user.UserId);
            Achievements = new ObservableCollection<Achievement>(
                await _reputationService.GetAchievementsAsync(user.UserId));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load reputation: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
