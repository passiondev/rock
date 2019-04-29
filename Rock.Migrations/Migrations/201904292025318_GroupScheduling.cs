// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace Rock.Migrations
{
    /// <summary>
    ///
    /// </summary>
    public partial class GroupScheduling : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            SchemaChangesUp();

            /** Section 1**/
            // Volunteer Schedule Decline Reasons
            RockMigrationHelper.AddDefinedType( "Group", "Volunteer Schedule Decline Reason", "List of all possible schedule decline reasons.", "70C9F9C4-20CC-43DD-888D-9243853A0E52", @"" );
            RockMigrationHelper.UpdateDefinedValue( "70C9F9C4-20CC-43DD-888D-9243853A0E52", "Family Emergency", "", "7533A32D-CC7B-4218-A1CA-030FB4F2473B", false );
            RockMigrationHelper.UpdateDefinedValue( "70C9F9C4-20CC-43DD-888D-9243853A0E52", "Have to Work", "", "8B9BF3F5-11CF-4E33-98A0-D48067A18103", false );
            RockMigrationHelper.UpdateDefinedValue( "70C9F9C4-20CC-43DD-888D-9243853A0E52", "On Vacation / Out of Town", "", "BB2F0712-5C57-40E9-83BF-68876890EC7A", false );
            RockMigrationHelper.UpdateDefinedValue( "70C9F9C4-20CC-43DD-888D-9243853A0E52", "Serving Elsewhere", "", "BBD314E2-B65A-4C23-8AE1-1ADFBD58C4B4", false );

            Sql( MigrationSQL._201904292025318_GroupScheduling_PopulateScheduleTemplates );

            RockMigrationHelper.AddPage( true, "0B213645-FA4E-44A5-8E4C-B2D8EF054985", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Group Member Schedule Templates", "", "1F50B5C5-2486-4D8F-9435-27BDF8302683", "" ); // Site:Rock RMS
            RockMigrationHelper.AddPage( true, "1F50B5C5-2486-4D8F-9435-27BDF8302683", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Group Member Schedule Template Detail", "", "B7B0864D-91F2-4B24-A7B0-FC7BEE769FA0", "" ); // Site:Rock RMS
            RockMigrationHelper.AddPage( true, "B0F4B33D-DD11-4CCC-B79D-9342831B8701", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Group Scheduler", "", "1815D8C6-7C4A-4C05-A810-CF23BA937477", "" ); // Site:Rock RMS
            RockMigrationHelper.AddPageRoute( "1815D8C6-7C4A-4C05-A810-CF23BA937477", "GroupScheduler/{GroupId}", "D0F198E2-6111-4EC1-8D1D-55AC10E28D04" );// for Page:Group Scheduler
            RockMigrationHelper.AddPageRoute( "1815D8C6-7C4A-4C05-A810-CF23BA937477", "GroupScheduler", "98CB9BAC-AE45-4EDB-BC31-352B889F908E" );// for Page:Group Scheduler
            RockMigrationHelper.UpdateBlockType( "Group Member Schedule Template Detail", "Displays the details of a group member schedule template.", "~/Blocks/Groups/GroupMemberScheduleTemplateDetail.ascx", "Groups", "B5EB66A1-7391-49D5-B613-5ED804A31E7B" );
            RockMigrationHelper.UpdateBlockType( "Group Member Schedule Template List", "Lists group member schedule templates.", "~/Blocks/Groups/GroupMemberScheduleTemplateList.ascx", "Groups", "D930E08B-ACD3-4ADD-9FAC-3B61C021D0F7" );
            RockMigrationHelper.UpdateBlockType( "Group Scheduler", "Allows volunteer schedules for groups and locations to be managed by a scheduler.", "~/Blocks/Groups/GroupScheduler.ascx", "Groups", "37D43C21-1A4D-4B13-9555-EF0B7304EB8A" );
            // Add Block to Page: Internal Homepage Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "20F97A93-7949-4C2A-8A5E-C756FE8585CA".AsGuid(), null, "C2D29296-6A87-47A9-A753-EE4E9159C4C4".AsGuid(), "19B61D65-37E3-459F-A44F-DEF0089118A3".AsGuid(), "Dev Links", "Main", @"", @"", 0, "F337823D-BA5D-49F8-9BC4-1EF48C9000CE" );
            // Add Block to Page: Group Member Schedule Templates Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "1F50B5C5-2486-4D8F-9435-27BDF8302683".AsGuid(), null, "C2D29296-6A87-47A9-A753-EE4E9159C4C4".AsGuid(), "D930E08B-ACD3-4ADD-9FAC-3B61C021D0F7".AsGuid(), "Group Member Schedule Template List", "Main", @"", @"", 0, "DFF3E9C7-1FB8-42E3-A6CB-5F28FC7DA564" );
            // Add Block to Page: Group Member Schedule Template Detail Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "B7B0864D-91F2-4B24-A7B0-FC7BEE769FA0".AsGuid(), null, "C2D29296-6A87-47A9-A753-EE4E9159C4C4".AsGuid(), "B5EB66A1-7391-49D5-B613-5ED804A31E7B".AsGuid(), "Group Member Schedule Template Detail", "Main", @"", @"", 0, "B251F51D-075A-4744-9788-F9AD89AA0552" );
            // Add Block to Page: Group Scheduler Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "1815D8C6-7C4A-4C05-A810-CF23BA937477".AsGuid(), null, "C2D29296-6A87-47A9-A753-EE4E9159C4C4".AsGuid(), "37D43C21-1A4D-4B13-9555-EF0B7304EB8A".AsGuid(), "Group Scheduler", "Main", @"", @"", 0, "B282B285-4600-4097-9CC0-3439E2B66C34" );
            // Attrib for BlockType: Group Member Schedule Template List:Detail Page
            RockMigrationHelper.UpdateBlockTypeAttribute( "D930E08B-ACD3-4ADD-9FAC-3B61C021D0F7", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", @"", 0, @"", "224F9179-1B11-4EFB-8315-3217301422DE" );
            // Attrib Value for Block:Group Member Schedule Template List, Attribute:Detail Page Page: Group Member Schedule Templates, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "DFF3E9C7-1FB8-42E3-A6CB-5F28FC7DA564", "224F9179-1B11-4EFB-8315-3217301422DE", @"b7b0864d-91f2-4b24-a7b0-fc7bee769fa0" );
            // Attrib Value for Block:Group Member Schedule Template List, Attribute:core.CustomGridColumnsConfig Page: Group Member Schedule Templates, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "DFF3E9C7-1FB8-42E3-A6CB-5F28FC7DA564", "70AB76D5-B6CA-42E7-86EC-32843B3BDB01", @"" );
            // Attrib Value for Block:Group Member Schedule Template List, Attribute:core.CustomGridEnableStickyHeaders Page: Group Member Schedule Templates, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "DFF3E9C7-1FB8-42E3-A6CB-5F28FC7DA564", "D735FD6C-2D7C-4400-93C6-1D1052C9A4B0", @"False" );

            AddSchedulingResponseEmailTemplateUp();

            AddSchedulingUpdateSystemEmailUp();
        }

        /// <summary>
        /// Adds the scheduling update system email up.
        /// </summary>
        private void AddSchedulingUpdateSystemEmailUp()
        {
            RockMigrationHelper.UpdateSystemEmail( "Groups", "Scheduling Update Email", "", "", "", "", "", "Scheduling Update", @"{{ 'Global' | Attribute:'EmailHeader' }}
<h1>Scheduling Update</h1>
<p>Hi {{  Attendance.PersonAlias.Person.NickName  }}!</p>
<br/>
<p>You have been added to the schedule for the following dates and times. Please let us know if you'll be attending as soon as possible.</p>
<br/>
<p>Thanks!</p>
{{ Attendance.ScheduledByPersonAlias.Person.FullName  }}
</br>
{{ 'Global' | 'Attribute:OrganizationName' }}
<br/>
<br/>
<table>
{% for attendance in Attendances %}
    <tr><td>&nbsp;</td></tr>
    <tr><td><h5>{{attendance.Occurrence.OccurrenceDate | Date:'dddd, MMMM, d, yyyy'}}</h5></td></tr>
    <tr><td>{{ attendance.Occurrence.Group.Name }}</td></tr>
    <tr><td>{{ attendance.Location.Name }}&nbsp;{{ attendance.Schedule.Name }}</td></tr>
    {% if forloop.first  %}
    <tr><td><a href=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}ScheduleConfirmation?attendanceId{{attendance.Id}}&isConfirmed=true"">Accept</a>&nbsp;<a href=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}ScheduleConfirmation?attendanceId{{attendance.Id}}&isConfirmed=false"">Decline</a></td>
    </tr>
    <tr><td>&nbsp;</td></tr>
{% endif %}
{% endfor %}
</table>
<br/>
{{ 'Global' | Attribute:'OrganizationName' }}
<br/>
{{ ScheduledDate | Date: 'dddd, MMMM, d, yyyy' }}", "F8E4CE07-68F5-4169-A865-ECE915CF421C" );
        }

        /// <summary>
        /// Adds the scheduling response email template up.
        /// </summary>
        private void AddSchedulingResponseEmailTemplateUp()
        {
            RockMigrationHelper.UpdateSystemEmail( "Groups", "Scheduling Response Email", "", "", "", "", "", "{%- assign rsvp = ScheduledItem.RSVP | AsBoolean -%} {% if rsvp %}Accepted{% else %}Declined{% endif %}",
            @"{{ ""Global"" | Attribute:""EmailHeader"" }}
<h1>Scheduling Response</h1>
<p>Hi {{ Scheduler.NickName }}!</p>
<br/>
<p>{{ Person.FullName }}{%- assign rsvp = ScheduledItem.RSVP | AsBoolean -%} {% if rsvp %} has confirmed and will be at the:{%else %} is unable to attend the: {% endif %}</p>
<br/>
{{ Group.Name }}
{{ ScheduledItem.Location.Name }} {{ScheduledItem.Schedule.Name}}
<br/>
{{ ""Global"" | Attribute:""OrganizationName"" }}<br/>
<h2>{{ScheduledItem.Occurence.OccurenceDate | Date: ""dddd, MMMM, d, yyyy""}}</h2>
<p>&nbsp;</p>
{{ ""Global"" | Attribute:""EmailFooter"" }}", "D095F78D-A5CF-4EF6-A038-C7B07E250611" );
        }

        /// <summary>
        /// Schemas the changes up.
        /// </summary>
        private void SchemaChangesUp()
        {
            CreateTable(
                            "dbo.GroupMemberAssignment",
                            c => new
                            {
                                Id = c.Int( nullable: false, identity: true ),
                                GroupMemberId = c.Int( nullable: false ),
                                LocationId = c.Int(),
                                ScheduleId = c.Int(),
                                CreatedDateTime = c.DateTime(),
                                ModifiedDateTime = c.DateTime(),
                                CreatedByPersonAliasId = c.Int(),
                                ModifiedByPersonAliasId = c.Int(),
                                Guid = c.Guid( nullable: false ),
                                ForeignId = c.Int(),
                                ForeignGuid = c.Guid(),
                                ForeignKey = c.String( maxLength: 100 ),
                            } )
                            .PrimaryKey( t => t.Id )
                            .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                            .ForeignKey( "dbo.GroupMember", t => t.GroupMemberId )
                            .ForeignKey( "dbo.Location", t => t.LocationId )
                            .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                            .ForeignKey( "dbo.Schedule", t => t.ScheduleId )
                            .Index( t => new { t.GroupMemberId, t.LocationId, t.ScheduleId }, unique: true, name: "IX_GroupMemberIdLocationIdScheduleId" )
                            .Index( t => t.CreatedByPersonAliasId )
                            .Index( t => t.ModifiedByPersonAliasId )
                            .Index( t => t.Guid, unique: true );

            CreateTable(
                "dbo.GroupMemberScheduleTemplate",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String( nullable: false, maxLength: 100 ),
                    GroupTypeId = c.Int(),
                    ScheduleId = c.Int( nullable: false ),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.GroupType", t => t.GroupTypeId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .ForeignKey( "dbo.Schedule", t => t.ScheduleId )
                .Index( t => t.GroupTypeId )
                .Index( t => t.ScheduleId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true );

            CreateTable(
                "dbo.GroupLocationScheduleConfig",
                c => new
                {
                    GroupLocationId = c.Int( nullable: false ),
                    ScheduleId = c.Int( nullable: false ),
                    MinimumCapacity = c.Int(),
                    DesiredCapacity = c.Int(),
                    MaximumCapacity = c.Int(),
                } )
                .PrimaryKey( t => new { t.GroupLocationId, t.ScheduleId } )
                .ForeignKey( "dbo.GroupLocation", t => t.GroupLocationId )
                .ForeignKey( "dbo.Schedule", t => t.ScheduleId )
                .Index( t => t.GroupLocationId )
                .Index( t => t.ScheduleId );

            CreateTable(
                "dbo.PersonScheduleExclusion",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    PersonAliasId = c.Int( nullable: false ),
                    Title = c.String( maxLength: 100 ),
                    StartDate = c.DateTime( nullable: false, storeType: "date" ),
                    EndDate = c.DateTime( nullable: false, storeType: "date" ),
                    GroupId = c.Int(),
                    ParentPersonScheduleExclusionId = c.Int(),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.Group", t => t.GroupId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .ForeignKey( "dbo.PersonScheduleExclusion", t => t.ParentPersonScheduleExclusionId )
                .ForeignKey( "dbo.PersonAlias", t => t.PersonAliasId )
                .Index( t => t.PersonAliasId )
                .Index( t => t.GroupId )
                .Index( t => t.ParentPersonScheduleExclusionId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true );

            AddColumn( "dbo.Group", "SchedulingMustMeetRequirements", c => c.Boolean( nullable: false ) );
            AddColumn( "dbo.Group", "AttendanceRecordRequiredForCheckIn", c => c.Int( nullable: false ) );
            AddColumn( "dbo.Group", "ScheduleCancellationPersonAliasId", c => c.Int() );
            AddColumn( "dbo.GroupType", "IsSchedulingEnabled", c => c.Boolean( nullable: false ) );
            AddColumn( "dbo.GroupType", "ScheduledSystemEmailId", c => c.Int() );
            AddColumn( "dbo.GroupType", "ScheduleReminderSystemEmailId", c => c.Int() );
            AddColumn( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId", c => c.Int() );
            AddColumn( "dbo.GroupType", "ScheduleConfirmationEmailOffsetDays", c => c.Int() );
            AddColumn( "dbo.GroupType", "ScheduleReminderEmailOffsetDays", c => c.Int() );
            AddColumn( "dbo.GroupType", "RequiresReasonIfDeclineSchedule", c => c.Boolean( nullable: false ) );
            AddColumn( "dbo.GroupMember", "ScheduleTemplateId", c => c.Int() );
            AddColumn( "dbo.GroupMember", "ScheduleStartDate", c => c.DateTime( storeType: "date" ) );
            AddColumn( "dbo.GroupMember", "ScheduleReminderEmailOffsetDays", c => c.Int() );
            AddColumn( "dbo.Attendance", "ScheduledToAttend", c => c.Boolean() );
            AddColumn( "dbo.Attendance", "RequestedToAttend", c => c.Boolean() );
            AddColumn( "dbo.Attendance", "ScheduleConfirmationSent", c => c.Boolean() );
            AddColumn( "dbo.Attendance", "ScheduleReminderSent", c => c.Boolean() );
            AddColumn( "dbo.Attendance", "RSVPDateTime", c => c.DateTime() );
            AddColumn( "dbo.Attendance", "DeclineReasonValueId", c => c.Int() );
            AddColumn( "dbo.Attendance", "ScheduledByPersonAliasId", c => c.Int() );
            CreateIndex( "dbo.Group", "ScheduleCancellationPersonAliasId" );
            CreateIndex( "dbo.GroupType", "ScheduledSystemEmailId" );
            CreateIndex( "dbo.GroupType", "ScheduleReminderSystemEmailId" );
            CreateIndex( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId" );
            CreateIndex( "dbo.GroupMember", "ScheduleTemplateId" );
            CreateIndex( "dbo.Attendance", "DeclineReasonValueId" );
            CreateIndex( "dbo.Attendance", "ScheduledByPersonAliasId" );
            AddForeignKey( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId", "dbo.WorkflowType", "Id" );
            AddForeignKey( "dbo.GroupType", "ScheduledSystemEmailId", "dbo.SystemEmail", "Id" );
            AddForeignKey( "dbo.GroupType", "ScheduleReminderSystemEmailId", "dbo.SystemEmail", "Id" );
            AddForeignKey( "dbo.GroupMember", "ScheduleTemplateId", "dbo.GroupMemberScheduleTemplate", "Id" );
            AddForeignKey( "dbo.Group", "ScheduleCancellationPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo.Attendance", "DeclineReasonValueId", "dbo.DefinedValue", "Id" );
            AddForeignKey( "dbo.Attendance", "ScheduledByPersonAliasId", "dbo.PersonAlias", "Id" );
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            SchemaChangesDown();

            // Down for AddSchedulingResponseEmailTemplate
            RockMigrationHelper.DeleteSystemEmail( "D095F78D-A5CF-4EF6-A038-C7B07E250611" );

            // Down for AddSchedulingUpdateSystemEmail
            RockMigrationHelper.DeleteSystemEmail( "F8E4CE07-68F5-4169-A865-ECE915CF421C" );
        }

        /// <summary>
        /// Schemas the changes down.
        /// </summary>
        private void SchemaChangesDown()
        {
            DropForeignKey( "dbo.PersonScheduleExclusion", "PersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.PersonScheduleExclusion", "ParentPersonScheduleExclusionId", "dbo.PersonScheduleExclusion" );
            DropForeignKey( "dbo.PersonScheduleExclusion", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.PersonScheduleExclusion", "GroupId", "dbo.Group" );
            DropForeignKey( "dbo.PersonScheduleExclusion", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.Attendance", "ScheduledByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.Attendance", "DeclineReasonValueId", "dbo.DefinedValue" );
            DropForeignKey( "dbo.GroupLocationScheduleConfig", "ScheduleId", "dbo.Schedule" );
            DropForeignKey( "dbo.GroupLocationScheduleConfig", "GroupLocationId", "dbo.GroupLocation" );
            DropForeignKey( "dbo.Group", "ScheduleCancellationPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.GroupMember", "ScheduleTemplateId", "dbo.GroupMemberScheduleTemplate" );
            DropForeignKey( "dbo.GroupMemberScheduleTemplate", "ScheduleId", "dbo.Schedule" );
            DropForeignKey( "dbo.GroupMemberScheduleTemplate", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.GroupMemberScheduleTemplate", "GroupTypeId", "dbo.GroupType" );
            DropForeignKey( "dbo.GroupMemberScheduleTemplate", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.GroupMemberAssignment", "ScheduleId", "dbo.Schedule" );
            DropForeignKey( "dbo.GroupMemberAssignment", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.GroupMemberAssignment", "LocationId", "dbo.Location" );
            DropForeignKey( "dbo.GroupMemberAssignment", "GroupMemberId", "dbo.GroupMember" );
            DropForeignKey( "dbo.GroupMemberAssignment", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.GroupType", "ScheduleReminderSystemEmailId", "dbo.SystemEmail" );
            DropForeignKey( "dbo.GroupType", "ScheduledSystemEmailId", "dbo.SystemEmail" );
            DropForeignKey( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId", "dbo.WorkflowType" );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "Guid" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "ParentPersonScheduleExclusionId" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "GroupId" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "PersonAliasId" } );
            DropIndex( "dbo.Attendance", new[] { "ScheduledByPersonAliasId" } );
            DropIndex( "dbo.Attendance", new[] { "DeclineReasonValueId" } );
            DropIndex( "dbo.GroupLocationScheduleConfig", new[] { "ScheduleId" } );
            DropIndex( "dbo.GroupLocationScheduleConfig", new[] { "GroupLocationId" } );
            DropIndex( "dbo.GroupMemberScheduleTemplate", new[] { "Guid" } );
            DropIndex( "dbo.GroupMemberScheduleTemplate", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo.GroupMemberScheduleTemplate", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo.GroupMemberScheduleTemplate", new[] { "ScheduleId" } );
            DropIndex( "dbo.GroupMemberScheduleTemplate", new[] { "GroupTypeId" } );
            DropIndex( "dbo.GroupMemberAssignment", new[] { "Guid" } );
            DropIndex( "dbo.GroupMemberAssignment", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo.GroupMemberAssignment", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo.GroupMemberAssignment", "IX_GroupMemberIdLocationIdScheduleId" );
            DropIndex( "dbo.GroupMember", new[] { "ScheduleTemplateId" } );
            DropIndex( "dbo.GroupType", new[] { "ScheduleCancellationWorkflowTypeId" } );
            DropIndex( "dbo.GroupType", new[] { "ScheduleReminderSystemEmailId" } );
            DropIndex( "dbo.GroupType", new[] { "ScheduledSystemEmailId" } );
            DropIndex( "dbo.Group", new[] { "ScheduleCancellationPersonAliasId" } );
            DropColumn( "dbo.Attendance", "ScheduledByPersonAliasId" );
            DropColumn( "dbo.Attendance", "DeclineReasonValueId" );
            DropColumn( "dbo.Attendance", "RSVPDateTime" );
            DropColumn( "dbo.Attendance", "ScheduleReminderSent" );
            DropColumn( "dbo.Attendance", "ScheduleConfirmationSent" );
            DropColumn( "dbo.Attendance", "RequestedToAttend" );
            DropColumn( "dbo.Attendance", "ScheduledToAttend" );
            DropColumn( "dbo.GroupMember", "ScheduleReminderEmailOffsetDays" );
            DropColumn( "dbo.GroupMember", "ScheduleStartDate" );
            DropColumn( "dbo.GroupMember", "ScheduleTemplateId" );
            DropColumn( "dbo.GroupType", "RequiresReasonIfDeclineSchedule" );
            DropColumn( "dbo.GroupType", "ScheduleReminderEmailOffsetDays" );
            DropColumn( "dbo.GroupType", "ScheduleConfirmationEmailOffsetDays" );
            DropColumn( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId" );
            DropColumn( "dbo.GroupType", "ScheduleReminderSystemEmailId" );
            DropColumn( "dbo.GroupType", "ScheduledSystemEmailId" );
            DropColumn( "dbo.GroupType", "IsSchedulingEnabled" );
            DropColumn( "dbo.Group", "ScheduleCancellationPersonAliasId" );
            DropColumn( "dbo.Group", "AttendanceRecordRequiredForCheckIn" );
            DropColumn( "dbo.Group", "SchedulingMustMeetRequirements" );
            DropTable( "dbo.PersonScheduleExclusion" );
            DropTable( "dbo.GroupLocationScheduleConfig" );
            DropTable( "dbo.GroupMemberScheduleTemplate" );
            DropTable( "dbo.GroupMemberAssignment" );
        }
    }
}
