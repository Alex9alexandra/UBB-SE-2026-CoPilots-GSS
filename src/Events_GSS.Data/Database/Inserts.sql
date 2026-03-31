USE ISSEvents

DELETE FROM MemoryLikes;
DELETE FROM Memories;
DELETE FROM AttendedEvents;
DELETE FROM Events;
DELETE FROM Users;

-- Reset identity to start again from 1
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Events', RESEED, 0);
DBCC CHECKIDENT ('Memories', RESEED, 0);

SET IDENTITY_INSERT Users ON;

-- User 1 (va fi admin la event)
INSERT INTO Users (Id, Name, Email, PasswordHash, ReputationPoints)
VALUES (1, 'Alice Admin', '[alice@test.com](mailto:alice@test.com)', 'hash123', 340);

-- User 2 (user normal)
INSERT INTO Users (Id, Name, Email, PasswordHash, ReputationPoints)
VALUES (2, 'Bob User', '[bob@test.com](mailto:bob@test.com)', 'hash456', 120);

-- User 3 (alt user normal)
INSERT INTO Users (Id, Name, Email, PasswordHash, ReputationPoints)
VALUES (3, 'Carol User', '[carol@test.com](mailto:carol@test.com)', 'hash789', 75);

INSERT INTO Users (Id, Name, Email, PasswordHash, ReputationPoints)
VALUES
(4, 'Dan Gheorghe', '[dan@test.com](mailto:dan@test.com)', 'blah', 510),
(5, 'Elena Moldovan', '[elena@test.com](mailto:elena@test.com)', 'blah', 0),
(6, 'Florin Stanescu', '[florin@test.com](mailto:florin@test.com)', 'blah', -50);

SET IDENTITY_INSERT Users OFF;

SET IDENTITY_INSERT Events ON;

-- Event creat de Alice (Id=1 e admin)
INSERT INTO Events (
    EventId,
    Name,
    Location,
    StartDateTime,
    EndDateTime,
    IsPublic,
    Description,
    MaximumPeople,
    EventBannerPath,
    CategoryId,
    AdminId,
    -- Per-event slow-mode for discussions; NULL = slow mode off
    SlowModeSeconds
    )
VALUES 
(1, 'Test Event Cluj', 'RO', '2026-04-01 10:00', '2026-04-01 20:00', 1, 'dhfs', 20, 'sudb', 1, 1, 1),
(2, 'Book Meet', 'Cluj-Napoca', '2025-09-15 10:00', '2025-09-15 14:00', 1, 'A meeting about books', 10, 'bannerpath',  5, 1, NULL),
(3, 'Art Club', 'Bucharest', '2026-03-31 10:00', '2026-04-01 15:00', 1, 'A meeting about art', 10, 'bannerpath',  5, 1, NULL);

SET IDENTITY_INSERT Events OFF;

-- Toti trei joined la event
INSERT INTO AttendedEvents (EventId, UserId)
VALUES (1, 1), (1, 2), (1, 3), (2, 1), (2, 2), (3, 1), (3, 3);

-- Memories
-- Memory cu doar text (de la Bob)
INSERT INTO Memories (EventId, UserId, Text, CreatedAt)
VALUES (1, 2, 'Ce seara frumoasa a fost!', GETUTCDATE());

-- Memory cu doar poza (de la Carol)
INSERT INTO Memories (EventId, UserId, PhotoPath, CreatedAt)
VALUES (1, 3, 'C:\Users\test\Pictures\photo1.jpg', GETUTCDATE());

-- Memory cu text si poza (de la Alice)
INSERT INTO Memories (EventId, UserId, PhotoPath, Text, CreatedAt)
VALUES (1, 1, 'C:\Users\test\Pictures\photo2.jpg', 'O amintire draguta!', GETUTCDATE());

-- SELECT * FROM Memories ORDER BY CreatedAt DESC

-- Likes
-- Bob da like la memoria lui Carol
INSERT INTO MemoryLikes (MemoryId, UserId) VALUES (2, 2);

-- Alice da like la memoria lui Bob
INSERT INTO MemoryLikes (MemoryId, UserId) VALUES (1, 1);
