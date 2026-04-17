using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;

using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Services.reputationService;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Data.Services
{
    public class MemoryService : IMemoryService
    {
        private readonly IMemoryRepository memoryRepository;
        private readonly IAttendedEventRepository attendedEventRepo;
        private readonly IReputationService reputationService;
        private const int MinimumReputationRequired = -300;

        public MemoryService(IMemoryRepository memoryRepository, IAttendedEventRepository attendedEventRepository, IReputationService reputationService)
        {
            this.memoryRepository = memoryRepository;
            attendedEventRepo = attendedEventRepository;
            this.reputationService = reputationService;
        }

        public async Task<List<Memory>> GetByEventAsync(Event currentEvent, User currentUser)
        {
            var memories = await memoryRepository.GetByEventAsync(currentEvent.EventId);

            foreach (var memory in memories)
            {
                var likes = await this.memoryRepository.GetLikesAsync(memory.MemoryId);
                memory.LikesCount = likes.Count; // Logic moved to C#
                memory.IsLikedByCurrentUser = likes.Contains(currentUser.UserId); // Logic moved to C#
            }

            return memories;
        }

        public async Task<List<string>> GetOnlyPhotosAsync(Event currentEvent)
        {
            var memories = await memoryRepository.GetByEventAsync(currentEvent.EventId);
            return memories
                .Where(memory => memory.PhotoPath != null)
                .Select(memory => memory.PhotoPath!)
                .ToList();
        }

        public async Task<List<Memory>> FilterByMyMemoriesAsync(Event currentEvent, User currentUser)
        {
            var memories = await GetByEventAsync(currentEvent, currentUser);
            return memories.Where(memory => memory.Author.UserId == currentUser.UserId).ToList();
        }

        public async Task<List<Memory>> OrderByDateAsync(Event currentEvent, User currentUser, bool ascending)
        {
            var memories = await GetByEventAsync(currentEvent, currentUser);
            return ascending
                ? memories.OrderBy(memory => memory.CreatedAt).ToList()
                : memories.OrderByDescending(memory => memory.CreatedAt).ToList();
        }

        public async Task AddAsync(Event currentEvent, User author, string? photoPath, string? text)
        {
            if (!await reputationService.CanPostMemoriesAsync(author.UserId))
            {
                throw new InvalidOperationException($"Your reputation is too low to post memories (below {MinimumReputationRequired} RP).");
            }

            var attendance = await attendedEventRepo.GetAsync(currentEvent.EventId, author.UserId);
            if (attendance == null)
            {
                throw new InvalidOperationException("You must first enroll to this event!.");
            }

            bool hasPhoto = !string.IsNullOrWhiteSpace(photoPath);
            bool hasText = !string.IsNullOrWhiteSpace(text);

            if (!hasPhoto && !hasText)
            {
                throw new InvalidOperationException("A memory must have at least a photo or text.");
            }

            var memory = new Memory
            {
                PhotoPath = hasPhoto ? photoPath : null,
                Text = hasText ? text : null,
                CreatedAt = DateTime.UtcNow,
                Event = currentEvent,
                Author = author
            };

            await memoryRepository.AddAsync(memory);

            var action = hasPhoto
                ? ReputationAction.MemoryAddedWithPhoto
                : ReputationAction.MemoryAddedTextOnly;
            WeakReferenceMessenger.Default.Send(
                new ReputationMessage(author.UserId, action));
        }

        public async Task DeleteAsync(Memory memory, User requestingUser)
        {
            var fullMemory = await memoryRepository.GetByIdAsync(memory.MemoryId);

            if (fullMemory == null)
            {
                throw new Exception("Memory not found.");
            }

            bool isAdmin = fullMemory.Event.Admin.UserId == requestingUser.UserId;
            bool isOwner = fullMemory.Author.UserId == requestingUser.UserId;

            if (!isAdmin && !isOwner)
            {
                throw new UnauthorizedAccessException("You can only delete your own memories.");
            }

            await memoryRepository.DeleteAsync(memory.MemoryId);
        }
        public async Task<int> GetLikesCountAsync(int memoryId)
        {
            var likes = await this.memoryRepository.GetLikesAsync(memoryId);
            return likes.Count;
        }

        public async Task ToggleLikeAsync(Memory memory, User currentUser)
        {
            var fullMemory = await memoryRepository.GetByIdAsync(memory.MemoryId);
            if (fullMemory == null)
            {
                throw new Exception("Memory not found.");
            }

            if (fullMemory.Author.UserId == currentUser.UserId)
            {
                throw new InvalidOperationException("You cannot like your own memory.");
            }

            var likes = await memoryRepository.GetLikesAsync(memory.MemoryId);
            bool alreadyLiked = likes.Contains(currentUser.UserId);

            if (alreadyLiked)
            {
                await memoryRepository.RemoveLikeAsync(memory.MemoryId, currentUser.UserId);
            }
            else
            {
                await memoryRepository.AddLikeAsync(memory.MemoryId, currentUser.UserId);
            }
        }
        public bool IsOwnMemory(Memory memory, User currentUser)
        {
            return memory.Author.UserId == currentUser.UserId;
        }
        public bool CanDelete(Memory memory, User currentUser)
        {
            bool isAuthor = memory.Author?.UserId == currentUser.UserId;
            bool isEventAdmin = memory.Event?.Admin?.UserId == currentUser.UserId;
            return isAuthor || isEventAdmin;
        }

        public bool CanLike(Memory memory, User currentUser)
        {
            return memory.Author?.UserId != currentUser.UserId;
        }
    }
}