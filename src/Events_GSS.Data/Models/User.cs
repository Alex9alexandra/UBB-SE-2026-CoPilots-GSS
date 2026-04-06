using System;

namespace Events_GSS.Data.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public int ReputationPoints { get; set; } = 0;
    }
}