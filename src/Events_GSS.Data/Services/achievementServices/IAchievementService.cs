using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.achievementServices;

public interface IAchievementService
{
    Task<List<Achievement>> GetUserAchievementsAsync(int userId);

    Task CheckAndAwardAchievementsAsync(int userId);
}
