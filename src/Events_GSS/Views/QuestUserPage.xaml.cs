using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Events_GSS.ViewModels;

using System;

namespace Events_GSS.Views;

public sealed partial class QuestUserPage : Page
{
    public QuestUserViewModel ViewModel { get; set; }

    public QuestUserPage()
    {
        InitializeComponent();
        
    }

    // public void Initialize(QuestUserViewModel viewModel)
    // {
    //     ViewModel = viewModel;
    //     Loaded += async (_, _) => await ViewModel.LoadQuestsCommand.ExecuteAsync(null);
    // }

    private async void OnSubmitClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: QuestItemViewModel item }) return;

        var dialog = new SubmitProofDialog(item) { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.Result is not null)
            await ViewModel.SubmitProofCommand.ExecuteAsync(dialog.Result);
    }

    private async void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: QuestItemViewModel item }) return;

        var confirm = new ContentDialog
        {
            Title = "Delete Submission",
            Content = $"Delete your proof for \"{item.Quest.Name}\"?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.DeleteSubmissionCommand.ExecuteAsync(item);
    }
}