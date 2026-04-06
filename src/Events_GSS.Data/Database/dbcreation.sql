
Use master;
GO

IF DB_ID('ISSEvents') IS NOT NULL
BEGIN
    ALTER DATABASE ISSEvents SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ISSEvents;
END
GO

CREATE DATABASE ISSEvents;
GO

USE ISSEvents;
GO

-- ============================================================
-- 1. CATEGORIES
-- ============================================================
CREATE TABLE Categories (
    CategoryId   INT           NOT NULL IDENTITY(1,1),
    Title        NVARCHAR(100) NOT NULL,

    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_Title UNIQUE (Title)
);

INSERT INTO Categories (Title)
VALUES ('NATURE'), ('FITNESS'), ('MUSIC'), ('SOCIAL'),
       ('ART'),    ('PETS'),    ('TECH'),  ('FUN');


-- ============================================================
-- 2. USERS
-- ============================================================
CREATE TABLE Users (
    Id                INT           NOT NULL IDENTITY(1,1),
    Name              NVARCHAR(100) NOT NULL,
    Email             NVARCHAR(254) NOT NULL,
    PasswordHash      NVARCHAR(512) NOT NULL,

    CONSTRAINT PK_Users          PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email)
);

-- Seed users
SET IDENTITY_INSERT Users ON;
INSERT INTO Users (Id, Name, Email, PasswordHash) VALUES
    (1, 'Alice Popescu',      'alice@test.com',      'hash1'),
    (2, 'Bob Ionescu',        'bob@test.com',        'hash2'),
    (3, 'Carol Popa',         'carol@test.com',      'hash3'),
    (4, 'Dan Gheorghe',       'dan@test.com',        'hash4'),
    (5, 'Elena Moldovan',     'elena@test.com',      'hash5'),
    (6, 'Florin Stanescu',    'florin@test.com',      'hash6'),
    (7, 'TestUser Penalized', 'penalized@test.com',  'hash7');
SET IDENTITY_INSERT Users OFF;
GO


-- ============================================================
-- 2b. USERS_RP_SCORES  (reputation kept separate from core Users)
-- ============================================================
CREATE TABLE users_RP_scores (
    UserId            INT           NOT NULL,
    ReputationPoints  INT           NOT NULL DEFAULT 0,
    Tier              NVARCHAR(50)  NOT NULL DEFAULT 'Newcomer',

    CONSTRAINT PK_users_RP_scores       PRIMARY KEY (UserId),
    CONSTRAINT FK_users_RP_scores_User  FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
    CONSTRAINT CK_users_RP_Floor        CHECK (ReputationPoints >= -1000),
    CONSTRAINT CK_users_RP_Tier         CHECK (Tier IN (
        'Newcomer', 'Contributor', 'Organizer', 'Community Leader', 'Event Master'
    ))
);

-- Seed RP scores
INSERT INTO users_RP_scores (UserId, ReputationPoints, Tier) VALUES
    (1,  340,   'Organizer'),
    (2,  120,   'Contributor'),
    (3,   75,   'Contributor'),
    (4,  510,   'Community Leader'),
    (5,    0,   'Newcomer'),
    (6,  -50,   'Newcomer'),
    (7, -1000,  'Newcomer');
GO


-- ============================================================
-- 3. EVENTS
-- ============================================================
CREATE TABLE Events (
    EventId         INT            NOT NULL IDENTITY(1,1),
    Name            NVARCHAR(200)  NOT NULL,
    Location        NVARCHAR(200)  NOT NULL,
    StartDateTime   DATETIME2      NOT NULL,
    EndDateTime     DATETIME2      NOT NULL,
    IsPublic        BIT            NOT NULL DEFAULT 1,
    Description     NVARCHAR(2000) NULL,
    MaximumPeople   INT            NULL,          -- NULL = no limit
    EventBannerPath NVARCHAR(500)  NULL,
    CategoryId      INT            NULL,
    AdminId       INT            NOT NULL,
    -- Per-event slow-mode for discussions; NULL = slow mode off
    SlowModeSeconds INT            NULL,

    CONSTRAINT PK_Events               PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Category      FOREIGN KEY (CategoryId)  REFERENCES Categories (CategoryId),
    CONSTRAINT FK_Events_AdminId       FOREIGN KEY (AdminId)   REFERENCES Users      (Id),
    CONSTRAINT CK_Events_Dates         CHECK (EndDateTime > StartDateTime),
    CONSTRAINT CK_Events_MaxPeople     CHECK (MaximumPeople IS NULL OR MaximumPeople > 0),
    CONSTRAINT CK_Events_SlowMode      CHECK (SlowModeSeconds IS NULL OR SlowModeSeconds > 0)
);


-- ============================================================
-- 4. ATTENDED EVENTS  (junction: Users <-> Events)
-- ============================================================
CREATE TABLE AttendedEvents (
    EventId      INT       NOT NULL,
    UserId       INT       NOT NULL,
    EnrollmentDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsArchived   BIT       NOT NULL DEFAULT 0,
    IsFavourite  BIT       NOT NULL DEFAULT 0,

    CONSTRAINT PK_AttendedEvents         PRIMARY KEY (EventId, UserId),
    CONSTRAINT FK_AttendedEvents_Event   FOREIGN KEY (EventId) REFERENCES Events (EventId) ON DELETE CASCADE,
    CONSTRAINT FK_AttendedEvents_User    FOREIGN KEY (UserId)  REFERENCES Users  (Id)      ON DELETE CASCADE
);


-- ============================================================
-- 5. ANNOUNCEMENTS
-- ============================================================
CREATE TABLE Announcements (
    AnnId    INT            NOT NULL IDENTITY(1,1),
    EventId  INT            NOT NULL,
    UserId   INT            NOT NULL,   -- author (EventAdmin)
    Message  NVARCHAR(MAX)  NOT NULL,
    Date     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    IsPinned BIT            NOT NULL DEFAULT 0,
    IsEdited BIT            NOT NULL DEFAULT 0,

    CONSTRAINT PK_Announcements        PRIMARY KEY (AnnId),
    CONSTRAINT FK_Announcements_Event  FOREIGN KEY (EventId) REFERENCES Events (EventId) ON DELETE CASCADE,
    CONSTRAINT FK_Announcements_User   FOREIGN KEY (UserId)  REFERENCES Users  (Id)
);

-- Enforces at most one pinned announcement per event (filtered unique index)
CREATE UNIQUE INDEX UX_Announcements_OnePinPerEvent
    ON Announcements (EventId)
    WHERE IsPinned = 1;


-- ============================================================
-- 6. ANNOUNCEMENT READ RECEIPTS
-- ============================================================
CREATE TABLE AnnouncementReadReceipts (
    Id             INT       NOT NULL IDENTITY(1,1),
    AnnouncementId INT       NOT NULL,
    UserId         INT       NOT NULL,
    ReadAt         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AnnReadReceipts          PRIMARY KEY (Id),
    CONSTRAINT FK_AnnReadReceipts_Ann      FOREIGN KEY (AnnouncementId) REFERENCES Announcements (AnnId) ON DELETE CASCADE,
    CONSTRAINT FK_AnnReadReceipts_User     FOREIGN KEY (UserId)         REFERENCES Users         (Id)    ON DELETE CASCADE,
    CONSTRAINT UQ_AnnReadReceipts_UserAnn  UNIQUE (AnnouncementId, UserId)
);


-- ============================================================
-- 7. ANNOUNCEMENT REACTIONS
-- ============================================================
CREATE TABLE AnnouncementReactions (
    Id             INT           NOT NULL IDENTITY(1,1),
    AnnouncementId INT           NOT NULL,
    UserId         INT           NOT NULL,
    Emoji          NVARCHAR(10)  NOT NULL,

    CONSTRAINT PK_AnnReactions           PRIMARY KEY (Id),
    CONSTRAINT FK_AnnReactions_Ann       FOREIGN KEY (AnnouncementId) REFERENCES Announcements (AnnId) ON DELETE CASCADE,
    CONSTRAINT FK_AnnReactions_User      FOREIGN KEY (UserId)         REFERENCES Users         (Id)    ON DELETE CASCADE,
    -- One reaction per user per announcement (can change emoji but not add a second)
    CONSTRAINT UQ_AnnReactions_UserAnn   UNIQUE (AnnouncementId, UserId)
);


-- ============================================================
-- 8. DISCUSSIONS
-- ============================================================
CREATE TABLE Discussions (
    DiscussionId  INT            NOT NULL IDENTITY(1,1),
    EventId       INT            NOT NULL,
    UserId        INT            NOT NULL,
    Message       NVARCHAR(MAX)  NULL,       -- NULL allowed when MediaPath is provided
    MediaPath     NVARCHAR(500)  NULL,
    Date          DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ReplyToId     INT            NULL,       -- self-referencing FK for threaded replies
    IsEdited      BIT            NOT NULL DEFAULT 0,

    CONSTRAINT PK_Discussions          PRIMARY KEY (DiscussionId),
    CONSTRAINT FK_Discussions_Event    FOREIGN KEY (EventId)    REFERENCES Events      (EventId) ON DELETE CASCADE,
    CONSTRAINT FK_Discussions_User     FOREIGN KEY (UserId)     REFERENCES Users       (Id),
    CONSTRAINT FK_Discussions_ReplyTo  FOREIGN KEY (ReplyToId)  REFERENCES Discussions (DiscussionId),
    -- A message must have at least a text body or a media attachment
    CONSTRAINT CK_Discussions_Content  CHECK (
        (Message IS NOT NULL AND LEN(LTRIM(RTRIM(Message))) > 0)
        OR MediaPath IS NOT NULL
    )
);


-- ============================================================
-- 9. DISCUSSION REACTIONS
-- ============================================================
CREATE TABLE DiscussionReactions (
    Id           INT           NOT NULL IDENTITY(1,1),
    MessageId    INT           NOT NULL,
    UserId       INT           NOT NULL,
    Emoji        NVARCHAR(10)  NOT NULL,

    CONSTRAINT PK_DiscReactions          PRIMARY KEY (Id),
    CONSTRAINT FK_DiscReactions_Message  FOREIGN KEY (MessageId) REFERENCES Discussions (DiscussionId) ON DELETE CASCADE,
    CONSTRAINT FK_DiscReactions_User     FOREIGN KEY (UserId)    REFERENCES Users       (Id)           ON DELETE CASCADE,
    CONSTRAINT UQ_DiscReactions_UserMsg  UNIQUE (MessageId, UserId)
);


-- ============================================================
-- 10. DISCUSSION MUTES
-- ============================================================
CREATE TABLE DiscussionMutes (
    Id            INT       NOT NULL IDENTITY(1,1),
    EventId       INT       NOT NULL,
    MutedUserId   INT       NOT NULL,
    MutedByUserId INT       NOT NULL,
    MutedUntil    DATETIME2 NULL,    -- NULL when IsPermanent = 1
    IsPermanent   BIT       NOT NULL DEFAULT 0,
    CreatedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_DiscMutes            PRIMARY KEY (Id),
    CONSTRAINT FK_DiscMutes_Event      FOREIGN KEY (EventId)       REFERENCES Events (EventId) ON DELETE CASCADE,
    CONSTRAINT FK_DiscMutes_MutedUser  FOREIGN KEY (MutedUserId)   REFERENCES Users  (Id),
    CONSTRAINT FK_DiscMutes_MutedBy    FOREIGN KEY (MutedByUserId) REFERENCES Users  (Id),
    -- Only one active mute record per user per event
    CONSTRAINT UQ_DiscMutes_UserEvent  UNIQUE (EventId, MutedUserId),
    CONSTRAINT CK_DiscMutes_Duration   CHECK (
        (IsPermanent = 1 AND MutedUntil IS NULL)
        OR (IsPermanent = 0 AND MutedUntil IS NOT NULL)
    )
);


-- ============================================================
-- 12. QUESTS  (event-specific instances)
-- ============================================================
CREATE TABLE Quests (
    QuestId              INT            NOT NULL IDENTITY(1,1),
    EventId              INT            NOT NULL,
    Name                 NVARCHAR(200)  NOT NULL,
    Description          NVARCHAR(MAX)  NOT NULL,
    Difficulty           INT            NOT NULL,
    PrerequisiteQuestId  INT            NULL,   -- locking system

    CONSTRAINT PK_Quests                  PRIMARY KEY (QuestId),
    CONSTRAINT FK_Quests_Event            FOREIGN KEY (EventId)             REFERENCES Events (EventId) ON DELETE CASCADE,
    CONSTRAINT FK_Quests_Prerequisite     FOREIGN KEY (PrerequisiteQuestId) REFERENCES Quests (QuestId),
    CONSTRAINT CK_Quests_Difficulty       CHECK (Difficulty BETWEEN 1 AND 5),
    -- A quest cannot be its own prerequisite
    CONSTRAINT CK_Quests_NoSelfPrereq     CHECK (PrerequisiteQuestId <> QuestId)
);


-- ============================================================
-- 13. MEMORIES
-- ============================================================
CREATE TABLE Memories (
    MemoryId   INT            NOT NULL IDENTITY(1,1),
    EventId    INT            NOT NULL,
    UserId     INT            NOT NULL,
    PhotoPath  NVARCHAR(500)  NULL,
    Text       NVARCHAR(MAX)  NULL,
    CreatedAt  DATETIME2      NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Memories         PRIMARY KEY (MemoryId),
    CONSTRAINT FK_Memories_Event   FOREIGN KEY (EventId) REFERENCES Events (EventId) ON DELETE CASCADE,
    CONSTRAINT FK_Memories_User    FOREIGN KEY (UserId)  REFERENCES Users  (Id),
    -- Must have at least a photo or text
    CONSTRAINT CK_Memories_Content CHECK (
        PhotoPath IS NOT NULL OR (Text IS NOT NULL AND LEN(LTRIM(RTRIM(Text))) > 0)
    )
);


-- ============================================================
-- 14. QUEST MEMORIES  (proof submissions)
--     Status: 'Submitted' | 'Approved' | 'Rejected'
-- ============================================================
CREATE TABLE QuestMemories (
    QuestMemoryId  INT           NOT NULL IDENTITY(1,1),
    QuestId        INT           NOT NULL,
    MemoryId       INT           NOT NULL,
    Status         NVARCHAR(20)  NOT NULL DEFAULT 'Submitted',

    CONSTRAINT PK_QuestMemories          PRIMARY KEY (QuestMemoryId),
    CONSTRAINT FK_QuestMemories_Quest    FOREIGN KEY (QuestId)  REFERENCES Quests  (QuestId),
    CONSTRAINT FK_QuestMemories_Memory   FOREIGN KEY (MemoryId) REFERENCES Memories (MemoryId) ON DELETE CASCADE ,
    CONSTRAINT UQ_QuestMemories_Pair     UNIQUE (QuestId, MemoryId),
    CONSTRAINT CK_QuestMemories_Status   CHECK (Status IN ('Submitted', 'Approved', 'Rejected'))
);


-- ============================================================
-- 15. MEMORY LIKES
-- ============================================================
CREATE TABLE MemoryLikes (
    MemoryId  INT NOT NULL,
    UserId    INT NOT NULL,

    CONSTRAINT PK_MemoryLikes          PRIMARY KEY (MemoryId, UserId),
    CONSTRAINT FK_MemoryLikes_Memory   FOREIGN KEY (MemoryId) REFERENCES Memories (MemoryId) ON DELETE CASCADE,
    CONSTRAINT FK_MemoryLikes_User     FOREIGN KEY (UserId)   REFERENCES Users    (Id)       ON DELETE CASCADE
);


-- ============================================================
-- 16. ACHIEVEMENTS  (static lookup)
-- ============================================================
CREATE TABLE Achievements (
    Id          INT            NOT NULL IDENTITY(1,1),
    Title       NVARCHAR(100)  NOT NULL,
    Description NVARCHAR(500)  NOT NULL,

    CONSTRAINT PK_Achievements       PRIMARY KEY (Id),
    CONSTRAINT UQ_Achievements_Title UNIQUE (Title)
);

-- Seed the 10 achievements defined in REQ-9.4
INSERT INTO Achievements (Title, Description) VALUES
    ('First Steps',              'Confirm attendance to an event for the first time.'),
    ('Proper Host',              'Create 3 events.'),
    ('Distinguished Gentleperson','Create 10 events.'),
    ('Quest Solver',             'Complete 25 approved quests.'),
    ('Quest Master',             'Complete 75 approved quests.'),
    ('Quest Champion',           'Complete 150 approved quests.'),
    ('Memory Keeper',            'Add 50 memories with photos.'),
    ('Social Butterfly',         'Post 100 discussion messages.'),
    ('Event Veteran',            'Attend 10 different events.'),
    ('Perfectionist',            'Achieve 100% quest completion in a single event.');


-- ============================================================
-- 17. USER ACHIEVEMENTS
-- ============================================================
CREATE TABLE UserAchievements (
    Id            INT       NOT NULL IDENTITY(1,1),
    UserId        INT       NOT NULL,
    AchievementId INT       NOT NULL,
    UnlockedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_UserAchievements         PRIMARY KEY (Id),
    CONSTRAINT FK_UserAchievements_User    FOREIGN KEY (UserId)        REFERENCES Users        (Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserAchievements_Achiev  FOREIGN KEY (AchievementId) REFERENCES Achievements (Id),
    -- Achievements are permanent and can only be earned once
    CONSTRAINT UQ_UserAchievements_Pair    UNIQUE (UserId, AchievementId)
);


-- ============================================================
-- 18. NOTIFICATIONS
-- ============================================================
CREATE TABLE Notifications (
    Id          INT            NOT NULL IDENTITY(1,1),
    UserId      INT            NOT NULL,
    Title       NVARCHAR(200)  NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    CreatedAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    IsRead      BIT            NOT NULL DEFAULT 0,

    CONSTRAINT PK_Notifications       PRIMARY KEY (Id),
    CONSTRAINT FK_Notifications_User  FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
);


-- ============================================================
-- INDEXES  (beyond the unique constraints above)
-- ============================================================

-- Events: frequently filtered/sorted by date, category, creator
CREATE INDEX IX_Events_StartDateTime ON Events (StartDateTime);
CREATE INDEX IX_Events_CategoryId    ON Events (CategoryId);
CREATE INDEX IX_Events_AdminId       ON Events (AdminId);

-- AttendedEvents: look up all events for a user
CREATE INDEX IX_AttendedEvents_UserId  ON AttendedEvents (UserId);

-- Announcements: always queried by event, ordered by date
CREATE INDEX IX_Announcements_EventId  ON Announcements (EventId, Date DESC);

-- Discussions: queried by event chronologically
CREATE INDEX IX_Discussions_EventId    ON Discussions (EventId, Date ASC);
CREATE INDEX IX_Discussions_ReplyToId  ON Discussions (ReplyToId) WHERE ReplyToId IS NOT NULL;

-- Memories: queried by event and by user
CREATE INDEX IX_Memories_EventId   ON Memories (EventId, CreatedAt DESC);
CREATE INDEX IX_Memories_UserId    ON Memories (UserId);

-- Quests: queried by event
CREATE INDEX IX_Quests_EventId     ON Quests (EventId);

-- QuestMemories: queried by quest (admin review) and by user memory
CREATE INDEX IX_QuestMemories_QuestId   ON QuestMemories (QuestId);
CREATE INDEX IX_QuestMemories_MemoryId  ON QuestMemories (MemoryId);

-- Notifications: always queried per user, newest first
CREATE INDEX IX_Notifications_UserId   ON Notifications (UserId, CreatedAt DESC);

-- UserAchievements: look up all achievements for a user
CREATE INDEX IX_UserAchievements_UserId ON UserAchievements (UserId);

-- DiscussionMutes: service checks per event+user on every post
CREATE INDEX IX_DiscMutes_EventUser ON DiscussionMutes (EventId, MutedUserId);