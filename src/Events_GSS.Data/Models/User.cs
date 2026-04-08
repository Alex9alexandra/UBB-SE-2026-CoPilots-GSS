using System;

namespace Events_GSS.Data.Models
{
    public class UserDefaults
    {
        public const int DefaultReputationPoints = 0;
    }
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public int ReputationPoints { get; set; } = UserDefaults.DefaultReputationPoints;
    }
}