// <copyright file="MemoryView.xaml.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Views
{
    using System;
    using System.Threading.Tasks;

    using Events_GSS.Data.Models;
    using Events_GSS.ViewModels;

    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Controls.Primitives;

    /// <summary>
    /// Interaction logic for the Memory View control.
    /// </summary>
    public sealed partial class MemoryView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryView"/> class.
        /// </summary>
        public MemoryView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the ViewModel associated with this view.
        /// </summary>
        public MemoryViewModel ViewModel { get; set; } = null!;

        /// <summary>
        /// Initializes and loads the view model data asynchronously.
        /// </summary>
        /// <param name="ev">The event to load memories for.</param>
        /// <param name="user">The current user viewing the memories.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadAsync(Event ev, User user)
        {
            await this.ViewModel.InitializeAsync(ev, user);
        }

        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton btn || btn.DataContext is not MemoryItemViewModel item)
            {
                return;
            }

            await this.ViewModel.ToggleLikeAsync(item);
        }

        private void MyMemoriesToggle_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.ShowOnlyMine = this.MyMemoriesToggle.IsChecked == true;
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not MemoryItemViewModel item)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Delete Memory",
                Content = "Are you sure you want to delete this memory?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await this.ViewModel.DeleteMemoryAsync(item);
            }
        }

        private async void AddMemoryButton_Click(object sender, RoutedEventArgs e)
        {
            var photoPathBox = new TextBox
            {
                PlaceholderText = "Photo path (optional, e.g. C:\\Photos\\photo.jpg)",
                Margin = new Thickness(0, 0, 0, 8),
            };
            var textBox = new TextBox
            {
                PlaceholderText = "Write something about this memory... (optional)",
                AcceptsReturn = true,
                Height = 120,
                TextWrapping = TextWrapping.Wrap,
            };

            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock { Text = "Photo path", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            panel.Children.Add(photoPathBox);
            panel.Children.Add(new TextBlock { Text = "Text", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            panel.Children.Add(textBox);
            panel.Children.Add(new TextBlock
            {
                Text = "At least one of photo or text is required.",
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Margin = new Thickness(0, 4, 0, 0),
            });

            var dialog = new ContentDialog
            {
                Title = "Add Memory",
                Content = panel,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await this.ViewModel.AddMemoryAsync(photoPathBox.Text, textBox.Text);
            }
        }
    }
}