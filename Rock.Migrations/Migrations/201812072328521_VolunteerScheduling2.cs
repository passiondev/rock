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
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class VolunteerScheduling2 : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            DropForeignKey("dbo.GroupType", "ScheduledCommunicationTemplateId", "dbo.CommunicationTemplate");
            DropForeignKey("dbo.GroupType", "ScheduleReminderCommunicationTemplateId", "dbo.CommunicationTemplate");
            DropIndex("dbo.GroupType", new[] { "ScheduledCommunicationTemplateId" });
            DropIndex("dbo.GroupType", new[] { "ScheduleReminderCommunicationTemplateId" });
            AddColumn("dbo.GroupType", "ScheduledSystemEmailId", c => c.Int());
            AddColumn("dbo.GroupType", "ScheduleReminderSystemEmailId", c => c.Int());
            CreateIndex("dbo.GroupType", "ScheduledSystemEmailId");
            CreateIndex("dbo.GroupType", "ScheduleReminderSystemEmailId");
            AddForeignKey("dbo.GroupType", "ScheduledSystemEmailId", "dbo.SystemEmail", "Id");
            AddForeignKey("dbo.GroupType", "ScheduleReminderSystemEmailId", "dbo.SystemEmail", "Id");
            DropColumn("dbo.GroupType", "ScheduledCommunicationTemplateId");
            DropColumn("dbo.GroupType", "ScheduleReminderCommunicationTemplateId");
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            AddColumn("dbo.GroupType", "ScheduleReminderCommunicationTemplateId", c => c.Int());
            AddColumn("dbo.GroupType", "ScheduledCommunicationTemplateId", c => c.Int());
            DropForeignKey("dbo.GroupType", "ScheduleReminderSystemEmailId", "dbo.SystemEmail");
            DropForeignKey("dbo.GroupType", "ScheduledSystemEmailId", "dbo.SystemEmail");
            DropIndex("dbo.GroupType", new[] { "ScheduleReminderSystemEmailId" });
            DropIndex("dbo.GroupType", new[] { "ScheduledSystemEmailId" });
            DropColumn("dbo.GroupType", "ScheduleReminderSystemEmailId");
            DropColumn("dbo.GroupType", "ScheduledSystemEmailId");
            CreateIndex("dbo.GroupType", "ScheduleReminderCommunicationTemplateId");
            CreateIndex("dbo.GroupType", "ScheduledCommunicationTemplateId");
            AddForeignKey("dbo.GroupType", "ScheduleReminderCommunicationTemplateId", "dbo.CommunicationTemplate", "Id");
            AddForeignKey("dbo.GroupType", "ScheduledCommunicationTemplateId", "dbo.CommunicationTemplate", "Id");
        }
    }
}
