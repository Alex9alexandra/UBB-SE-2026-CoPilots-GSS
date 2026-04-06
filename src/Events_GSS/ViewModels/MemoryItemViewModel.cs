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


        public bool CanDelete { get; }
        public bool CanLike { get; }

        public MemoryItemViewModel(Memory memory, bool canDelete, bool canLike)
        {
            Memory = memory;
            _likesCount = memory.LikesCount;
            _isLikedByCurrentUser = memory.IsLikedByCurrentUser;
            CanDelete = canDelete;
            CanLike = canLike;
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}