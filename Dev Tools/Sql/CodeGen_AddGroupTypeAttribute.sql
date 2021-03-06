/* Code Generate 'AddGroupTypeGroupAttribute and AddGroupTypeGroupMemberAttribute' for migrations. 
*/

/* ToDo: Set the @GroupTypeName to the GroupType you are generating Attribute Migrations for*/
--DECLARE @GroupTypeName nvarchar(max) = '#TODO#'
DECLARE @GroupTypeName nvarchar(max) = 'Check in by Age'
DECLARE @crlf varchar(2) = char(13) + char(10)

DECLARE @EntityTypeIdGroup INT = (SELECT [Id] FROM [dbo].[EntityType] WHERE [Name] = 'Rock.Model.Group')
DECLARE @EntityTypeIdGroupMember INT = (SELECT [Id] FROM [dbo].[EntityType] WHERE [Name] = 'Rock.Model.GroupMember')

SELECT x.Up FROM (
  SELECT 
        '            RockMigrationHelper.AddGroupTypeGroupAttribute( '
		+ '"' + CONVERT(nvarchar(50), [gt].[Guid]) + '", '
        + '"' + CONVERT(nvarchar(50), [ft].[Guid])+ '", '
		+ '"' + [a].[Name] + '", ' +
		+ '@"' + [a].[Description] + '", '
		+ CONVERT(VARCHAR(5), [a].[Order]) + ', '
		+ ( CASE WHEN [a].[DefaultValue] IS NULL THEN 'null'
			ELSE + '"' + A.[DefaultValue] + '"' END) + ', '
		+ '"' + CONVERT(nvarchar(50), [a].[Guid]) + '", ' 
		+ '"' + ISNULL([a].[AbbreviatedName], '') + '" );'
		+ @crlf [Up]
		, [a].EntityTypeId
		, [gt].[Name]
		, a.[Order]
  FROM [Attribute] [a]
  INNER JOIN [GroupType] [gt] ON TRY_CONVERT(INT, A.[EntityTypeQualifierValue]) IS NOT NULL AND GT.Id = CONVERT(INT, A.[EntityTypeQualifierValue])
  INNER JOIN [FieldType] [ft] ON [ft].[Id] = [a].[FieldTypeId]
  WHERE [a].[EntityTypeQualifierColumn] = 'GROUPTYPEID'
	AND [a].[EntityTypeId] = @EntityTypeIdGroup
	AND [gt].[Name] = @GroupTypeName OR @GroupTypeName IS NULL
--ORDER BY GT.[Name], A.[Order] 
UNION ALL 
  SELECT 
        '            RockMigrationHelper.AddGroupTypeGroupMemberAttribute( '
		+ '"' + CONVERT(nvarchar(50), [gt].[Guid]) + '", '
        + '"' + CONVERT(nvarchar(50), [ft].[Guid])+ '", '
		+ '"' + [a].[Name] + '", '
		+ '@"' + [a].[Description] + '", '
		+ CONVERT(VARCHAR(5), [a].[Order]) + ', '
		+ ( CASE WHEN [a].[DefaultValue] IS NULL THEN 'null' 
			ELSE + '"' + A.[DefaultValue] + '"' END) + ', '
        + '"' + CONVERT(nvarchar(50), A.[Guid]) 
		+ '"' + [a].[AbbreviatedName] + '");'
		+ @crlf AS [Up]
		, a.EntityTypeId
		, gt.[Name]
		, a.[Order]
  FROM [Attribute] [a]
  INNER JOIN [GroupType] [gt] ON TRY_CONVERT(INT, [a].[EntityTypeQualifierValue]) IS NOT NULL AND [gt].[Id] = CONVERT(INT, [a].[EntityTypeQualifierValue])
  INNER JOIN [FieldType] [ft] ON [ft].[Id] = [a].[FieldTypeId]
  WHERE [a].[EntityTypeQualifierColumn] = 'GroupTypeId'
	AND [a].[EntityTypeId] = @EntityTypeIdGroupMember
	AND [gt].[Name] = @GroupTypeName OR @GroupTypeName IS NULL
) x
ORDER BY [x].[EntityTypeId], [x].[Name], [x].[Order] 

SELECT 
	  '            RockMigrationHelper.DeleteAttribute( "' + CONVERT(NVARCHAR(50), [a].[Guid])+ '");'
	+ '    // GroupType - ' + [et].FriendlyName + ' Attribute, '
	+ [gt].[Name] + ': ' + [a].[Name]
	+ @crlf AS [Down]
FROM [Attribute] a
INNER JOIN [GroupType] gt ON TRY_CONVERT(INT, [a].[EntityTypeQualifierValue]) IS NOT NULL AND [gt].[Id] = CONVERT(INT, [a].[EntityTypeQualifierValue])
INNER JOIN [FieldType] ft ON [ft].Id = [a].[FieldTypeId]
LEFT OUTER JOIN [EntityType] et on [et].Id = [a].EntityTypeId
WHERE [a].[EntityTypeQualifierColumn] = 'GroupTypeId'
	AND [a].[EntityTypeId] IN (@EntityTypeIdGroup, @EntityTypeIdGroupMember)
	AND [gt].[Name] = @GroupTypeName OR @GroupTypeName IS NULL
ORDER BY [a].EntityTypeId, [gt].[Name], [a].[Order] 
