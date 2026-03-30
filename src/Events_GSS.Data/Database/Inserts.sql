USE ISSEvents

SET IDENTITY_INSERT Users ON;

INSERT INTO Users (Id, Name, Email, PasswordHash, ReputationPoints, Tier)
VALUES (1, 'Default User', 'default@example.com', 'hashed_password_here', 0, 'Newcomer');

SET IDENTITY_INSERT Users OFF;


-- =========================
-- INSERT DEFAULT EVENT (EventId = 1)
-- =========================
SET IDENTITY_INSERT Events ON;

INSERT INTO Events (
    EventId,
    Name,
    LocationLat,
    LocationLng,
    StartDateTime,
    EndDateTime,
    IsPublic,
    Description,
    MaximumPeople,
    EventBannerPath,
    CategoryId,
    CreatedBy,
    SlowModeSeconds
)
VALUES (
           1,
           'Default Event',
           46.7700,          -- Example: Cluj-Napoca latitude
           23.5900,          -- Example: Cluj-Napoca longitude
           GETDATE(),
           DATEADD(HOUR, 8, GETDATE()),
           1,
           'This is a default event.',
           NULL,
           NULL,
           NULL,             -- Must be NULL unless you already have Categories
           1,                -- CreatedBy = User Id 1
           NULL
       );

SET IDENTITY_INSERT Events OFF;