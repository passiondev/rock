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
    public partial class VolunteerScheduling : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
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
                "dbo.GroupMemberAssignment",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    GroupMemberId = c.Int( nullable: false ),
                    LocationId = c.Int( nullable: false ),
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
            AddColumn( "dbo.GroupType", "ScheduledCommunicationTemplateId", c => c.Int() );
            AddColumn( "dbo.GroupType", "ScheduleReminderCommunicationTemplateId", c => c.Int() );
            AddColumn( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId", c => c.Int() );
            AddColumn( "dbo.GroupType", "ScheduleConfirmationEmailOffsetDays", c => c.Int( defaultValue: 4 ) );
            AddColumn( "dbo.GroupType", "ScheduleReminderEmailOffsetDays", c => c.Int() );
            AddColumn( "dbo.GroupType", "RequiresReasonIfDeclineSchedule", c => c.Boolean( nullable: false ) );
            AddColumn( "dbo.GroupMember", "ScheduleTemplateId", c => c.Int() );
            AddColumn( "dbo.GroupMember", "ScheduleStartDate", c => c.DateTime( storeType: "date" ) );
            AddColumn( "dbo.GroupMember", "ScheduleReminderEmailOffsetDays", c => c.Int( defaultValue: 1 ) );
            AddColumn( "dbo.Attendance", "ScheduledToAttend", c => c.Boolean() );
            AddColumn( "dbo.Attendance", "RequestedToAttend", c => c.Boolean() );
            AddColumn( "dbo.Attendance", "ScheduleConfirmationSent", c => c.Boolean() );
            AddColumn( "dbo.Attendance", "ScheduleReminderSent", c => c.Boolean() );
            AddColumn( "dbo.Attendance", "RSVPDateTime", c => c.DateTime() );
            AddColumn( "dbo.Attendance", "DeclineReasonValueId", c => c.Int() );
            AddColumn( "dbo.Attendance", "ScheduledByPersonAliasId", c => c.Int() );
            CreateIndex( "dbo.Group", "ScheduleCancellationPersonAliasId" );
            CreateIndex( "dbo.GroupType", "ScheduledCommunicationTemplateId" );
            CreateIndex( "dbo.GroupType", "ScheduleReminderCommunicationTemplateId" );
            CreateIndex( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId" );
            CreateIndex( "dbo.GroupMember", "ScheduleTemplateId" );
            CreateIndex( "dbo.Attendance", "DeclineReasonValueId" );
            CreateIndex( "dbo.Attendance", "ScheduledByPersonAliasId" );
            AddForeignKey( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId", "dbo.WorkflowType", "Id" );
            AddForeignKey( "dbo.GroupType", "ScheduledCommunicationTemplateId", "dbo.CommunicationTemplate", "Id" );
            AddForeignKey( "dbo.GroupType", "ScheduleReminderCommunicationTemplateId", "dbo.CommunicationTemplate", "Id" );
            AddForeignKey( "dbo.GroupMember", "ScheduleTemplateId", "dbo.GroupMemberScheduleTemplate", "Id" );
            AddForeignKey( "dbo.Group", "ScheduleCancellationPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo.Attendance", "DeclineReasonValueId", "dbo.DefinedValue", "Id" );
            AddForeignKey( "dbo.Attendance", "ScheduledByPersonAliasId", "dbo.PersonAlias", "Id" );

            // Volunteer Schedule Decline Reasons
            RockMigrationHelper.AddDefinedType( "Group", "Volunteer Schedule Decline Reason", "List of all possible schedule decline reasons.", "70C9F9C4-20CC-43DD-888D-9243853A0E52", @"" );
            RockMigrationHelper.UpdateDefinedValue( "70C9F9C4-20CC-43DD-888D-9243853A0E52", "Family Emergency", "", "7533A32D-CC7B-4218-A1CA-030FB4F2473B", false );
            RockMigrationHelper.UpdateDefinedValue( "70C9F9C4-20CC-43DD-888D-9243853A0E52", "Have to Work", "", "8B9BF3F5-11CF-4E33-98A0-D48067A18103", false );
            RockMigrationHelper.UpdateDefinedValue( "70C9F9C4-20CC-43DD-888D-9243853A0E52", "On Vacation / Out of Town", "", "BB2F0712-5C57-40E9-83BF-68876890EC7A", false );
            RockMigrationHelper.UpdateDefinedValue( "70C9F9C4-20CC-43DD-888D-9243853A0E52", "Serving Elsewhere", "", "BBD314E2-B65A-4C23-8AE1-1ADFBD58C4B4", false );
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DropForeignKey( "dbo.PersonScheduleExclusion", "PersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.PersonScheduleExclusion", "ParentPersonScheduleExclusionId", "dbo.PersonScheduleExclusion" );
            DropForeignKey( "dbo.PersonScheduleExclusion", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.PersonScheduleExclusion", "GroupId", "dbo.Group" );
            DropForeignKey( "dbo.PersonScheduleExclusion", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.Attendance", "ScheduledByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo.Attendance", "DeclineReasonValueId", "dbo.DefinedValue" );
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
            DropForeignKey( "dbo.GroupType", "ScheduleReminderCommunicationTemplateId", "dbo.CommunicationTemplate" );
            DropForeignKey( "dbo.GroupType", "ScheduledCommunicationTemplateId", "dbo.CommunicationTemplate" );
            DropForeignKey( "dbo.GroupType", "ScheduleCancellationWorkflowTypeId", "dbo.WorkflowType" );
            DropForeignKey( "dbo.GroupLocationScheduleConfig", "ScheduleId", "dbo.Schedule" );
            DropForeignKey( "dbo.GroupLocationScheduleConfig", "GroupLocationId", "dbo.GroupLocation" );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "Guid" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "ParentPersonScheduleExclusionId" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "GroupId" } );
            DropIndex( "dbo.PersonScheduleExclusion", new[] { "PersonAliasId" } );
            DropIndex( "dbo.Attendance", new[] { "ScheduledByPersonAliasId" } );
            DropIndex( "dbo.Attendance", new[] { "DeclineReasonValueId" } );
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
            DropIndex( "dbo.GroupType", new[] { "ScheduleReminderCommunicationTemplateId" } );
            DropIndex( "dbo.GroupType", new[] { "ScheduledCommunicationTemplateId" } );
            DropIndex( "dbo.GroupLocationScheduleConfig", new[] { "ScheduleId" } );
            DropIndex( "dbo.GroupLocationScheduleConfig", new[] { "GroupLocationId" } );
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
            DropColumn( "dbo.GroupType", "ScheduleReminderCommunicationTemplateId" );
            DropColumn( "dbo.GroupType", "ScheduledCommunicationTemplateId" );
            DropColumn( "dbo.GroupType", "IsSchedulingEnabled" );
            DropColumn( "dbo.Group", "ScheduleCancellationPersonAliasId" );
            DropColumn( "dbo.Group", "AttendanceRecordRequiredForCheckIn" );
            DropColumn( "dbo.Group", "SchedulingMustMeetRequirements" );
            DropTable( "dbo.PersonScheduleExclusion" );
            DropTable( "dbo.GroupMemberScheduleTemplate" );
            DropTable( "dbo.GroupMemberAssignment" );
            DropTable( "dbo.GroupLocationScheduleConfig" );
        }
    }
}
