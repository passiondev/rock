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
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using Rock.Chart;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;

namespace Rock.Rest.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public partial class AttendancesController
    {
        /// <summary>
        /// Gets the chart data.
        /// </summary>
        /// <returns></returns>
        [Authenticate, Secured]
        [System.Web.Http.Route( "api/Attendances/GetChartData" )]
        public IEnumerable<IChartData> GetChartData( ChartGroupBy groupBy = ChartGroupBy.Week, AttendanceGraphBy graphBy = AttendanceGraphBy.Total, DateTime? startDate = null, DateTime? endDate = null, string groupIds = null, string campusIds = null, string scheduleIds = null, int? dataViewId = null )
        {
            return new AttendanceService( new RockContext() ).GetChartData( groupBy, graphBy, startDate, endDate, groupIds, campusIds, dataViewId, scheduleIds );
        }


        /// <summary>
        /// Gets the scheduled.
        /// </summary>
        /// <param name="attendanceOccurrenceId">The attendance occurrence identifier.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [System.Web.Http.Route( "api/Attendances/GetScheduled" )]
        [HttpGet]
        public IEnumerable<ScheduledAttendanceItem> GetScheduled( int attendanceOccurrenceId )
        {
            var rockContext = new RockContext();
            var attendanceService = new AttendanceService( rockContext );

            var scheduledAttendances = attendanceService.Queryable().Include( a => a.PersonAlias ).Include( a => a.PersonAlias.Person )
                .Where( a => a.OccurrenceId == attendanceOccurrenceId
                 && a.RequestedToAttend == true || a.ScheduledToAttend == true );

            var result = scheduledAttendances.ToList().Select( a => 
             {
                 string status = "pending";
                 if ( a.DeclineReasonValueId.HasValue )
                 {
                     status = "declined";
                 }
                 else if ( a.ScheduledToAttend == true )
                 {
                     status = "scheduled";
                 }

                 return new ScheduledAttendanceItem
                 {
                     AttendanceId = a.Id,
                     Status = status,
                     PersonName = a.PersonAlias.Person.FullName
                 };
             } );
            

            return result;
        }

        /// <summary>
        /// Schedules a person to an attendance
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <param name="attendanceOccurrenceId">The attendance occurrence identifier.</param>
        [Authenticate, Secured]
        [System.Web.Http.Route( "api/Attendances/ScheduledPersonAssign" )]
        [HttpPut]
        public Attendance ScheduledPersonAssign( int personId, int attendanceOccurrenceId )
        {
            var rockContext = new RockContext();
            var attendanceService = new AttendanceService( rockContext );
            var scheduledAttendance = attendanceService.Queryable()
                .FirstOrDefault( a => a.PersonAlias.PersonId == personId
                    && a.OccurrenceId == attendanceOccurrenceId );

            var currentPersonAlias = this.GetPersonAlias();

            if ( scheduledAttendance == null )
            {
                var personAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( personId );
                var attendanceOccurrence = new AttendanceOccurrenceService( rockContext ).Get( attendanceOccurrenceId );
                var scheduledDateTime = attendanceOccurrence.OccurrenceDate.Add( attendanceOccurrence.Schedule.StartTimeOfDay );
                scheduledAttendance = new Attendance
                {
                    PersonAliasId = personAliasId,
                    OccurrenceId = attendanceOccurrenceId,
                    ScheduledByPersonAliasId = currentPersonAlias.Id,
                    StartDateTime = scheduledDateTime,
                    DidAttend = false,
                    RequestedToAttend = true,
                    ScheduledToAttend = false,
                };

                attendanceService.Add( scheduledAttendance );
                rockContext.SaveChanges();
            }

            return scheduledAttendance;
        }
    }

    public class ScheduledAttendanceItem
    {
        public int AttendanceId { get; set; }
        public string Status { get; set; }
        public string PersonName { get; set; }
    }
}
