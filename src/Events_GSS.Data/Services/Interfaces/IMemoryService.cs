using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.Interfaces {

    public interface IMemoryService
    {
        
        Task<List<Memory>> GetByEventAsync(Event forEvent, User currentUser);
        Task<List<string>> GetOnlyPhotosAsync(Event eve);
        Task<List<Memory>> FilterByMyMemoriesAsync(Event eve, User user);
        Task<List<Memory>> OrderByDateAsync(Event eve, User user, bool ascending);
        Task AddAsync(Event eve, User user, string? photoPath, string? text);
        Task DeleteAsync(Memory memory, User user);
        Task ToggleLikeAsync(Memory memory, User user);
        Task<int> GetLikesCountAsync(int memoryId);
        public bool IsOwnMemory(Memory memory, User currentUser);
            

        }

}