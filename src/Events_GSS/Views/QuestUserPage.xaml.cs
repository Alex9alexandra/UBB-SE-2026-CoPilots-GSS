using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Events_GSS.ViewModels;

namespace Events_GSS.Views;

public sealed partial class QuestUserPage : Page
{
    public QuestUserViewModel ViewModel { get; set; }

    public QuestUserPage()
    {
        this.InitializeComponent();
    }

    private async void OnSubmitClicked(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { Tag: QuestItemViewModel selectedQuestItem })
        {
            return;
        }

        SubmitProofDialog proofDialog = new SubmitProofDialog(selectedQuestItem)
        {
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result = await proofDialog.ShowAsync();

        if (result == ContentDialogResult.Primary && proofDialog.Result is not null)
        {
            await ViewModel.SubmitProofCommand.ExecuteAsync(proofDialog.Result);
        }
    }

    private async void OnDeleteClicked(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { Tag: QuestItemViewModel selectedQuestItem })
        {
            return;
        }

        ContentDialog confirmationDialog = new ContentDialog
        {
            Title = "Delete Submission",
            Content = $"Delete your proof for \"{selectedQuestItem.Quest.Name}\"?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult confirmationResult = await confirmationDialog.ShowAsync();

        if (confirmationResult == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteSubmissionCommand.ExecuteAsync(selectedQuestItem);
        }
    }
}