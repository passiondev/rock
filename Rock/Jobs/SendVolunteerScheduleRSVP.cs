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
using System;
using System.Collections.Generic;
using System.Linq;
using Quartz;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;

namespace Rock.Jobs
{
    [GroupField( "Group", "Only people in or under this group will receive the RSVP. ", false, order: 0 )]
    [SystemEmailField( "Schedule Update Email", "The system email that will be used to send the digest of rsvp / confirmation emails. Available Lava variables Attendance, Attendances", true, Rock.SystemGuid.SystemEmail.SCHEDULING_UPDATE, "", 1 )]
    public class SendVolunteerScheduleRSVP : IJob
    {
        #region Fields

        private JobDataMap _dataMap = null;
        private int _VolunteerScheduleRemindersSent = 0;
        private int _errorCount = 0;
        private List<string> _errorMessages = new List<string>();

        #endregion

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            _dataMap = context.JobDetail.JobDataMap;
            var parentGroupGuid = _dataMap.GetString( "Group" ).AsGuid();

            //If a group is configured on the job then filter by that group.

            if ( !parentGroupGuid.IsEmpty() )
            {
                ProcessAttendances( parentGroupGuid );
            }
            else
            {
                ProcessAttendances( null );
            }
        }

        /// <summary>
        /// Processes the attendances.
        /// </summary>
        /// <param name="parentGroupGuid">The parent group unique identifier.</param>
        private void ProcessAttendances( Guid? parentGroupGuid )
        {
            List<Person> personsScheduled = new List<Person>();
            bool hasChanges = false;
            using ( var rockContext = new RockContext() )
            {
                List<int> groupIds = new List<int>();
                var groupService = new GroupService( rockContext );

                //if the parent group is configured on the Job then find all people in or under the configured group who have been scheduled,
                //but who have not already been notified( attendance.ScheduleConfirmationSent = false ) and who have been requested to attend.

                if ( parentGroupGuid != null )
                {
                    var parentGroup = groupService.Get( ( Guid ) parentGroupGuid );
                    groupIds.Add( parentGroup.Id );
                    var groupChildrenIds = groupService.GetAllDescendents( parentGroup.Id ).Select( g => g.Id ).ToArray();
                    groupIds.AddRange( groupChildrenIds );
                }

                var qryPendingConfirmations = new AttendanceService( rockContext )
                       .GetPendingScheduledConfirmations()
                       .Where( a =>
                       ( parentGroupGuid != null
                        && groupIds.Contains( a.Occurrence.Group.Id )// if we have configure group filter by it
                        || parentGroupGuid == null )// or ignore groupIds 
                         && ( a.ScheduleConfirmationSent == null
                        || a.ScheduleConfirmationSent == false ) ).ToList();

                if ( qryPendingConfirmations != null && qryPendingConfirmations.Count() > 0 )
                {
                    //Group all of the attendance occurenaces per person.

                    var groupedAttendances = qryPendingConfirmations.GroupBy( x => x.PersonAliasId );

                    foreach ( var attendanceGroup in groupedAttendances )
                    {
                        SendEmailNotification( attendanceGroup.ToList() );
                        hasChanges = UpdateScheduleConfirmationSentStatus( attendanceGroup );
                    }

                    if ( hasChanges )
                    {
                        rockContext.SaveChanges();
                    }
                }
            }
        }

        /// <summary>
        /// Sends the email notification.
        /// </summary>
        /// <param name="personAttendances">The person attendances.</param>
        private void SendEmailNotification( List<Attendance> personAttendances )
        {
            int? personId = null;
            try     
            {
                var errors = new List<string>();
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                // Use the first attendance from the group to build out the top level information and use the full collection to build out
                // the list of all pending attendance occurances. 

                var firstOccurance = personAttendances.First();
                var recipient = firstOccurance.PersonAlias.Person.Email;
                personId = firstOccurance.PersonAlias.PersonId;

                mergeFields.Add( "Attendance", firstOccurance );
                mergeFields.Add( "Attendances", personAttendances );

                var groupName = firstOccurance.Occurrence.Group.Name;

                var emailMessage = new RockEmailMessage( _dataMap.GetString( "ScheduleUpdateEmail" ).AsGuid() );
                emailMessage.AddRecipient( new RecipientData( recipient, mergeFields ) );
                emailMessage.Send( out errors );

                if ( errors.Any() )
                {
                    _errorCount = +errors.Count;
                    _errorMessages.AddRange( errors );
                }
                else
                {
                    _VolunteerScheduleRemindersSent++;
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( new Exception( $"Exception occured trying to send email notification for PersonId:{ personId }", ex ) );
            }
        }

        /// <summary>
        /// Updates the schedule confirmation sent status.
        /// </summary>
        /// <param name="personAttendances">The person attendances.</param>
        private bool UpdateScheduleConfirmationSentStatus( IGrouping<int?, Attendance> personAttendances )
        {
            var changes = false;
            foreach ( var attendance in personAttendances )
            {
                attendance.ScheduleConfirmationSent = true;
                attendance.ModifiedDateTime = RockDateTime.Now;
                changes = true;
            }
            return changes;
        }
    }
}
