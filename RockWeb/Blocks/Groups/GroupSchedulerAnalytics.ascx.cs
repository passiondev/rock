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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Groups
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Group Scheduler Analytics" )]
    [Category( "Groups" )]
    [Description( "Provides some visibility into scheduling accountability. Shows check-ins, missed confirmations, declines, and decline reasons with ability to filter by group, date range, data view, and person." )]

    [TextField( "Series Colors", "A comma-delimited list of colors that the Clients chart will use.", false, "#5DA5DA,#60BD68,#FFBF2F,#F36F13,#C83013,#676766", order: 0 )]
    public partial class GroupSchedulerAnalytics : RockBlock
    {
        #region Properties
        public string SeriesColorsJSON { get; set; }

        #endregion Properties


        #region Overrides
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // NOTE: moment.js needs to be loaded before chartjs
            RockPage.AddScriptLink( "~/Scripts/moment.min.js", true );
            RockPage.AddScriptLink( "~/Scripts/Chartjs/Chart.js", true );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            //this.BlockUpdated += Block_BlockUpdated;
            //this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        
            if( !Page.IsPostBack )
            {
                
            }
        }

        #endregion Overrides

        /// <summary>
        /// Populates the locations checkbox list for the selected group
        /// </summary>
        protected void LoadLocationsForGroupSelection()
        {
            using ( var rockContext = new RockContext() )
            {
                var groupLocationService = new GroupLocationService( rockContext );
                var locations = groupLocationService
                    .Queryable()
                    .Where( gl => gl.GroupId == gpGroups.GroupId )
                    .Where( gl => gl.Location.IsActive == true )
                    .OrderBy( gl => gl.Order )
                    .ThenBy( gl => gl.Location.Name )
                    .Select( gl => gl.Location )
                    .ToList();

                cblLocations.DataValueField = "Id";
                cblLocations.DataTextField = "Name";
                cblLocations.DataSource = locations;
                cblLocations.DataBind();
            }
        }

        /// <summary>
        /// Populates the locations checkbox list for the selected person.
        /// </summary>
        protected void LoadLocationsForPersonSelection()
        {
            using ( var rockContext = new RockContext() )
            {
                var groupLocationService = new GroupLocationService( rockContext );
                var locations = groupLocationService
                    .Queryable()
                    .AsNoTracking()
                    .Where( gl => gl.GroupMemberPersonAliasId == ppPerson.PersonAliasId )
                    .Where( gl => gl.Location.IsActive == true )
                    .OrderBy( gl => gl.Order )
                    .ThenBy( gl => gl.Location.Name )
                    .Select( gl => gl.Location)
                    .ToList();

                cblLocations.DataValueField = "Id";
                cblLocations.DataTextField = "Name";
                cblLocations.DataSource = locations;
                cblLocations.DataBind();
            }
        }

        /// <summary>
        /// Populates the schedules checkbox list for the selected group and locations
        /// </summary>
        protected void LoadSchedulesForGroupSelection()
        {
            using ( var rockContext = new RockContext() )
            {
                var groupLocationService = new GroupLocationService( rockContext );
                var schedules = groupLocationService
                    .Queryable()
                    .AsNoTracking()
                    .Where( gl => gl.GroupId == gpGroups.GroupId )
                    .Where( gl => cblLocations.SelectedValuesAsInt.Contains( gl.Location.Id ) )
                    .SelectMany( gl => gl.Schedules )
                    .DistinctBy( s => s.Guid )
                    .ToList();

                cblSchedules.DataValueField = "Id";
                cblSchedules.DataTextField = "Name";
                cblSchedules.DataSource = schedules;
                cblSchedules.DataBind();
            }
        }

        /// <summary>
        /// Populates the schedules checkbox list for the selected person and locations
        /// </summary>
        protected void LoadSchedulesForPersonSelection()
        {
            using ( var rockContext = new RockContext() )
            {
                var groupLocationService = new GroupLocationService( rockContext );
                var schedules = groupLocationService
                    .Queryable()
                    .AsNoTracking()
                    .Where( gl => gl.GroupMemberPersonAliasId == ppPerson.PersonAliasId )
                    .Where( gl => cblLocations.SelectedValuesAsInt.Contains( gl.LocationId ) )
                    .SelectMany( gl => gl.Schedules )
                    .DistinctBy( s => s.Guid )
                    .ToList();

                cblSchedules.DataValueField = "Id";
                cblSchedules.DataTextField = "Name";
                cblSchedules.DataSource = schedules;
                cblSchedules.DataBind();
            }
        }


        protected void ShowBarGraph()
        {

        }

        protected void ShowDoughnutGraph()
        {

        }

        protected void ShowTable()
        {

        }

        protected void ShowGridForGroup()
        {
            var schedulerGroupMembers = new List<SchedulerGroupMember>();

            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );
                var attendances = attendanceService
                    .Queryable()
                    .AsNoTracking()
                    .Where( a => a.Occurrence.GroupId == gpGroups.GroupId )
                    .ToList();

                
                foreach ( var attendance in attendances )
                {
                    var schedulerGroupMember = new SchedulerGroupMember();
                    // set the props here
                    //SELECT PersonAliasId, COUNT(Id) AS Scheduled FROM #tmp GROUP BY PersonAliasId
                    //SELECT PersonAliasId, COUNT(Id) AS NoResponse FROM #tmp WHERE [RSVP] = 3 GROUP BY PersonAliasId
                    //SELECT PersonAliasId, COUNT(Id) AS Declines FROM #tmp WHERE [RSVP] = 0 GROUP BY PersonAliasId
                    //SELECT PersonAliasId, COUNT(Id) AS [Attended] FROM #tmp WHERE [DidAttend] = 1 GROUP BY PersonAliasId
                    //SELECT PersonAliasId, COUNT(Id) AS CommitedNoShow FROM #tmp WHERE [RSVP] = 1 AND [DidAttend] = 0 GROUP BY PersonAliasId
                    //SELECT PersonAliasId, COUNT(Id) AS TentativeNoShow FROM #tmp WHERE [RSVP] = 2 AND [DidAttend] = 0 GROUP BY PersonAliasId


                    schedulerGroupMembers.Add( schedulerGroupMember );
                }

            }

            gData.DataSource = schedulerGroupMembers;
            gData.DataBind();
        }

        #region Control Events

        protected void gpGroups_SelectItem( object sender, EventArgs e )
        {
            LoadLocationsForGroupSelection();
        }

        protected void ppPerson_SelectPerson( object sender, EventArgs e )
        {
            LoadLocationsForPersonSelection();
            LoadSchedulesForPersonSelection();
        }

        protected void btnUpdate_Click( object sender, EventArgs e )
        {
            ShowBarGraph();
            ShowDoughnutGraph();
            ShowTable();
        }

        protected void cblLocations_SelectedIndexChanged( object sender, EventArgs e )
        {
            LoadSchedulesForGroupSelection();
        }

        #endregion Control Events

        protected class SchedulerGroupMember
        {
            public Person person { get; set; }
            public int Scheduled { get; set; }
            public int NoResponse { get; set; }
            public int Declines { get; set; }
            public int Attended { get; set; }
            public int CommitedNoShow { get; set; }
            public int TentativeNoShow { get; set; }

        }

    }
}