using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Services.Interfaces;

namespace Events_GSS.Data.Services
{
    public class MemoryService : IMemoryService
    {
        private readonly IMemoryRepository _memoryRepo;
        //IAttendedEvents ....

        public MemoryService(IMemoryRepository memoryRepo)
        {
            _memoryRepo = memoryRepo;
        }

        public async Task<List<Memory>> GetByEventAsync(Event forEvent, User currentUser)
        {
            var memories = await _memoryRepo.GetByEventAsync(forEvent.EventId);

            foreach (var memory in memories)
            {
                memory.LikesCount = await _memoryRepo.GetLikesCountAsync(memory.MemoryId);
                memory.IsLikedByCurrentUser = await _memoryRepo.HasLikedAsync(memory.MemoryId, currentUser.UserId);
            }

            return memories;
        }

        public async Task<List<string>> GetOnlyPhotosAsync(Event forEvent)
        {
            var memories = await _memoryRepo.GetByEventAsync(forEvent.EventId);
            return memories
                .Where(m => m.PhotoPath != null)
                .Select(m => m.PhotoPath!)
                .ToList();
        }

        public async Task<List<Memory>> FilterByMyMemoriesAsync(Event forEvent, User currentUser)
        {
            var memories = await GetByEventAsync(forEvent, currentUser);
            return memories.Where(m => m.Author.UserId == currentUser.UserId).ToList();
        }

        public async Task<List<Memory>> OrderByDateAsync(Event forEvent, User currentUser, bool ascending)
        {
            var memories = await GetByEventAsync(forEvent, currentUser);
            return ascending
                ? memories.OrderBy(m => m.CreatedAt).ToList()
                : memories.OrderByDescending(m => m.CreatedAt).ToList();
        }

        public async Task AddAsync(Event forEvent, User author, string? photoPath, string? text)
        {
            bool hasPhoto = !string.IsNullOrWhiteSpace(photoPath);
            bool hasText = !string.IsNullOrWhiteSpace(text);
            //check if user is in attended events, if no exception

            if (!hasPhoto && !hasText)
                throw new InvalidOperationException("A memory must have at least a photo or text.");

            var memory = new Memory
            {
                PhotoPath = hasPhoto ? photoPath : null,
                Text = hasText ? text : null,
                CreatedAt = DateTime.UtcNow,
                Event = forEvent,
                Author = author
            };

            await _memoryRepo.AddAsync(memory);
        }

        public async Task DeleteAsync(Memory memory, User requestingUser)
        {
            var fullMemory = await _memoryRepo.GetByIdAsync(memory.MemoryId);

            if (fullMemory == null)
                throw new Exception("Memory not found.");

            bool isAdmin = fullMemory.Event.Admin.UserId == requestingUser.UserId;
            bool isOwner = fullMemory.Author.UserId == requestingUser.UserId;

            if (!isAdmin && !isOwner)
                throw new UnauthorizedAccessException("You can only delete your own memories.");

            await _memoryRepo.DeleteAsync(memory.MemoryId);
        }
        public async Task<int> GetLikesCountAsync(int memoryId)
        {
            return await _memoryRepo.GetLikesCountAsync(memoryId);
        }


        public async Task ToggleLikeAsync(Memory memory, User currentUser)
        {
            var fullMemory = await _memoryRepo.GetByIdAsync(memory.MemoryId);

            if (fullMemory == null)
                throw new Exception("Memory not found.");

            if (fullMemory.Author.UserId == currentUser.UserId)
                throw new InvalidOperationException("You cannot like your own memory.");

            bool alreadyLiked = await _memoryRepo.HasLikedAsync(memory.MemoryId, currentUser.UserId);
            if (alreadyLiked)
                await _memoryRepo.RemoveLikeAsync(memory.MemoryId, currentUser.UserId);
            else
                await _memoryRepo.AddLikeAsync(memory.MemoryId, currentUser.UserId);
        }
        public bool IsOwnMemory(Memory memory, User currentUser)
        {
            return memory.Author.UserId == currentUser.UserId;
        }
    }
}


