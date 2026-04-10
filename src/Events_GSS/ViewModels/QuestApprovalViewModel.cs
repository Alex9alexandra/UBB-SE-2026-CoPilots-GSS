using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services.Interfaces;
using Events_GSS.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace Events_GSS.ViewModels;

public partial class QuestApprovalViewModel : ObservableObject
{
    private readonly IQuestApprovalService _questService = App.Services.GetRequiredService<IQuestApprovalService>();
    private readonly IUserService _userService = App.Services.GetRequiredService<IUserService>();
    
    public QuestAdminViewModel QuestAdminVM { get; }

    public ObservableCollection<QuestMemory> Submissions { get; set; } = new();

    [ObservableProperty]
    public partial bool IsLoadingSubmissions { get; set; }

    public QuestApprovalViewModel(QuestAdminViewModel adminVM)
    {
        QuestAdminVM = adminVM;

        QuestAdminVM.PropertyChanged += QuestAdminVM_PropertyChanged;
    }
    private async void QuestAdminVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuestAdminVM.SelectedQuest))
        {
            try
            {
                await RefreshSubmissionsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing submissions: {ex.Message}");
                throw ex;
                return;
            }
        }
    }
    public async Task RefreshSubmissionsAsync()
    {
        Submissions.Clear();
        if (QuestAdminVM.SelectedQuest == null) return;

        IsLoadingSubmissions = true;
        try
        {
            var proofs = await _questService.GetProofsForQuestAsync(QuestAdminVM.SelectedQuest);
            foreach (var p in proofs) Submissions.Add(p);
        }
        catch (Exception exc)
        {
            Debug.WriteLine("HEREEEEEEEEEE"+ exc.Message);
        }
        finally { IsLoadingSubmissions = false; }
    }

    [RelayCommand]
    private async Task ApproveAsync(QuestMemory proof)
    {
        proof.ProofStatus = QuestMemoryStatus.Approved;
        await _questService.ChangeProofStatusAsync(proof);
        Submissions.Remove(proof);
    }

    [RelayCommand]
    private async Task DenyAsync(QuestMemory proof)
    {
        proof.ProofStatus = QuestMemoryStatus.Rejected;
        await _questService.ChangeProofStatusAsync(proof);
        Submissions.Remove(proof);
    }
    [RelayCommand]
    public async Task DeleteAsync(QuestMemory proof)
    {
        await _questService.DeleteSubmissionAsync(proof,_userService.GetCurrentUser());
        Submissions.Remove(proof);
    }
}