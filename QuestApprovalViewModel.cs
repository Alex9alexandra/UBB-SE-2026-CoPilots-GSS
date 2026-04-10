using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace Events_GSS.ViewModels;

public partial class QuestApprovalViewModel : ObservableObject
{
    private readonly IQuestApprovalService questApprovalService = App.Services.GetRequiredService<IQuestApprovalService>();
    private readonly IUserService userService = App.Services.GetRequiredService<IUserService>();

    public QuestAdminViewModel QuestAdminVM { get; }

    public ObservableCollection<QuestMemory> Submissions { get; set; } = new ();

    [ObservableProperty]
    public partial bool IsLoadingSubmissions { get; set; }

    public QuestApprovalViewModel(QuestAdminViewModel adminViewModel)
    {
        QuestAdminVM = adminViewModel;

        QuestAdminVM.PropertyChanged += QuestAdminVM_PropertyChanged;
    }

    private async void QuestAdminVM_PropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(QuestAdminVM.SelectedQuest))
        {
            try
            {
                await RefreshSubmissionsAsync();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Error refreshing submissions: {exception.Message}");
                throw;
            }
        }
    }

    public async Task RefreshSubmissionsAsync()
    {
        Submissions.Clear();
        if (QuestAdminVM.SelectedQuest == null)
        {
            return;
        }

        IsLoadingSubmissions = true;
        try
        {
            var questProofs = await questApprovalService.GetProofsForQuestAsync(QuestAdminVM.SelectedQuest);
            foreach (var questProof in questProofs)
            {
                Submissions.Add(questProof);
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine("Error retrieving proofs: " + exception.Message);
        }
        finally
        {
            IsLoadingSubmissions = false;
        }
    }

    [RelayCommand]
    private async Task ApproveAsync(QuestMemory proof)
    {
        proof.ProofStatus = QuestMemoryStatus.Approved;
        await questApprovalService.ChangeProofStatusAsync(proof);
        Submissions.Remove(proof);
    }

    [RelayCommand]
    private async Task DenyAsync(QuestMemory proof)
    {
        proof.ProofStatus = QuestMemoryStatus.Rejected;
        await questApprovalService.ChangeProofStatusAsync(proof);
        Submissions.Remove(proof);
    }

    [RelayCommand]
    public async Task DeleteAsync(QuestMemory proof)
    {
        await questApprovalService.DeleteSubmissionAsync(proof, userService.GetCurrentUser());
        Submissions.Remove(proof);
    }
}