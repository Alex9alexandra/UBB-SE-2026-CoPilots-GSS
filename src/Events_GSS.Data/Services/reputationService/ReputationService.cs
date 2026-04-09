// <copyright file="ReputationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Services.reputationService;

using System.Diagnostics;

using CommunityToolkit.Mvvm.Messaging;

using Events_GSS.Data.Messaging;
using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories;
using Events_GSS.Data.Repositories.achievementRepository;
using Events_GSS.Data.Repositories.eventRepository;
using Events_GSS.Data.Repositories.reputationRepository;
using Events_GSS.Data.Services.achievementServices;

/// <summary>
/// Implements the <see cref="IReputationService"/> interface, providing methods to manage and retrieve user reputation points, tiers, and related permissions in the system. This class interacts with the reputation repository to perform operations related to user reputation management, allowing for setting and retrieving reputation points and tiers based on user activity and achievements. The service also handles reputation changes triggered by various user actions, ensuring that reputation data is updated accurately and efficiently in response to user interactions within the platform. Additionally, it integrates with the achievement service to check and award achievements based on reputation changes, supporting the gamification features of the application.
/// </summary>
public class ReputationService : IReputationService
{
    private static readonly Dictionary<ReputationAction, int> ReputationDeltasMap = new ()
    {
        { ReputationAction.EventCreated, ReputationDeltas.EventCreated },
        { ReputationAction.EventCancelled, ReputationDeltas.EventCancelled },
        { ReputationAction.DiscussionMessagePosted, ReputationDeltas.DiscussionMessagePosted },
        { ReputationAction.DiscussionMessageRemovedByAdmin, ReputationDeltas.DiscussionMessageRemovedByAdmin },
        { ReputationAction.MemoryAddedWithPhoto, ReputationDeltas.MemoryAddedWithPhoto },
        { ReputationAction.MemoryAddedTextOnly, ReputationDeltas.MemoryAddedTextOnly },
        { ReputationAction.QuestSubmitted, ReputationDeltas.QuestSubmitted },
        { ReputationAction.QuestApproved, ReputationDeltas.QuestApproved },
        { ReputationAction.QuestDenied, ReputationDeltas.QuestDenied },
    };

    private readonly IReputationRepository reputationRepository;
    private readonly IAttendedEventRepository attendedEventRepository;
    private readonly IEventRepository eventRepository;
    private readonly IAchievementService achievementService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReputationService"/> class with the specified repositories and services. The constructor sets up the necessary dependencies for managing user reputation, including the reputation repository for handling reputation data, the attended event repository and event repository for managing event-related reputation changes, and the achievement service for checking and awarding achievements based on reputation changes. Additionally, the constructor registers a message handler for reputation change messages using the CommunityToolkit.Mvvm.Messaging library, allowing the service to respond to reputation changes triggered by various user actions within the platform.
    /// </summary>
    /// <param name="reputationRepository">The repository for managing reputation data.</param>
    /// <param name="attendedEventRepository">The repository for managing attended events.</param>
    /// <param name="eventRepository">The repository for managing events.</param>
    /// <param name="achievementService">The service for managing achievements.</param>
    public ReputationService(
        IReputationRepository reputationRepository,
        IAttendedEventRepository attendedEventRepository,
        IEventRepository eventRepository,
        IAchievementService achievementService)
    {
        this.reputationRepository = reputationRepository;
        this.attendedEventRepository = attendedEventRepository;
        this.eventRepository = eventRepository;
        this.achievementService = achievementService;

        WeakReferenceMessenger.Default.Register<ReputationMessage>(this, (_, msg) =>
        {
            _ = this.HandleReputationChangeAsync(msg);
        });
    }

    /// <summary>
    /// Asynchronously retrieves the reputation points for a specific user by their user ID. This method interacts with the reputation repository to fetch the current reputation points associated with the user, allowing the application to display the user's reputation status and determine their permissions based on their reputation level. The method ensures that the reputation data is retrieved efficiently, supporting the gamification features of the application and enabling dynamic updates to user permissions and tiers based on their reputation points.
    /// </summary>
    /// <param name="userId">The ID of the user whose reputation points are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the reputation points of the specified user.</returns>
    public async Task<int> GetReputationPointsAsync(int userId)
    {
        return await this.reputationRepository.GetReputationPointsAsync(userId);
    }

    /// <summary>
    /// Asynchronously retrieves the reputation tier for a specific user by their user ID. This method interacts with the reputation repository to fetch the current reputation tier associated with the user, allowing the application to display the user's tier status and determine their permissions based on their reputation level. The method ensures that the tier data is retrieved efficiently, supporting the gamification features of the application and enabling dynamic updates to user permissions and tiers based on their reputation points.
    /// </summary>
    /// <param name="userId">The ID of the user whose reputation tier is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the reputation tier of the specified user.</returns>
    public async Task<string> GetTierAsync(int userId)
    {
        return await this.reputationRepository.GetTierAsync(userId);
    }

    /// <summary>
    /// Asynchronously retrieves a list of achievements for a specific user by their user ID. This method interacts with the achievement service to fetch the achievements that the user has unlocked based on their reputation points and actions within the platform. The method allows the application to display the user's achievements, providing recognition for their contributions and engagement in the community. By retrieving the user's achievements, the application can enhance the gamification experience and encourage users to continue participating and contributing to the platform to unlock more achievements.
    /// </summary>
    /// <param name="userId">The ID of the user whose achievements are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of achievements for the specified user.</returns>
    public async Task<List<Achievement>> GetAchievementsAsync(int userId)
    {
        return await this.achievementService.GetUserAchievementsAsync(userId);
    }

    /// <summary>
    /// Asynchronously determines whether a specific user has the permission to post memories based on their reputation points. This method retrieves the user's current reputation points and compares them against a predefined threshold to determine if they are allowed to post memories. If the user's reputation points exceed the threshold, the method returns true, indicating that the user has permission to post memories; otherwise, it returns false. This functionality helps enforce restrictions on user actions based on their reputation, ensuring that users with low reputation points are limited in their ability to engage with certain features of the platform, while encouraging users to improve their reputation through positive contributions and engagement within the community.
    /// </summary>
    /// <param name="userId">The ID of the user whose permission to post memories is to be checked.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the user has permission to post memories.</returns>
    public async Task<bool> CanPostMemoriesAsync(int userId)
    {
        var reputationPoints = await this.GetReputationPointsAsync(userId);
        return reputationPoints > ReputationThresholds.PostMemories;
    }

    /// <summary>
    /// Asynchronously determines whether a specific user has the permission to post messages based on their reputation points. This method retrieves the user's current reputation points and compares them against a predefined threshold to determine if they are allowed to post messages. If the user's reputation points exceed the threshold, the method returns true, indicating that the user has permission to post messages; otherwise, it returns false. This functionality helps enforce restrictions on user actions based on their reputation, ensuring that users with low reputation points are limited in their ability to engage with certain features of the platform, while encouraging users to improve their reputation through positive contributions and engagement within the community.
    /// </summary>
    /// <param name="userId">The ID of the user whose permission to post messages is to be checked.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the user has permission to post messages.</returns>
    public async Task<bool> CanPostMessagesAsync(int userId)
    {
        var reputationPoints = await this.GetReputationPointsAsync(userId);
        return reputationPoints > ReputationThresholds.PostMessages;
    }

    /// <summary>
    /// Asynchronously determines whether a specific user has the permission to create events based on their reputation points. This method retrieves the user's current reputation points and compares them against a predefined threshold to determine if they are allowed to create events. If the user's reputation points exceed the threshold, the method returns true, indicating that the user has permission to create events; otherwise, it returns false. This functionality helps enforce restrictions on user actions based on their reputation, ensuring that users with low reputation points are limited in their ability to engage with certain features of the platform, while encouraging users to improve their reputation through positive contributions and engagement within the community.
    /// </summary>
    /// <param name="userId">The ID of the user whose permission to create events is to be checked.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the user has permission to create events.</returns>
    public async Task<bool> CanCreateEventsAsync(int userId)
    {
        var reputationPoints = await this.GetReputationPointsAsync(userId);
        return reputationPoints > ReputationThresholds.CreateEvents;
    }

    /// <summary>
    /// Asynchronously determines whether a specific user has the permission to attend events based on their reputation points. This method retrieves the user's current reputation points and compares them against a predefined threshold to determine if they are allowed to attend events. If the user's reputation points exceed the threshold, the method returns true, indicating that the user has permission to attend events; otherwise, it returns false. This functionality helps enforce restrictions on user actions based on their reputation, ensuring that users with low reputation points are limited in their ability to engage with certain features of the platform, while encouraging users to improve their reputation through positive contributions and engagement within the community.
    /// </summary>
    /// <param name="userId">The ID of the user whose permission to attend events is to be checked.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the user has permission to attend events.</returns>
    public async Task<bool> CanAttendEventsAsync(int userId)
    {
        var reputationPoints = await this.GetReputationPointsAsync(userId);
        return reputationPoints > ReputationThresholds.AttendEvents;
    }

    private async Task HandleReputationChangeAsync(ReputationMessage message)
    {
        try
        {
            if (message.Value == ReputationAction.EventAttended)
            {
                await this.HandleEventAttendedAsync(message.EventId!.Value);
                await this.achievementService.CheckAndAwardAchievementsAsync(message.UserId);
                return;
            }

            if (ReputationDeltasMap.TryGetValue(message.Value, out int delta))
            {
                var current = await this.reputationRepository.GetReputationPointsAsync(message.UserId);

                var newReputation = Math.Max(
                    ReputationConstants.MinReputation,
                    current + delta);

                var newTier = this.CalculateTier(newReputation);

                await this.reputationRepository.SetReputationAsync(message.UserId, newReputation, newTier);
                await this.achievementService.CheckAndAwardAchievementsAsync(message.UserId);
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Reputation update failed: {exception}");
        }
    }

    private async Task HandleEventAttendedAsync(int eventId)
    {
        var attendeeCount = await this.attendedEventRepository.GetAttendeeCountAsync(eventId);
        if (attendeeCount == 10)
        {
            var ev = await this.eventRepository.GetByIdAsync(eventId);
            if (ev?.Admin != null)
            {
                var current = await this.reputationRepository.GetReputationPointsAsync(ev.Admin.UserId);

                var newReputation = Math.Max(
                    ReputationConstants.MinReputation,
                    current + ReputationDeltas.EventAttendedAdminBonus);

                var newTier = this.CalculateTier(newReputation);

                await this.reputationRepository.SetReputationAsync(
                    ev.Admin.UserId,
                    newReputation,
                    newTier);
            }
        }
    }

    private string CalculateTier(int reputationPoints)
    {
        if (reputationPoints >= ReputationConstants.EventMasterThreshold)
        {
            return ReputationConstants.EventMasterTier;
        }

        if (reputationPoints >= ReputationConstants.CommunityLeaderThreshold)
        {
            return ReputationConstants.CommunityLeaderTier;
        }

        if (reputationPoints >= ReputationConstants.OrganizerThreshold)
        {
            return ReputationConstants.OrganizerTier;
        }

        if (reputationPoints >= ReputationConstants.ContributorThreshold)
        {
            return ReputationConstants.ContributorTier;
        }

        return ReputationConstants.NewcomerTier;
    }

    /// <summary>
    /// Defines the reputation point changes (deltas) associated with various user actions in the system. Each constant represents the number of reputation points to be added or subtracted from a user's total reputation based on specific actions they perform, such as creating an event, posting a discussion message, adding a memory, submitting a quest, and more. These deltas are used to calculate the new reputation points for users when they engage in different activities on the platform, allowing for dynamic updates to their reputation based on their contributions and interactions within the community.
    /// </summary>
    public static class ReputationDeltas
    {
        /// <summary>
        /// The reputation point change associated with the action of creating an event. When a user creates an event, they will receive a positive reputation boost of 5 points, reflecting their contribution to the community by organizing and hosting events. This delta encourages users to create events and actively participate in the platform's event ecosystem, rewarding them for their efforts in fostering community engagement and interaction through event creation.
        /// </summary>
        public const int EventCreated = 5;

        /// <summary>
        /// The reputation point change associated with the action of cancelling an event. When a user cancels an event, they will receive a negative reputation penalty of 20 points, reflecting the potential disruption and inconvenience caused to attendees and the community. This delta discourages users from cancelling events and encourages them to follow through with their commitments, promoting reliability and accountability within the platform's event ecosystem.
        /// </summary>
        public const int EventCancelled = -20;

        /// <summary>
        /// The reputation point change associated with the action of posting a discussion message. When a user posts a discussion message, they will receive a positive reputation boost of 1 point, reflecting their contribution to the community by engaging in discussions and sharing their thoughts and insights. This delta encourages users to actively participate in discussions and fosters a sense of community engagement, rewarding them for their contributions to the platform's discourse and interaction.
        /// </summary>
        public const int DiscussionMessagePosted = 1;

        /// <summary>
        /// The reputation point change associated with the action of having a discussion message removed by an administrator. When a user's discussion message is removed by an admin, they will receive a negative reputation penalty of 10 points, reflecting the potential violation of community guidelines or inappropriate content. This delta encourages users to adhere to community standards and promotes respectful and constructive communication within the platform's discussions, discouraging behavior that may lead to content removal by administrators.
        /// </summary>
        public const int DiscussionMessageRemovedByAdmin = -10;

        /// <summary>
        /// The reputation point change associated with the action of adding a memory with a photo. When a user adds a memory that includes a photo, they will receive a positive reputation boost of 4 points, reflecting their contribution to the community by sharing visual content and enriching the platform's collection of memories. This delta encourages users to share memories with photos, fostering a more engaging and visually appealing experience for the community, and rewarding them for their efforts in contributing meaningful and memorable content to the platform.
        /// </summary>
        public const int MemoryAddedWithPhoto = 4;

        /// <summary>
        /// The reputation point change associated with the action of adding a memory without a photo (text-only). When a user adds a memory that does not include a photo, they will receive a positive reputation boost of 1 point, reflecting their contribution to the community by sharing their experiences and stories through text. This delta encourages users to share memories even if they do not have accompanying photos, fostering a more inclusive and diverse range of content on the platform, and rewarding them for their efforts in contributing meaningful narratives and reflections to the community's collection of memories.
        /// </summary>
        public const int MemoryAddedTextOnly = 1;

        /// <summary>
        /// The reputation point change associated with the action of submitting a quest. When a user submits a quest, they will receive a positive reputation boost of 6 points, reflecting their contribution to the community by creating and sharing quests for others to engage with. This delta encourages users to submit quests, fostering creativity and interaction within the platform's quest ecosystem, and rewarding them for their efforts in contributing engaging and challenging content for the community to enjoy.
        /// </summary>
        public const int QuestSubmitted = 6;

        /// <summary>
        /// The reputation point change associated with the action of having a quest approved. When a user's submitted quest is approved by an administrator, they will receive a positive reputation boost of 10 points, reflecting their contribution to the community by creating a quest that meets the platform's standards and guidelines. This delta encourages users to submit high-quality quests that are likely to be approved, fostering creativity and engagement within the platform's quest ecosystem, and rewarding them for their efforts in contributing valuable and enjoyable content for the community to participate in.
        /// </summary>
        public const int QuestApproved = 10;

        /// <summary>
        /// The reputation point change associated with the action of having a quest denied. When a user's submitted quest is denied by an administrator, they will receive a negative reputation penalty of 4 points, reflecting the potential issues with the quest's content, quality, or adherence to guidelines. This delta encourages users to submit well-crafted and appropriate quests that are more likely to be approved, fostering a higher standard of content within the platform's quest ecosystem, and discouraging submissions that may not meet the community's expectations or guidelines for quests.
        /// </summary>
        public const int QuestDenied = -4;

        /// <summary>
        /// The reputation point change associated with the action of attending an event, specifically for the event administrator when the event reaches a milestone of 10 attendees. When an event reaches 10 attendees, the administrator of that event will receive a positive reputation boost of 20 points, reflecting their successful organization and hosting of a well-attended event. This delta encourages administrators to create engaging and popular events that attract attendees, fostering community engagement and interaction through events, and rewarding them for their efforts in contributing to the platform's vibrant event ecosystem.
        /// </summary>
        public const int EventAttendedAdminBonus = 20;
    }

    /// <summary>
    /// Defines the reputation thresholds for different user tiers in the system, determining the minimum reputation points required for users to be classified into specific tiers such as Newcomer, Contributor, Organizer, Community Leader, and Event Master. These thresholds are used to categorize users based on their reputation points, allowing for a tiered system that reflects their level of engagement and contribution to the community. The constants defined in this class provide a clear and maintainable way to manage the tier thresholds, enabling easy adjustments as needed to balance the reputation system and encourage user participation and growth within the platform.
    /// </summary>
    public static class ReputationConstants
    {
        /// <summary>
        /// The minimum reputation points that a user can have in the system. This constant defines the lower bound for reputation points, ensuring that users cannot have a negative reputation below this threshold. It serves as a safeguard to prevent excessively negative reputation values, which could impact user experience and engagement on the platform. By setting a minimum reputation, the system can maintain a more balanced and fair reputation management approach, allowing users to recover from negative actions without being permanently penalized with an unmanageable reputation score.
        /// </summary>
        public const int MinReputation = -1000;

        /// <summary>
        /// The reputation point threshold for the "Contributor" tier. Users with reputation points equal to or greater than this threshold will be classified as Contributors, reflecting their active participation and contributions to the community. This threshold encourages users to engage with the platform and contribute positively, rewarding them with a higher tier that may come with additional privileges or recognition within the community. By setting clear thresholds for each tier, the system can motivate users to strive for higher levels of engagement and contribution, fostering a more vibrant and active community environment.
        /// </summary>
        public const int ContributorThreshold = 50;

        /// <summary>
        /// The reputation point threshold for the "Organizer" tier. Users with reputation points equal to or greater than this threshold will be classified as Organizers, reflecting their significant contributions and leadership within the community. This threshold encourages users to take on more active roles in organizing events, leading discussions, and contributing to the growth of the platform, rewarding them with a higher tier that may come with additional privileges or recognition. By defining clear thresholds for each tier, the system can motivate users to increase their engagement and contributions, fostering a more dynamic and collaborative community environment.
        /// </summary>
        public const int OrganizerThreshold = 200;

        /// <summary>
        /// The reputation point threshold for the "Community Leader" tier. Users with reputation points equal to or greater than this threshold will be classified as Community Leaders, reflecting their exceptional contributions, leadership, and influence within the community. This threshold encourages users to become highly engaged and influential members of the platform, rewarding them with a prestigious tier that may come with exclusive privileges or recognition. By establishing clear thresholds for each tier, the system can inspire users to actively contribute and lead within the community, fostering a more vibrant and supportive environment for all members.
        /// </summary>
        public const int CommunityLeaderThreshold = 500;

        /// <summary>
        /// The reputation point threshold for the "Event Master" tier. Users with reputation points equal to or greater than this threshold will be classified as Event Masters, reflecting their outstanding contributions, leadership, and influence in organizing and hosting events within the community. This threshold encourages users to excel in event organization and community engagement, rewarding them with the highest tier that may come with exclusive privileges or recognition. By defining clear thresholds for each tier, the system can motivate users to strive for excellence in their contributions and leadership, fostering a more dynamic and thriving community environment where users are encouraged to take on active roles in shaping the platform's culture and activities.
        /// </summary>
        public const int EventMasterThreshold = 1000;

        /// <summary>
        /// The tier name for users who do not have any recorded reputation points or whose reputation points fall below the Contributor threshold. This constant represents the default tier assigned to new users or those with low engagement, categorizing them as "Newcomers" in the community. By defining this tier, the system can provide a clear starting point for users as they begin their journey on the platform, encouraging them to engage and contribute in order to progress to higher tiers with additional privileges and recognition. The Newcomer tier serves as an entry-level classification that motivates users to become more active and involved in the community, fostering growth and participation from the outset of their experience on the platform.
        /// </summary>
        public const string NewcomerTier = "Newcomer";

        /// <summary>
        /// The tier name for users who have achieved a certain level of reputation points, specifically those who have reached or exceeded the Contributor threshold. This constant represents the "Contributor" tier, which signifies that users in this category have made meaningful contributions to the community through their engagement and participation. By defining this tier, the system can recognize and reward users for their efforts, encouraging them to continue contributing and engaging with the platform to reach even higher tiers with additional privileges and recognition. The Contributor tier serves as an important milestone for users, motivating them to actively participate and contribute to the growth and vibrancy of the community.
        /// </summary>
        public const string ContributorTier = "Contributor";

        /// <summary>
        /// The tier name for users who have achieved a significant level of reputation points, specifically those who have reached or exceeded the Organizer threshold. This constant represents the "Organizer" tier, which signifies that users in this category have demonstrated leadership and a strong commitment to the community through their contributions and engagement. By defining this tier, the system can recognize and reward users for their efforts in organizing events, leading discussions, and contributing to the growth of the platform, encouraging them to continue their active involvement to reach even higher tiers with additional privileges and recognition. The Organizer tier serves as a prestigious classification that motivates users to take on more active roles in shaping the community and fostering a collaborative environment for all members.
        /// </summary>
        public const string OrganizerTier = "Organizer";

        /// <summary>
        /// The tier name for users who have achieved an exceptional level of reputation points, specifically those who have reached or exceeded the Community Leader threshold. This constant represents the "Community Leader" tier, which signifies that users in this category have made outstanding contributions, demonstrated leadership, and exerted significant influence within the community. By defining this tier, the system can recognize and reward users for their exceptional engagement and contributions, encouraging them to continue their active involvement to reach the highest tier with additional privileges and recognition. The Community Leader tier serves as a prestigious classification that motivates users to excel in their contributions and leadership, fostering a more dynamic and thriving community environment where users are encouraged to take on active roles in shaping the platform's culture and activities.
        /// </summary>
        public const string CommunityLeaderTier = "Community Leader";

        /// <summary>
        /// The tier name for users who have achieved the highest level of reputation points, specifically those who have reached or exceeded the Event Master threshold. This constant represents the "Event Master" tier, which signifies that users in this category have made outstanding contributions, demonstrated exceptional leadership, and exerted significant influence in organizing and hosting events within the community. By defining this tier, the system can recognize and reward users for their exceptional engagement and contributions in event organization, encouraging them to continue their active involvement to maintain their status and inspire others to strive for excellence in their contributions and leadership. The Event Master tier serves as the pinnacle classification that motivates users to excel in their contributions and leadership, fostering a more dynamic and thriving community environment where users are encouraged to take on active roles in shaping the platform's culture and activities through event organization and community engagement.
        /// </summary>
        public const string EventMasterTier = "Event Master";
    }

    /// <summary>
    /// Defines the reputation thresholds for various user actions, determining the minimum reputation points required for users to perform specific actions such as posting memories, posting messages, creating events, and attending events. These thresholds are used to enforce restrictions on user actions based on their reputation, ensuring that users with low reputation points are limited in their ability to engage with certain features of the platform. The thresholds are defined as constants, allowing for easy configuration and maintenance of the reputation system within the application.
    /// </summary>
    private static class ReputationThresholds
    {
        public const int PostMemories = -300;
        public const int PostMessages = -500;
        public const int CreateEvents = -700;
        public const int AttendEvents = -1000;
    }
}
