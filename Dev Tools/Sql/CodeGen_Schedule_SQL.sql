/* 
Generate a SQL script to insert schedules
*/

DECLARE @crlf varchar(2) = char(13) + char(10)
DECLARE @ScheduleId int = 7

SELECT 
	'INSERT INTO [dbo].[Schedule] ([Name], [Description], [iCalendarContent], [CheckInStartOffsetMinutes], [CheckInEndOffsetMinutes], [EffectiveStartDate], [EffectiveEndDate], [CategoryId], [Guid], [ForeignKey], [WeeklyDayOfWeek], [WeeklyTimeOfDay], [ForeignGuid], [ForeignId], [IsActive])'
	+ @crlf
	+ 'VALUES(' +
    CASE WHEN s.[Name] IS NULL THEN 'NULL' ELSE '''' + s.[Name] + '''' END + ' ,' +
    CASE WHEN s.[Description] IS NULL THEN 'NULL' ELSE '''' + s.[Description] + '''' END + ' ,' +
	CASE WHEN s.[iCalendarContent] IS NULL THEN 'NULL' ELSE '''' + REPLACE(s.[iCalendarContent], '''', '''''') + '''' END + ' ,' +
    CASE WHEN s.[CheckInStartOffsetMinutes] IS NULL THEN 'NULL' ELSE CONVERT(nvarchar(max), s.[CheckInStartOffsetMinutes]) END + ' ,' +
    CASE WHEN s.[CheckInEndOffsetMinutes] IS NULL THEN 'NULL' ELSE CONVERT(nvarchar(max), s.[CheckInEndOffsetMinutes]) END + ' ,' +
    CASE WHEN s.[EffectiveStartDate] IS NULL THEN 'NULL' ELSE '''' + CONVERT(nvarchar(max), s.[EffectiveStartDate]) + '''' END + ' ,' +
    CASE WHEN s.[EffectiveEndDate] IS NULL THEN 'NULL' ELSE '''' + CONVERT(nvarchar(max), s.[EffectiveEndDate]) + '''' END + ' ,' +
	CASE WHEN s.[CategoryId] IS NULL THEN 'NULL' ELSE CONVERT( nvarchar(50), (SELECT TOP 1 [Id] FROM [Category] WHERE [Guid] = c.Guid) ) END + ' , ' +	
	'''' + CONVERT(nvarchar(max), s.[Guid]) + ''', ' +
    CASE WHEN s.[ForeignKey] IS NULL THEN 'NULL' ELSE '''' + s.[ForeignKey] + '''' END + ' ,' +
    CASE WHEN s.[WeeklyDayOfWeek] IS NULL THEN 'NULL' ELSE CONVERT(nvarchar(max), s.[WeeklyDayOfWeek]) END + ' ,' +
	CASE WHEN s.[WeeklyTimeOfDay] IS NULL THEN 'NULL' ELSE CONVERT(nvarchar(max), s.[WeeklyTimeOfDay]) END + ' ,' +
    CASE WHEN s.[ForeignGuid] IS NULL THEN 'NULL' ELSE '''' + CONVERT(nvarchar(max), s.[ForeignGuid]) + '''' END + ' ,' +
	CASE WHEN s.[ForeignId] IS NULL THEN 'NULL' ELSE CONVERT(nvarchar(max), s.[ForeignId]) END + ' ,' +
	CASE WHEN s.[IsActive] IS NULL THEN 'NULL' ELSE CONVERT(nvarchar(max), s.[IsActive]) END + 
	' )' + 
	@crlf + @crlf AS [CodeGen Query]
  FROM [Schedule] [s]
  left join [Category] [c] on s.CategoryId = [c].[Id]
  where 
  s.Id = @ScheduleId

