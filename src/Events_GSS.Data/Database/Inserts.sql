USE ISSEvents

DELETE FROM AnnouncementReactions;
DELETE FROM AnnouncementReadReceipts;
DELETE FROM Announcements;
DELETE FROM DiscussionReactions;
DELETE FROM DiscussionMutes;
DELETE FROM Discussions;
DELETE FROM MemoryLikes;
DELETE FROM Memories;
DELETE FROM QuestMemories;
DELETE FROM Quests;
DELETE FROM AttendedEvents;
DELETE FROM Events;
DELETE FROM users_RP_scores;
DELETE FROM Users;

DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Events', RESEED, 0);
DBCC CHECKIDENT ('Memories', RESEED, 0);
DBCC CHECKIDENT ('Announcements', RESEED, 0);
DBCC CHECKIDENT ('Discussions', RESEED, 0);
DBCC CHECKIDENT ('Quests', RESEED, 0);
DBCC CHECKIDENT ('QuestMemories', RESEED, 0);

SET IDENTITY_INSERT Users ON;

INSERT INTO Users (Id, Name, Email, PasswordHash)
VALUES 
(1, 'Alice Admin', 'alice@test.com', 'hash123'),
(2, 'Bob User', 'bob@test.com', 'hash456'),
(3, 'Carol User', 'carol@test.com', 'hash789'),
(4, 'Dan Gheorghe', 'dan@test.com', 'blah'),
(5, 'Elena Moldovan', 'elena@test.com', 'blah'),
(6, 'Florin Stanescu', 'florin@test.com', 'blah');

SET IDENTITY_INSERT Users OFF;

-- Seed reputation scores
INSERT INTO users_RP_scores (UserId, ReputationPoints, Tier) VALUES
(1, 340, 'Organizer'),
(2, 120, 'Contributor'),
(3,  75, 'Contributor'),
(4, 510, 'Community Leader'),
(5,   0, 'Newcomer'),
(6, -50, 'Newcomer');
 
SET IDENTITY_INSERT Events ON;

INSERT INTO Events (EventId, Name, Location, StartDateTime, EndDateTime, IsPublic, Description, MaximumPeople, EventBannerPath, CategoryId, AdminId, SlowModeSeconds)
VALUES
(1, 'Test Event Cluj',  'Cluj-Napoca',  '2026-06-01 10:00', '2026-06-01 20:00', 1, 'A test event in Cluj',    20, NULL, 1, 1, NULL),
(2, 'Book Meet',        'Cluj-Napoca',  '2026-07-15 10:00', '2026-07-15 14:00', 1, 'A meeting about books',   10, NULL, 5, 1, NULL),
(3, 'Art Club',         'Bucharest',    '2026-08-01 10:00', '2026-08-02 15:00', 1, 'A meeting about art',     10, NULL, 5, 1, NULL);

SET IDENTITY_INSERT Events OFF;

INSERT INTO AttendedEvents (EventId, UserId)
VALUES (1, 1), (1, 2), (1, 3),
       (2, 1), (2, 2),
       (3, 1), (3, 3);

INSERT INTO Memories (EventId, UserId, Text, CreatedAt)
VALUES (1, 2, 'Ce seara frumoasa a fost!', GETUTCDATE());

INSERT INTO Memories (EventId, UserId, PhotoPath, CreatedAt)
VALUES (1, 3, 'C:\Users\test\Pictures\photo1.jpg', GETUTCDATE());

INSERT INTO Memories (EventId, UserId, PhotoPath, Text, CreatedAt)
VALUES (1, 1, 'C:\Users\test\Pictures\photo2.jpg', 'O amintire draguta!', GETUTCDATE());

INSERT INTO MemoryLikes (MemoryId, UserId) VALUES (2, 2);
INSERT INTO MemoryLikes (MemoryId, UserId) VALUES (1, 1);

INSERT INTO Announcements (EventId, UserId, Message, Date, IsPinned, IsEdited)
VALUES
(1, 1, 'Welcome to the event! Please read the rules.', GETUTCDATE(), 1, 0),
(1, 1, 'Reminder: event starts at 10 AM sharp. Bring your own laptop if you want to participate in the coding session.', DATEADD(MINUTE, 5, GETUTCDATE()), 0, 0);

INSERT INTO Discussions (EventId, UserId, Message, Date, IsEdited)
VALUES
(1, 2, 'Hey everyone! Excited for this event!', GETUTCDATE(), 0),
(1, 3, 'Me too! @Bob User are you bringing snacks?', DATEADD(MINUTE, 1, GETUTCDATE()), 0),
(1, 1, 'Welcome all! Feel free to ask questions here.', DATEADD(MINUTE, 2, GETUTCDATE()), 0);

INSERT INTO Quests (EventId, [Name], Description, Difficulty)
VALUES
(1, 'Find the Hidden Code', 'There is a hidden code somewhere in the event. Find it and share it in the discussion!', 5),
(1, 'Photo Challenge', 'Take a creative photo at the event and share it in the memories!', 2),
(2, 'Book Trivia', 'Answer trivia question: who inspired the worldbuildinng of the book?.', 1),
(3, 'Flower Master', 'Find and share a beautiful photo of flowers.', 3);

INSERT INTO QuestMemories (QuestId, MemoryId) VALUES (1, 1);