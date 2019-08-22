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
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;

using Quartz;

using Rock.Data;
using Rock.Model;

namespace Rock.Jobs
{
    /// <summary>
    /// Job to update Group Members' Group Requirement statuses for requirements that are calculated from SQL or Dataview
    /// </summary>
    [DisallowConcurrentExecution]
    public class CalculateGroupRequirements : IJob
    {
        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public CalculateGroupRequirements()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            var rockContext = new RockContext();
            var groupRequirementService = new GroupRequirementService( rockContext );
            var groupMemberRequirementService = new GroupMemberRequirementService( rockContext );
            var groupMemberService = new GroupMemberService( rockContext );
            var groupService = new GroupService( rockContext );

            // we only need to consider group requirements that are based on a DataView or SQL
            var groupRequirementQry = groupRequirementService.Queryable()
                .Where( a => a.GroupRequirementType.RequirementCheckType != RequirementCheckType.Manual )
                .AsNoTracking();

            var calculationExceptions = new List<Exception>();
            List<int> groupRequirementsCalculatedPersonIds = new List<int>();

            foreach ( var groupRequirement in groupRequirementQry.Include( i => i.GroupRequirementType ).AsNoTracking().ToList() )
            {
                var groupQuery = groupService.Queryable();
                if ( groupRequirement.GroupId.HasValue )
                {
                    groupQuery = groupQuery.Where( g => g.Id == groupRequirement.GroupId );
                }
                else if ( groupRequirement.GroupTypeId.HasValue )
                {
                    groupQuery = groupQuery.Where( g => g.GroupTypeId == groupRequirement.GroupTypeId );
                }
                else
                {
                    // shouldn't happen, but grouprequirement doesn't have a groupId or a GroupTypeId
                    break;
                }

                var groupList = groupQuery.Select( a => new { a.Id, a.Name } ).ToList();
                var groupCount = groupList.Count();
                foreach ( var group in groupList )
                {
                    Stopwatch stopwatchGroup = Stopwatch.StartNew();
                    var stopWatchUpdateResults = new List<double>();
                    var stopWatchSaveChangesResults = new List<double>();
                    try
                    {
                        var currentDateTime = RockDateTime.Now;
                        var qryGroupMemberRequirementsAlreadyOK = groupMemberRequirementService.Queryable().Where( a => a.GroupRequirementId == groupRequirement.Id && a.GroupMember.GroupId == group.Id );

                        if ( groupRequirement.GroupRequirementType.CanExpire && groupRequirement.GroupRequirementType.ExpireInDays.HasValue )
                        {
                            // Expirable: don't recalculate members that already met the requirement within the expiredays (unless they are flagged with a warning)
                            var expireDaysCount = groupRequirement.GroupRequirementType.ExpireInDays.Value;
                            qryGroupMemberRequirementsAlreadyOK = qryGroupMemberRequirementsAlreadyOK.Where( a => !a.RequirementWarningDateTime.HasValue && a.RequirementMetDateTime.HasValue && SqlFunctions.DateDiff( "day", a.RequirementMetDateTime, currentDateTime ) < expireDaysCount );
                        }
                        else
                        {
                            // No Expiration: don't recalculate members that already met the requirement
                            qryGroupMemberRequirementsAlreadyOK = qryGroupMemberRequirementsAlreadyOK.Where( a => a.RequirementMetDateTime.HasValue );
                        }



                        var groupMemberQry = groupMemberService.Queryable();//.Where( a => ( groupRequirement.GroupId.HasValue && groupRequirement.GroupId == a.GroupId ) || ( groupRequirement.GroupTypeId.HasValue && groupRequirement.GroupTypeId == a.Group.GroupTypeId ) && a.GroupId == group.Id ).AsNoTracking();

                        if ( groupRequirement.GroupId.HasValue )
                        {
                            groupMemberQry = groupMemberQry.Where( g => g.Id == groupRequirement.GroupId );
                        }
                        else if ( groupRequirement.GroupTypeId.HasValue )
                        {
                            groupMemberQry = groupMemberQry.Where( g => ( g.Group.GroupTypeId == groupRequirement.GroupTypeId ) && g.GroupId == group.Id );
                        }
                        else
                        {
                            // shouldn't happen, but grouprequirement doesn't have a groupId or a GroupTypeId
                            break;
                        }


                        var personQry = groupMemberQry.Where( a => !qryGroupMemberRequirementsAlreadyOK.Any( r => r.GroupMemberId == a.Id ) ).Select( a => a.Person );

                        var results = groupRequirement.PersonQueryableMeetsGroupRequirement( rockContext, personQry, group.Id, groupRequirement.GroupRoleId ).ToList();
                        groupRequirementsCalculatedPersonIds.AddRange( results.Select( a => a.PersonId ).Distinct() );

                        foreach ( var result in results )
                        {
                            double stopWatchUpdateResultMS;
                            double stopwatchSaveChangesMS;
                            // use a fresh rockContext per Update so that ChangeTracker doesn't get bogged down
                            var rockContextUpdate = new RockContext();
                            var stopWatchUpdateResult = Stopwatch.StartNew();
                            groupRequirement.UpdateGroupMemberRequirementResult( rockContextUpdate, result.PersonId, group.Id, result.MeetsGroupRequirement );
                            stopWatchUpdateResult.Stop();
                            stopWatchUpdateResultMS = stopWatchUpdateResult.Elapsed.TotalMilliseconds;

                            var stopwatchSaveChanges = Stopwatch.StartNew();
                            rockContextUpdate.SaveChanges();

                            stopwatchSaveChanges.Stop();
                            stopwatchSaveChangesMS = stopwatchSaveChanges.Elapsed.TotalMilliseconds;

                            stopWatchUpdateResults.Add( stopWatchUpdateResultMS );
                            stopWatchSaveChangesResults.Add( stopwatchSaveChangesMS );
                        }
                    }
                    catch ( Exception ex )
                    {
                        calculationExceptions.Add( new Exception( string.Format( "Exception when calculating group requirement: {0} ", groupRequirement ), ex ) );
                    }

                    stopwatchGroup.Stop();
                    var stopWatchResultsMessage = string.Empty;
                    if ( stopWatchUpdateResults.Any() )
                    {
                        stopWatchResultsMessage = $", stopWatchUpdateResults.Count:{stopWatchUpdateResults.Count}, stopWatchUpdateResults.Average:[{stopWatchUpdateResults.Average()}ms], stopWatchSaveChangesResults.Average: [{stopWatchSaveChangesResults.Average()}]ms";
                    }

                    Debug.WriteLine( $"[{stopwatchGroup.Elapsed.TotalMilliseconds}ms] {group.Name}{stopWatchResultsMessage}" );
                }
            }

            context.Result = $"{groupRequirementQry.Count()} group member requirements re-calculated for {groupRequirementsCalculatedPersonIds.Distinct().Count()} people";

            if ( calculationExceptions.Any() )
            {
                throw new AggregateException( "One or more group requirement calculations failed ", calculationExceptions );
            }
        }
    }
}
