using Events_GSS.Data.Models;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Events_GSS.ViewModels;

namespace Events_GSS.Views;

public sealed partial class QuestApprovalPage : Page
{
    public QuestApprovalViewModel ViewModel { get; set; }

    public QuestApprovalPage()
    {
        this.InitializeComponent();
    }

    public static Visibility GetVisibility(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? Visibility.Collapsed : Visibility.Visible;
    }
    public static BitmapImage? GetImageSource(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        return new BitmapImage(new System.Uri(path));
    }
    private void ApproveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is QuestMemory item)
        {
            ViewModel.ApproveCommand.Execute(item);
        }
    }


    private void DenyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is QuestMemory item)
        {
            ViewModel.DenyCommand.Execute(item);
        }
    }
    private async void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is QuestMemory selectedItem)
        {
            await ViewModel.DeleteAsync(selectedItem);
        }
    }
}