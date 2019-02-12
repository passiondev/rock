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
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

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
        protected List<Attendance> attendances = new List<Attendance>();

        public string SeriesColorsJSON { get; set; }

        public string BarChartScheduledJSON { get; set; }
        public string BarChartNoResponseJSON { get; set; }
        public string BarChartDeclinesJSON { get; set; }
        public string BarChartAttendedJSON { get; set; }
        public string BarChartCommitedNoShowJSON { get; set; }
        public string BarChartTentativeNoShowJSON { get; set; }

        /// <summary>
        /// Gets or sets the line chart time format. see http://momentjs.com/docs/#/displaying/format/
        /// </summary>
        /// <value>
        /// The line chart time format.
        /// </value>
        public string BarChartTimeFormat { get; set; }


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
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        
            if( Page.IsPostBack )
            {
                
            }
        }

        #endregion Overrides

        protected void RegisterScript()
        {
            string script = string.Format( @"
var barChartScheduled = '{0}';
var barChartNoResponse = '{1}';
var barChartDeclines = '{2}';
var barChartAttended = '{3}';
var barChartCommitedNoShow = '{4}';
var barChartTentativeNoShow = '{5}';

var barchartCtx = $('#{6}')[0].getContext('2d');

var barChart = new Chart(barchartCtx, {{
    type: 'bar',
    data: {{
        labels: ['1', '2', '3'],
        datasets: [{{
            xAxisID: 'Name',
            yAxisID: 'Count',
            label: 'Scheduled',
            data: barChartScheduled
        }}],

    }},
    options: {{
        title: {{
            display: true,
            text: 'My Title'
        }}
    }}
}});
",
            BarChartScheduledJSON,
            BarChartNoResponseJSON,
            BarChartDeclinesJSON,
            BarChartAttendedJSON,
            BarChartCommitedNoShowJSON,
            BarChartTentativeNoShowJSON,
            barChartCanvas.ClientID
            );

            if ( gpGroups.GroupId != null )
            {
                    ScriptManager.RegisterClientScriptBlock( this, this.GetType(), "chartScript", script, true );
            }
        }

        /// <summary>
        /// What to do if the block settings are changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            this.NavigateToCurrentPageReference();
        }

        /// <summary>
        /// Clears the existing data from class var attendances and repopulates it with data for the selected group and filter criteria.
        /// Data is organized by each person in the group.
        /// </summary>
        private void GetAttendanceDataForGroup()
        {
            attendances.Clear();
            // Source data for all tables and graphs
            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );
                var groupAttendances = attendanceService
                    .Queryable()
                    .AsNoTracking()
                    .Where( a => a.Occurrence.GroupId == gpGroups.GroupId );

                if ( sdrpDateRange.DelimitedValues.IsNotNullOrWhiteSpace())
                {
                    var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( sdrpDateRange.DelimitedValues );
                    // parse the date range and add to query
                    if ( dateRange.Start.HasValue )
                    {
                        groupAttendances = groupAttendances.Where( a => DbFunctions.TruncateTime( a.StartDateTime ) >= dateRange.Start.Value );
                    }

                    if ( dateRange.End.HasValue )
                    {
                        groupAttendances = groupAttendances.Where( a => DbFunctions.TruncateTime( a.StartDateTime ) <= dateRange.End.Value );
                    }
                }

                if ( cblLocations.SelectedValues.Any() )
                {
                    // add selected locations to the query
                    groupAttendances = groupAttendances.Where( a => cblLocations.SelectedValuesAsInt.Contains( a.Occurrence.LocationId ?? -1 ) );
                }

                if (cblSchedules.SelectedValues.Any() )
                {
                    // add selected schedules to the query
                    groupAttendances = groupAttendances.Where( a => cblSchedules.SelectedValuesAsInt.Contains( a.Occurrence.ScheduleId ?? -1 ) );
                }

                attendances = groupAttendances.ToList();
            }
        }

        /// <summary>
        /// Clears the existing data from class var attendances and repopulates it with data for the selected person and filter criteria.
        /// </summary>
        private void GetAttendanceDataForPerson()
        {
            // Source data for all tables and graphs
            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );
                var groupAttendances = attendanceService
                    .Queryable()
                    .AsNoTracking()
                    .Where( a => a.PersonAliasId == ppPerson.PersonAliasId );

                if ( sdrpDateRange.DelimitedValues.IsNotNullOrWhiteSpace())
                {
                    // parse the date range and add to query
                    // groupAttendances = groupAttendances.Where
                }

                if ( cblLocations.SelectedValues.Any() )
                {
                    // add selected locations to the query
                    // groupAttendances = groupAttendances.Where
                }

                if (cblSchedules.SelectedValues.Any() )
                {
                    // add selected schedules to the query
                    // groupAttendances = groupAttendances.Where
                }

                attendances = groupAttendances.ToList();
            }
        }

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


        protected void ShowBarGraphForGroup()
        {
            if ( !attendances.Any() )
            {
                return;
            }

            List<SchedulerGroupMember> barchartdata = null;

            this.SeriesColorsJSON = this.GetAttributeValue( "SeriesColors" ).SplitDelimitedValues().ToArray().ToJson();
            this.BarChartTimeFormat = "LL";

            DateTime firstDateTime;
            DateTime lastDateTime;

            if ( sdrpDateRange.DelimitedValues.IsNotNullOrWhiteSpace() )
            {
                var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( sdrpDateRange.DelimitedValues );
                firstDateTime = dateRange.Start.Value;
                lastDateTime = dateRange.End.Value;
                var timeUnit = sdrpDateRange.TimeUnit;
            }
            else
            {
                firstDateTime = attendances.Min( a => a.StartDateTime );
                lastDateTime = attendances.Max( a => a.StartDateTime );
            }

            TimeSpan roundTimeSpan = TimeSpan.FromDays( 1 );

            //var daysCount = ( lastDateTime - firstDateTime ).TotalDays;
            //string groupBy = string.Empty;

            //if ( daysCount / 7 > 26 )
            //{
            //    // if more than 6 months summarize by month
            //    groupBy = "MM";
            //}
            //else if ( daysCount > 30 )
            //{
            //    // if more than 1 month summarize by week
            //}
            //else if ( daysCount > 1 )
            //{
            //    // if more than 1 day summarize by day
            //}
            //else
            //{
            //    // For one day summarize by hour
            //    this.BarChartTimeFormat = "h A";
            //}

            var weeksCount = ( lastDateTime - firstDateTime ).TotalDays / 7;

            if ( weeksCount > 26 )
            {
                // if there is more than 26 weeks worth, summarize by week
                roundTimeSpan = TimeSpan.FromDays( 7 );
            }
            else if ( weeksCount > 3 )
            {
                // if there is more than 3 weeks worth, summarize by day
                roundTimeSpan = TimeSpan.FromDays( 1 );
            }
            else
            {
                // if there is less than 3 weeks worth, summarize by hour
                roundTimeSpan = TimeSpan.FromHours( 1 );
                this.BarChartTimeFormat = "LLLL";
            }


            barchartdata = attendances
                //.GroupBy( a => new { roundedTime = a.Min( b => b.StartDateTime ).Round( roundTimeSpan ) } )
                .GroupBy( a => new { StartTime = a.StartDateTime.ToString( "MM") } )
                .Select( a => new SchedulerGroupMember
                {
                    //Name = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( a.Key.StartTime.AsIntegerOrNull() ?? 1 ),
                    Name = a.Key.StartTime,
                    Scheduled = a.Count(),
                    NoResponse = a.Count( aa => aa.RSVP == RSVP.Unknown ),
                    Declines = a.Count( aa => aa.RSVP == RSVP.No ),
                    Attended = a.Count( aa => aa.DidAttend == true ),
                    CommitedNoShow = a.Count( aa => aa.RSVP == RSVP.Yes && aa.DidAttend == false ),
                    TentativeNoShow = a.Count( aa => aa.RSVP == RSVP.Maybe && aa.DidAttend == false )
                } )
                .ToList();

            barChartCanvas.Style[HtmlTextWriterStyle.Display] = barchartdata.Any() ? string.Empty : "none";
            nbBarChartMessage.Visible = !barchartdata.Any();

            BarChartScheduledJSON = barchartdata.Select( d => d.Scheduled ).ToJson();
            BarChartNoResponseJSON = barchartdata.Select( d => d.NoResponse ).ToJson();
            BarChartDeclinesJSON = barchartdata.Select( d => d.Declines ).ToJson();
            BarChartAttendedJSON = barchartdata.Select( d => d.Attended ).ToJson();
            BarChartCommitedNoShowJSON = barchartdata.Select( d => d.CommitedNoShow ).ToJson();
            BarChartTentativeNoShowJSON = barchartdata.Select( d => d.TentativeNoShow ).ToJson();
        }

        protected void ShowBarGraphForPerson()
        {

        }

        protected void ShowDoughnutGraphForGroup()
        {
            var declineGuid = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.VOLUNTEER_SCHEDULE_DECLINE_REASON );



        }

        protected void ShowDoughnutGraphForPerson()
        {

        }

        protected void ShowGrid()
        {
            var schedulerGroupMembers = new List<SchedulerGroupMember>();

            using ( var rockContext = new RockContext() )
            {
                var personAliasService = new PersonAliasService( rockContext );

                foreach ( var personAliasId in attendances.Select( a => a.PersonAliasId ).Distinct() )
                {
                    var schedulerGroupMember = new SchedulerGroupMember();
                    schedulerGroupMember.Name = personAliasService.GetPerson( personAliasId.Value ).FullName;
                    schedulerGroupMember.Scheduled = attendances.Where( a => a.PersonAliasId == personAliasId.Value ).Count();
                    schedulerGroupMember.NoResponse = attendances.Where( a => a.PersonAliasId == personAliasId.Value && a.RSVP == RSVP.Unknown ).Count();
                    schedulerGroupMember.Declines = attendances.Where( a => a.PersonAliasId == personAliasId.Value && a.RSVP == RSVP.No).Count();
                    schedulerGroupMember.Attended = attendances.Where( a => a.PersonAliasId == personAliasId.Value && a.DidAttend == true ).Count();
                    schedulerGroupMember.CommitedNoShow = attendances.Where( a => a.PersonAliasId == personAliasId.Value && a.RSVP == RSVP.Yes && a.DidAttend == false ).Count();
                    schedulerGroupMember.TentativeNoShow = attendances.Where( a => a.PersonAliasId == personAliasId.Value && a.RSVP == RSVP.Maybe && a.DidAttend == false ).Count();

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
            if ( gpGroups.GroupId != null )
            {
                GetAttendanceDataForGroup();
                ShowGrid();
                ShowBarGraphForGroup();
                ShowDoughnutGraphForGroup();
            }
            else if ( ppPerson.PersonAliasId != null )
            {
                GetAttendanceDataForPerson();
                ShowGrid();
                ShowBarGraphForPerson();
                ShowDoughnutGraphForPerson();
            }

            //RegisterScript();
            // maybe put a warning here or make is so they can't click the button if they don't have a valid selection.
        }

        protected void cblLocations_SelectedIndexChanged( object sender, EventArgs e )
        {
            LoadSchedulesForGroupSelection();
        }

        #endregion Control Events

        protected class SchedulerGroupMember
        {
            public string Name { get; set; }
            public string Key { get { return Name; } }
            public int Scheduled { get; set; }
            public int NoResponse { get; set; }
            public int Declines { get; set; }
            public int Attended { get; set; }
            public int CommitedNoShow { get; set; }
            public int TentativeNoShow { get; set; }

        }

    }
}