// <copyright file="MemoryItemViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using Events_GSS.Data.Models;

    /// <summary>
    /// Represents the view model for a single memory item.
    /// </summary>
    public class MemoryItemViewModel : INotifyPropertyChanged
    {
        private int likesCount;
        private bool isLikedByCurrentUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryItemViewModel"/> class.
        /// </summary>
        /// <param name="memory">The memory model.</param>
        /// <param name="canDelete">Indicates if the user can delete this memory.</param>
        /// <param name="canLike">Indicates if the user can like this memory.</param>
        public MemoryItemViewModel(Memory memory, bool canDelete, bool canLike)
        {
            this.Memory = memory;
            this.likesCount = memory.LikesCount;
            this.isLikedByCurrentUser = memory.IsLikedByCurrentUser;
            this.CanDelete = canDelete;
            this.CanLike = canLike;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the underlying memory model.
        /// </summary>
        public Memory Memory { get; }

        /// <summary>
        /// Gets the memory identifier.
        /// </summary>
        public int MemoryId => this.Memory.MemoryId;

        /// <summary>
        /// Gets the photo path.
        /// </summary>
        public string? PhotoPath => this.Memory.PhotoPath;

        /// <summary>
        /// Gets the text content.
        /// </summary>
        public string? Text => this.Memory.Text;

        /// <summary>
        /// Gets the creation date.
        /// </summary>
        public System.DateTime CreatedAt => this.Memory.CreatedAt;

        /// <summary>
        /// Gets the author's name.
        /// </summary>
        public string AuthorName => this.Memory.Author?.Name ?? string.Empty;

        /// <summary>
        /// Gets a value indicating whether the memory has a photo.
        /// </summary>
        public bool HasPhoto => !string.IsNullOrEmpty(this.Memory.PhotoPath);

        /// <summary>
        /// Gets a value indicating whether the memory has text.
        /// </summary>
        public bool HasText => !string.IsNullOrEmpty(this.Memory.Text);

        /// <summary>
        /// Gets or sets the number of likes.
        /// </summary>
        public int LikesCount
        {
            get => this.likesCount;
            set
            {
                this.likesCount = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current user liked this memory.
        /// </summary>
        public bool IsLikedByCurrentUser
        {
            get => this.isLikedByCurrentUser;
            set
            {
                this.isLikedByCurrentUser = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the memory can be deleted by the user.
        /// </summary>
        public bool CanDelete { get; }

        /// <summary>
        /// Gets a value indicating whether the memory can be liked by the user.
        /// </summary>
        public bool CanLike { get; }

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}