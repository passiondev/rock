/*
 * Creates two new buildings ("Auditorium" and "Youth Bldg") with set number of rooms
 * and creates a new "Bldg 2" with N number of rooms defined below.
 */

DECLARE
  @maxRooms int = 50

------------------------------------------------------------------------------------------------------------
DECLARE @CampusLocationTypeId int = (SELECT [Id] FROM [DefinedValue] WHERE [Guid] = 'C0D7AE35-7901-4396-870E-3AAF472AAE88' )
DECLARE @BuildingLocationTypeId int = (SELECT [Id] FROM [DefinedValue] WHERE [Guid] = 'D9646A93-1667-4A44-82DA-12E1229B4695' )
DECLARE @RoomLocationTypeId int = (SELECT [Id] FROM [DefinedValue] WHERE [Guid] = '107C6DA1-266D-4E1C-A443-1CD37064601D' )

-- Get Main Campus
DECLARE @MainCampusLocationId int = (SELECT [Id] FROM [Location] WHERE [Name] = 'Main Campus' AND [LocationTypeValueId] = @CampusLocationTypeId )

-- Create Locations
DECLARE @LocationId int

-- Create Auditorium
INSERT INTO [Location] ([Name], [ParentLocationId], [LocationTypeValueId], [IsActive], [Guid])	VALUES ( 'Auditorium', @MainCampusLocationId, @BuildingLocationTypeId, 1, NEWID() )
SET @LocationId = SCOPE_IDENTITY()

-- Create Auditorium Locations
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Auditorium Sec. A', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Auditorium Sec. B', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Auditorium Sec. C', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Auditorium Sec. D', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Auditorium Sec. E', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Courtyard', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'East Entrance', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'West Entrance', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Chapel', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Lobby', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Prayer Room', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Baptismal', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Stage', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'A/V Studio', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Multipurpose', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Cafe', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Bookstore', @RoomLocationTypeId, 1, NEWID())

-- Create Youth Bldg
INSERT INTO [Location] ([Name], [ParentLocationId], [LocationTypeValueId], [IsActive], [Guid])	VALUES ( 'Youth Bldg', @MainCampusLocationId, @BuildingLocationTypeId, 1, NEWID() )
SET @LocationId = SCOPE_IDENTITY()

-- Create Youth Bldg Locations
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Blue Jays', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Cardinals', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Deer', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Foxes', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Hawks', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Otters', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Owls', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Porcupines', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Possums', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Quails', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Raccoons', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Ravens', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Road Runners', @RoomLocationTypeId, 1, NEWID())
INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, 'Wolves', @RoomLocationTypeId, 1, NEWID())

DECLARE
  @roomName nvarchar(100),
  @roomCounter int = 1

-- Create Bldg2
INSERT INTO [Location] ([Name], [ParentLocationId], [LocationTypeValueId], [IsActive], [Guid])	VALUES ( 'Bldg 2', @MainCampusLocationId, @BuildingLocationTypeId, 1, NEWID() )
SET @LocationId = SCOPE_IDENTITY()

-- NG groups
WHILE @roomCounter < @maxRooms
BEGIN
    SELECT @roomName = 'Room ' + REPLACE(str(@roomCounter, 3), ' ', '0')
	INSERT INTO [Location] ([ParentLocationId], [Name], [LocationTypeValueId], [IsActive], [Guid])	VALUES (@LocationId, @roomName, @RoomLocationTypeId, 1, NEWID())
    SET @roomCounter += 1;
END;
