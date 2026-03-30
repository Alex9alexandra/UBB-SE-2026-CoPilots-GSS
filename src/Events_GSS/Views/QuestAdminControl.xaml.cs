using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Windows.Input;

using Events_GSS.ViewModels;

namespace Events_GSS.Views;

public sealed partial class QuestAdminControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(QuestAdminViewModel),
            typeof(QuestAdminControl),
            new PropertyMetadata(null));

    public QuestAdminViewModel? ViewModel
    {
        get => (QuestAdminViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }


    public QuestAdminControl()
    {
        InitializeComponent();
    }
}