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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Quartz;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace Rock.Jobs
{
    [DisallowConcurrentExecution]
    [DisplayName( "Send Volunteer Schedule RSVPs" )]
    [Description( "Sends Volunteer Scheduling RSVP emails to people that haven't been notified yet." )]

    [GroupField(
        "Group",
        Key = AttributeKey.Group,
        Description = "Only people in or under this group will receive the Schedule RSVP email.",
        IsRequired = false,
        Order = 0 )]
    public class SendVolunteerScheduleRSVP : IJob
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for Job Attributes
        /// </summary>
        protected static class AttributeKey
        {
            public const string Group = "Group";
        }

        #endregion Attribute Keys

        #region Fields

        private int _volunteerScheduleRSVPsSent = 0;

        #endregion Fields

        /// <summary>
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public SendVolunteerScheduleRSVP()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            var parentGroupGuid = context.JobDetail.JobDataMap.GetString( AttributeKey.Group ).AsGuidOrNull();

            List<Person> personsScheduled = new List<Person>();
            using ( var rockContext = new RockContext() )
            {
                List<int> groupIds = new List<int>();
                var groupService = new GroupService( rockContext );
                var attendanceService = new AttendanceService( rockContext );

                // Get all who have not already been notified( attendance.ScheduleConfirmationSent = false ) and who have been requested to attend.
                var sendConfirmationAttendancesQuery = new AttendanceService( rockContext )
                    .GetPendingScheduledConfirmations()
                    .Where( a => a.ScheduleConfirmationSent != true );

                // if the parent group is configured on the Job then limit to the group and its child groups
                if ( parentGroupGuid.HasValue )
                {
                    var parentGroup = groupService.Get( parentGroupGuid.Value );
                    groupIds.Add( parentGroup.Id );
                    var groupChildrenIds = groupService.GetAllDescendents( parentGroup.Id ).Select( g => g.Id ).ToArray();
                    groupIds.AddRange( groupChildrenIds );
                    sendConfirmationAttendancesQuery = sendConfirmationAttendancesQuery.Where( a => groupIds.Contains( a.Occurrence.GroupId.Value ) );
                }

                var currentDate = RockDateTime.Now.Date;

                // limit to confirmation offset window
                sendConfirmationAttendancesQuery = sendConfirmationAttendancesQuery
                    .Where( a => a.Occurrence.Group.GroupType.ScheduleConfirmationEmailOffsetDays.HasValue )
                    .Where( a => System.Data.Entity.SqlServer.SqlFunctions.DateDiff( "day", currentDate, a.Occurrence.OccurrenceDate ) <= a.Occurrence.Group.GroupType.ScheduleConfirmationEmailOffsetDays.Value );

                _volunteerScheduleRSVPsSent = attendanceService.SendScheduledAttendanceUpdateEmails( sendConfirmationAttendancesQuery );
                rockContext.SaveChanges();
            }
        }
    }
}
