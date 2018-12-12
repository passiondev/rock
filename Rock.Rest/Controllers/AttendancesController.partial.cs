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
using System.Runtime.Serialization;
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


        #region Group Scheduler Related

        /// <summary>
        /// Gets the scheduler resources.
        /// </summary>
        /// <param name="schedulerResourceParameters">The scheduler resource parameters.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [System.Web.Http.Route( "api/Attendances/GetSchedulerResources" )]
        [HttpPost]
        public IEnumerable<SchedulerResource> GetSchedulerResources( [FromBody]SchedulerResourceParameters schedulerResourceParameters )
        {
            // remove any 0 ids
            schedulerResourceParameters.ResourceAdditionalPersonIds = schedulerResourceParameters.ResourceAdditionalPersonIds?.Where( a => a > 0 ).ToList();

            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var groupService = new GroupService( rockContext );
            var attendanceService = new AttendanceService( rockContext );
            IQueryable<GroupMember> groupMemberQry = null;
            IQueryable<Person> personQry = null;

            HashSet<int> _groupMemberIdsThatLackGroupRequirements;
            Dictionary<int, DateTime> _personIdLastAttendedDateTime;

            List<SchedulerResource> schedulerResourceList = new List<SchedulerResource>();

            _groupMemberIdsThatLackGroupRequirements = null;

            if ( schedulerResourceParameters.ResourceGroupId.HasValue )
            {
                groupMemberQry = groupMemberService.Queryable().Where( a => a.GroupId == schedulerResourceParameters.ResourceGroupId.Value );
                var resourceGroup = groupService.GetNoTracking( schedulerResourceParameters.ResourceGroupId.Value );
                if ( resourceGroup?.SchedulingMustMeetRequirements == true )
                {
                    _groupMemberIdsThatLackGroupRequirements = new HashSet<int>( new GroupService( rockContext ).GroupMembersNotMeetingRequirements( resourceGroup, false ).Select( a => a.Key.Id ).ToList().Distinct() );
                }
            }

            if ( schedulerResourceParameters.ResourceDataViewId.HasValue )
            {
                var dataView = new DataViewService( rockContext ).Get( schedulerResourceParameters.ResourceDataViewId.Value );

                if ( dataView != null )
                {
                    List<string> errorMessages;
                    personQry = dataView.GetQuery( null, null, out errorMessages ) as IQueryable<Person>;
                }
            }

            var lastAttendedDateTimeQuery = attendanceService.Queryable()
                .Where( a => a.DidAttend == true
                    && a.Occurrence.GroupId == schedulerResourceParameters.AttendanceOccurrenceGroupId
                    && a.Occurrence.ScheduleId == schedulerResourceParameters.AttendanceOccurrenceScheduleId
                    && a.PersonAliasId.HasValue );

            if ( groupMemberQry != null )
            {
                lastAttendedDateTimeQuery.Where( a => groupMemberQry.Any( m => m.PersonId == a.PersonAlias.PersonId ) );
            }
            else if ( personQry != null )
            {
                lastAttendedDateTimeQuery.Where( a => personQry.Any( p => p.Id == a.PersonAlias.PersonId ) );
            }

            _personIdLastAttendedDateTime = lastAttendedDateTimeQuery
                .GroupBy( a => a.PersonAlias.PersonId )
                .Select( a => new
                {
                    PersonId = a.Key,
                    LastScheduledDate = a.Max( x => x.StartDateTime )
                } )
                .ToDictionary( k => k.PersonId, v => v.LastScheduledDate );

            var scheduledAttendanceGroupIdsLookup = attendanceService.Queryable()
                .Where( a => ( a.RequestedToAttend == true || a.ScheduledToAttend == true )
                          && a.Occurrence.ScheduleId == schedulerResourceParameters.AttendanceOccurrenceScheduleId
                          && a.Occurrence.OccurrenceDate == schedulerResourceParameters.AttendanceOccurrenceOccurrenceDate
                          && a.Occurrence.GroupId.HasValue )
                .GroupBy( a => a.PersonAlias.PersonId )
                .Select( a => new
                {
                    PersonId = a.Key,
                    ScheduledOccurrenceGroupIds = a.Select( x => x.Occurrence.GroupId.Value ).ToList()
                } )
                .ToDictionary( k => k.PersonId, v => v.ScheduledOccurrenceGroupIds );


            if ( groupMemberQry != null )
            {
                schedulerResourceList = groupMemberQry.Include( a => a.Person ).ToList().Select( a => new SchedulerResource
                {
                    PersonId = a.PersonId,
                    Note = a.Note,
                    PersonName = a.Person.FullName,
                    HasGroupRequirementsConflict = _groupMemberIdsThatLackGroupRequirements?.Contains( a.Id ) ?? false,
                } ).ToList();
            }
            else if ( personQry != null )
            {
                schedulerResourceList = personQry.ToList().Select( a => new SchedulerResource
                {
                    PersonId = a.Id,
                    Note = null,
                    PersonName = a.FullName,
                    HasGroupRequirementsConflict = false,
                } ).ToList();
            }
            

            if ( schedulerResourceParameters.ResourceAdditionalPersonIds.Any())
            {
                var additionalSchedulerResources = new PersonService( rockContext ).GetByIds( schedulerResourceParameters.ResourceAdditionalPersonIds ).ToList().Select( a => new SchedulerResource
                {
                    PersonId = a.Id,
                    Note = null,
                    PersonName = a.FullName,
                    HasGroupRequirementsConflict = false,
                } ).ToList();

                schedulerResourceList.AddRange( additionalSchedulerResources );
            }

            foreach ( var schedulerResource in schedulerResourceList )
            {
                schedulerResource.LastAttendanceDateTime = _personIdLastAttendedDateTime.GetValueOrNull( schedulerResource.PersonId );
                schedulerResource.HasSchedulingConflict = scheduledAttendanceGroupIdsLookup.GetValueOrNull( schedulerResource.PersonId )?.Any( groupId => groupId != schedulerResourceParameters.AttendanceOccurrenceGroupId ) ?? false;
            }

            return schedulerResourceList;
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
            var conflictingScheduledAttendancesQuery = attendanceService.Queryable();

            var scheduledAttendancesQuery = attendanceService.Queryable().Include( a => a.PersonAlias ).Include( a => a.PersonAlias.Person )
                .Where( a => a.OccurrenceId == attendanceOccurrenceId
                 && a.RequestedToAttend == true || a.ScheduledToAttend == true )
                .Select( a => new
                {
                    a.Id,
                    a.DeclineReasonValueId,
                    a.ScheduledToAttend,
                    a.PersonAlias.Person,
                    // set HasSchedulingConflict = true if the same person is requested/scheduled
                    HasSchedulingConflict = conflictingScheduledAttendancesQuery.Any( c => c.Id != a.Id
                                                                                && c.PersonAlias.PersonId == a.PersonAlias.PersonId
                                                                                && c.RequestedToAttend == true
                                                                                && c.Occurrence.ScheduleId == a.Occurrence.ScheduleId
                                                                                && c.Occurrence.OccurrenceDate == a.Occurrence.OccurrenceDate )
                } );

            var result = scheduledAttendancesQuery.ToList().Select( a =>
             {
                 ScheduledAttendanceItemStatus status = ScheduledAttendanceItemStatus.Pending;
                 if ( a.DeclineReasonValueId.HasValue )
                 {
                     status = ScheduledAttendanceItemStatus.Denied;
                 }
                 else if ( a.ScheduledToAttend == true )
                 {
                     status = ScheduledAttendanceItemStatus.Confirmed;
                 }

                 return new ScheduledAttendanceItem
                 {
                     AttendanceId = a.Id,
                     Status = status.ConvertToString( false ).ToLower(),
                     PersonId = a.Person.Id,
                     PersonName = a.Person.FullName,
                     HasSchedulingConflict = a.HasSchedulingConflict
                 };
             } );


            return result;
        }

        /// <summary>
        /// Unassigns a person from a scheduled attendance
        /// </summary>
        /// <param name="attendanceId">The attendance identifier.</param>
        [Authenticate, Secured]
        [System.Web.Http.Route( "api/Attendances/ScheduledPersonUnassign" )]
        [HttpPut]
        public void ScheduledPersonUnassign( int attendanceId )
        {
            var rockContext = new RockContext();
            var attendanceService = new AttendanceService( rockContext );
            var scheduledAttendance = attendanceService.Get( attendanceId );
            if ( scheduledAttendance != null )
            {
                scheduledAttendance.ScheduledToAttend = false;
                scheduledAttendance.RequestedToAttend = false;
                rockContext.SaveChanges();
            }
            else
            {
                // ignore if there is no attendance record
            }
        }

        /// <summary>
        /// Scheduleds the person confirm.
        /// </summary>
        /// <param name="attendanceId">The attendance identifier.</param>
        [Authenticate, Secured]
        [System.Web.Http.Route( "api/Attendances/ScheduledPersonConfirm" )]
        [HttpPut]
        public void ScheduledPersonConfirm( int attendanceId )
        {
            var rockContext = new RockContext();
            var attendanceService = new AttendanceService( rockContext );
            var scheduledAttendance = attendanceService.Get( attendanceId );
            if ( scheduledAttendance != null )
            {
                scheduledAttendance.ScheduledToAttend = true;
                rockContext.SaveChanges();
            }
            else
            {
                // ignore if there is no attendance record
            }
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
            else
            {
                if ( scheduledAttendance.RequestedToAttend != true )
                {
                    scheduledAttendance.RequestedToAttend = true;
                    rockContext.SaveChanges();
                }
            }

            return scheduledAttendance;
        }

        #endregion Group Scheduler Related
    }

    /// <summary>
    /// 
    /// </summary>
    public class SchedulerResource
    {
        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public int PersonId { get; set; }

        /// <summary>
        /// Gets or sets the name of the person.
        /// </summary>
        /// <value>
        /// The name of the person.
        /// </value>
        public string PersonName { get; set; }

        /// <summary>
        /// Gets or sets the last attendance date time.
        /// </summary>
        /// <value>
        /// The last attendance date time.
        /// </value>
        public DateTime? LastAttendanceDateTime { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        /// <value>
        /// The note.
        /// </value>
        public string Note { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has conflict.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has conflict; otherwise, <c>false</c>.
        /// </value>
        public bool HasConflict => HasGroupRequirementsConflict || HasSchedulingConflict;

        /// <summary>
        /// Gets or sets the conflict note.
        /// </summary>
        /// <value>
        /// The conflict note.
        /// </value>
        public string ConflictNote { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has group requirements conflict.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has group requirements conflict; otherwise, <c>false</c>.
        /// </value>
        public bool HasGroupRequirementsConflict { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has scheduling conflict.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has scheduling conflict; otherwise, <c>false</c>.
        /// </value>
        public bool HasSchedulingConflict { get; set; }
    }


    /// <summary>
    /// 
    /// </summary>
    [RockClientInclude( "Use this as the Content of a api/Attendances/GetSchedulerResources POST" )]
    public class SchedulerResourceParameters
    {
        /// <summary>
        /// Gets or sets the attendance occurrence group identifier.
        /// </summary>
        /// <value>
        /// The attendance occurrence group identifier.
        /// </value>
        [DataMember]
        public int AttendanceOccurrenceGroupId { get; set; }

        /// <summary>
        /// Gets or sets the attendance occurrence schedule identifier.
        /// </summary>
        /// <value>
        /// The attendance occurrence schedule identifier.
        /// </value>
        [DataMember]
        public int AttendanceOccurrenceScheduleId { get; set; }

        /// <summary>
        /// Gets the attendance occurrence occurrence date.
        /// </summary>
        /// <value>
        /// The attendance occurrence occurrence date.
        /// </value>
        [DataMember]
        public DateTime AttendanceOccurrenceOccurrenceDate { get; set; }

        /// <summary>
        /// Gets or sets the resource group identifier.
        /// </summary>
        /// <value>
        /// The resource group identifier.
        /// </value>
        [DataMember]
        public int? ResourceGroupId { get; set; }

        /// <summary>
        /// Gets or sets the resource data view identifier.
        /// </summary>
        /// <value>
        /// The resource data view identifier.
        /// </value>
        [DataMember]
        public int? ResourceDataViewId { get; set; }

        /// <summary>
        /// Gets or sets the resource additional person ids.
        /// </summary>
        /// <value>
        /// The resource additional person ids.
        /// </value>
        [DataMember]
        public List<int> ResourceAdditionalPersonIds { get; set; }

    }


    /// <summary>
    /// 
    /// </summary>
    public class ScheduledAttendanceItem : SchedulerResource
    {
        /// <summary>
        /// Gets or sets the attendance identifier.
        /// </summary>
        /// <value>
        /// The attendance identifier.
        /// </value>
        public int AttendanceId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ScheduledAttendanceItemStatus"/> as a lowercase string
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public string Status { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ScheduledAttendanceItemStatus
    {
        /// <summary>
        /// pending
        /// </summary>
        Pending,

        /// <summary>
        /// confirmed
        /// </summary>
        Confirmed,

        /// <summary>
        /// denied
        /// </summary>
        Denied
    }
}
