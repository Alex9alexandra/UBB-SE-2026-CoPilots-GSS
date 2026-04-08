using System.Diagnostics;

using Events_GSS.Data.Models;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Services
{
    /// <summary>
    /// Mock implementation of IUserService.
    /// Returns hardcoded users since the Users module is owned by another team.
    /// Replace with a real implementation once the Users module is integrated.
    /// </summary>
    public class MockUserService : IUserService
    {
        private readonly IAttendedEventService _attendedEventService;

        public MockUserService(IAttendedEventService attendedEventService)
        {
            _attendedEventService = attendedEventService;
        }

        private static class UserServiceConstants
        {
            public const int DefaultCurrentUserId = 3;
        }

        // Hardcoded user pool — mirrors the Users table structure
        private static readonly List<User> _allUsers = new()
        {
            new User { UserId = 1, Name = "Alice Popescu",    ReputationPoints = 340  },
            new User { UserId = 2, Name = "Bob Ionescu",      ReputationPoints = 120  },
            new User { UserId = 3, Name = "Carol Popa",       ReputationPoints = 75   },
            new User { UserId = 4, Name = "Dan Gheorghe",     ReputationPoints = 510  },
            new User { UserId = 5, Name = "Elena Moldovan",   ReputationPoints = 0    },
            new User { UserId = 6, Name = "Florin Stanescu",  ReputationPoints = -50  },
        };

        // Hardcoded friendship pairs (bidirectional)
        // Key = userId, Value = list of friend userIds
        private static readonly Dictionary<int, List<int>> _friendships = new()
        {
            { 1, new List<int> { 2, 3, 4 } },
            { 2, new List<int> { 1, 5 } },
            { 3, new List<int> { 1, 4, 6 } },
            { 4, new List<int> { 1, 3 } },
            { 5, new List<int> { 2 } },
            { 6, new List<int> { 3 } },
        };

        // The currently logged-in user — change UserId to simulate a different user
        private static readonly int _currentUserId = 3;

        /// <summary>
        /// Returns the currently logged-in user.
        /// </summary>
        public User GetCurrentUser()
        {
            return _allUsers.First(user => user.UserId == _currentUserId);
        }

        /// <summary>
        /// Returns a user by their ID, or null if not found.
        /// </summary>
        public User? GetUserById(int userId)
        {
            return _allUsers.FirstOrDefault(user => user.UserId == userId);
        }

        /// <summary>
        /// Returns all friends of the given user.
        /// </summary>
        public List<User> GetFriends(int userId)
        {
            if (!_friendships.TryGetValue(userId, out var friendIds))
                return new List<User>();

            return _allUsers
                .Where(u => friendIds.Contains(u.UserId))
                .ToList();
        }

        /// <summary>
        /// Returns friends of the given user whose name contains the search string.
        /// The search is case-insensitive
        /// </summary>
        public List<User> SearchFriends(int userId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return GetFriends(userId);

            return GetFriends(userId)
                .Where(user => user.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }


        public async Task<bool> IsAttending(Event currentEvent)
        {
            try
            {
                var attendingEvents = await _attendedEventService
                    .GetAttendedEventsAsync(_currentUserId);

                return attendingEvents
                    .Any(attendedEvent => attendedEvent.Event.EventId == currentEvent.EventId);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Error checking attendance: " + exception);
                return false;
            }
        }
        public bool IsAdmin(Event currentEvent)
        {
            return currentEvent.Admin.UserId == _currentUserId;
        }

    }
}