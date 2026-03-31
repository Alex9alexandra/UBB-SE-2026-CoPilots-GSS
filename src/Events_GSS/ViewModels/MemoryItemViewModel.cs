using System.ComponentModel;
using System.Runtime.CompilerServices;

using Events_GSS.Data.Models;

namespace Events_GSS.ViewModels
{
    public class MemoryItemViewModel : INotifyPropertyChanged
    {
        public Memory Memory { get; }

        private int _likesCount;
        private bool _isLikedByCurrentUser;

        public int MemoryId => Memory.MemoryId;
        public string? PhotoPath => Memory.PhotoPath;
        public string? Text => Memory.Text;
        public System.DateTime CreatedAt => Memory.CreatedAt;
        public string AuthorName => Memory.Author?.Name ?? "";

        public bool HasPhoto => !string.IsNullOrEmpty(Memory.PhotoPath);
        public bool HasText => !string.IsNullOrEmpty(Memory.Text);

        public int LikesCount
        {
            get => _likesCount;
            set { _likesCount = value; OnPropertyChanged(); }
        }

        public bool IsLikedByCurrentUser
        {
            get => _isLikedByCurrentUser;
            set { _isLikedByCurrentUser = value; OnPropertyChanged(); }
        }

        // Visibility-equivalent booleans 
        public bool CanDelete { get; }
        public bool CanLike { get; }

        public MemoryItemViewModel(Memory memory, User currentUser)
        {
            Memory = memory;
            _likesCount = memory.LikesCount;
            _isLikedByCurrentUser = memory.IsLikedByCurrentUser;

            bool isAuthor = memory.Author?.UserId == currentUser.UserId;
            bool isEventAdmin = memory.Event?.Admin?.UserId == currentUser.UserId;

            CanDelete = isAuthor || isEventAdmin;
            CanLike = !isAuthor;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}