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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

[DisplayName( "Group Volunteer Schedule Status Board" )]
[Category( "Groups" )]
[Description( "Scheduler can see overview of current schedules by groups and dates." )]

[SlidingDateRangeField(

   name: "Date Range",
   description: "How many weeks into the future should bedisplayed.",
   required: false,
   defaultValue: "Next|6|Week||",
   enabledSlidingDateRangeTypes: "Next,Upcoming,Current",
   order: 0,
   key: AttributeKeys.DateRange )]

[GroupField(
    name: "Groups",
    description: "A parent group to start from when allowing someone to pick one or more groups to view.",
    required: false,
    order: 0,
    key: AttributeKeys.Groups )]

public partial class GroupVolunteerScheduleStatusBoard : RockBlock
{
    #region Fields
    protected static class AttributeKeys
    {
        public const string Groups = "Groups";
        public const string DateRange = "DateRange";
    }

    protected static class UserPreferenceKeys
    {
        public const string VolunteerScheduleMaxWeeks = "volunteer-schedule-status-maxweeks";
        public const string VolunteerScheduleStatusGroups = "volunteer-schedule-status-group";
    }

    /// <summary>
    /// Occurrence Person is used to bring person display information together
    /// </summary>
    private class OccurrencePerson
    {
        public string NickName { get; set; }
        public string LastName { get; set; }
        public Attendance Attendance { get; set; }
    }

    /// <summary>
    /// Status Cell is used to bring together
    /// the information of the Columns and Row into a single object
    /// </summary>
    private class StatusCell
    {
        public Guid Id { get; set; }
        public DateTime OccurrenceDate { get; set; }
        public DateTime? SortDateTime { get; set; }
        public List<GroupLocation> GroupLocations { get; set; }
        public Group Group { get; set; }
        public Location Location { get; set; }
        public Schedule Schedule { get; set; }
        public List<GroupLocationScheduleConfig> GroupLocationConfigs { get; set; }
        public List<OccurrencePerson> OccurrencePersons { get; set; }
    }

    private List<StatusCell> _occurrenceScheduleCells = new List<StatusCell>();
    private int? _numberOfWeeks = 0;
    private DateRange _dateRange;

    /// <summary>
    /// Set by user preferences or default attribute
    /// </summary>
    private string[] _selectedGroupIds { get; set; }

    #endregion

    #region Base Control Methods

    /// <summary>
    /// Handles the Load event of the Page control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    protected void Page_Load( object sender, EventArgs e )
    {
        base.OnInit( e );
        if ( !IsPostBack )
        {
            UpdateGroupAndDateRangeControls();
            GetDataAndRenderResults();
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Call get user preference and set the selected group. on Group Picker 
    /// </summary>
    private void SetSelectedGroup()
    {
        _selectedGroupIds = this.GetUserPreference( UserPreferenceKeys.VolunteerScheduleStatusGroups ).SplitDelimitedValues( false );
        //Set Root Group
        using ( var rockContext = new RockContext() )
        {
            var groupGuid = this.GetAttributeValue( AttributeKeys.Groups ).AsGuidOrNull();
            if ( groupGuid.HasValue )
            {
                var parentGroup = new GroupService( rockContext ).Get( groupGuid.Value );
                gpGroups.RootGroupId = parentGroup.Id;
            }
        }

        if ( _selectedGroupIds.Length == 0 )
        {
            nbGroupsWarning.Visible = true;
            ltContentPlaceholer.Visible = false;
        }
        else
        {
            gpGroups.SetValues( _selectedGroupIds.ToList().AsIntegerList() );
        }
    }

    /// <summary>
    /// Call get user prefernce and set the  number of weeks
    /// on the date picker
    /// </summary>
    private void SetSelectedNumberOfWeeks()
    {
        var dateRangeNumberOfWeeks = this.GetUserPreference( UserPreferenceKeys.VolunteerScheduleMaxWeeks ).AsIntegerOrNull();
        // get default date range
        var dateRangeAttributes = GetAttributeValue( AttributeKeys.DateRange );
        var delimitedDateRange = dateRangeAttributes.SplitDelimitedValues( false );

        if ( delimitedDateRange.Length > 2 )
        {
            // extract the number of weeks from the default date attribute value
            var defaultWeeks = delimitedDateRange[1].AsInteger();
            //use user preference if diffrent;
            if ( dateRangeNumberOfWeeks != defaultWeeks )
            {
                dateRangeAttributes = string.Format( "Next|{0}|Week||", dateRangeNumberOfWeeks );
                rsDateRange.SelectedValue = dateRangeNumberOfWeeks;
            }
            else
            {
                rsDateRange.SelectedValue = defaultWeeks;
            }
        }

        // set date range 
        _dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dateRangeAttributes ?? "-1||" );
    }

    /// <summary>
    /// Gets the data using a context and then disconnect
    /// </summary>
    private void GetData()
    {
        if ( _selectedGroupIds.Length != 0 )
        {
            using ( var rockContext = new RockContext() )
            {
                int[] groupIds = _selectedGroupIds.AsIntegerList().ToArray();
                var occurrences = new AttendanceOccurrenceService( rockContext ).GetOccurrencesGroupedByGroupsAndDateRange( groupIds, _dateRange );
                BuildDataSourceCells( occurrences );
            }
        }
    }

    /// <summary>
    /// Builds the data source cells.
    /// Pass in the Occurrences that are grouped by date
    /// fill list of datasource Schedule Cells
    /// </summary>
    /// <param name="occurrences">The occurrences.</param>
    private void BuildDataSourceCells( IQueryable<IGrouping<DateTime, AttendanceOccurrence>> occurrences )
    {
        List<StatusCell> cells = new List<StatusCell>();
        var allDates = occurrences.Select( k => k.Key ).ToList();
        foreach ( var date in allDates )
        {
            var occurrenceGroupByDate = occurrences.Where( k => k.Key == date );
            foreach ( var occurrence in occurrenceGroupByDate )
            {
                var occurrencesByDate = occurrence.Select( o => o );
                foreach ( var dateOccurence in occurrencesByDate )
                {
                    // Represents cell information to include information for grouping and sorting
                    var cell = new StatusCell
                    {
                        Id = Guid.NewGuid(),
                        OccurrenceDate = dateOccurence.OccurrenceDate,
                        Group = dateOccurence.Group,
                        Location = dateOccurence.Location,
                        OccurrencePersons = BuildListOfOccurencePersons( dateOccurence.Attendees.ToList() ),
                        Schedule = dateOccurence.Schedule,
                        GroupLocations = dateOccurence.Group.GroupLocations.ToList(),
                        GroupLocationConfigs = BuildListOfConfigurationsByGroupLocations( dateOccurence.Group.GroupLocations ),
                        SortDateTime = dateOccurence.Schedule.GetNextStartDateTime( dateOccurence.OccurrenceDate ),
                    };
                    cells.Add( cell );
                }
            }
        }

        _occurrenceScheduleCells.AddRange( cells.OrderBy( ord => ord.SortDateTime ) );
    }

    /// <summary>
    /// Selects the distinct groups.
    /// </summary>
    /// <returns></returns>
    private List<StatusCell> SelectDistinctGroups()
    {
        List<StatusCell> returnList = new List<StatusCell>();
        foreach ( var statusColumn in _occurrenceScheduleCells )
        {
            if ( !returnList.Where( l => l.Group.Id == statusColumn.Group.Id && l.Location.Id == statusColumn.Location.Id ).Any() )
            {
                returnList.Add( statusColumn );
            }
        }

        var group = returnList.GroupBy( g => g.Group.Id ).Select( g => g.FirstOrDefault() ).ToList();
        return group;
    }
    private List<GroupLocationScheduleConfig> BuildListOfConfigurationsByGroupLocations( ICollection<GroupLocation> groupLocations )
    {
        List<GroupLocationScheduleConfig> groupLocationConfiguration = new List<GroupLocationScheduleConfig>();
        foreach ( var location in groupLocations )
        {
            var configurations = location.GroupLocationScheduleConfigs.ToList();
            groupLocationConfiguration.AddRange( configurations );
        }
        return groupLocationConfiguration;
    }

    /// <summary>
    /// Builds the list of occurence persons from Attendances.
    /// </summary>
    /// <param name="attendances">The attendances.</param>
    /// <returns></returns>
    private List<OccurrencePerson> BuildListOfOccurencePersons( List<Attendance> attendances )
    {
        List<OccurrencePerson> persons = new List<OccurrencePerson>();
        foreach ( var attendance in attendances )
        {
            persons.Add( new OccurrencePerson
            {
                Attendance = attendance,
                NickName = attendance.PersonAlias.Person.NickName,
                LastName = attendance.PersonAlias.Person.LastName,
            } );
        }
        return persons;
    }

    /// <summary>
    /// Builds the unique locations.
    /// </summary>
    /// <param name="groupLocation">The group location.</param>
    /// <returns></returns>
    private List<GroupLocation> BuildUniqueLocations( StatusCell groupLocation )
    {
        List<GroupLocation> results = new List<GroupLocation>();
        foreach ( var location in groupLocation.GroupLocations )
        {
            if ( !results.Where( l => l.Id == location.Id ).Any() )
            {
                results.Add( location );
            }
        }
        return results;
    }

    /// <summary>
    /// Gets the group location configuration by attendance.
    /// </summary>
    /// <param name="attendance">The attendance.</param>
    /// <param name="groupLocation">The item.</param>
    /// <returns></returns>
    private GroupLocationScheduleConfig GetGroupLocationConfigByAttendance( Attendance attendance, GroupLocation groupLocation )
    {
        var currentScheduleId = attendance.Occurrence.Schedule.Id;
        var groupLocationId = groupLocation.Id;
        var currentConfig = attendance.Occurrence.Group.GroupLocations
            .Where( loc => loc.Id == groupLocation.Id )
                .Select( cnfg => cnfg.GroupLocationScheduleConfigs
                .Where( gls => gls.ScheduleId == currentScheduleId )
                .Select( r => r = new GroupLocationScheduleConfig
                {
                    ScheduleId = r.ScheduleId,
                    Schedule = r.Schedule,
                    GroupLocationId = r.GroupLocationId,
                    GroupLocation = r.GroupLocation,
                    MinimumCapacity = r.MinimumCapacity,
                    DesiredCapacity = r.DesiredCapacity,
                    MaximumCapacity = r.MaximumCapacity
                } ) ).FirstOrDefault();

        return currentConfig.First();
    }

    #endregion

    #region Render Methods

    /// <summary>
    /// Gets the data and calls render results.
    /// </summary>
    private void GetDataAndRenderResults()
    {
        GetData();
        //if we have cells then render results 
        if ( _occurrenceScheduleCells.Count() > 0 )
        {
            RenderResults();
        }
    }

    // Structure of Result:
    //  <table class='table'>
    //  <thead>
    //  <tr>
    //    <th scope = "col" ></ th >
    //    <th scope="col">
    //      <span class="date">Jul 8, 2018</span>
    //      <span class="day-time">Sunday 8am</span>
    //    </th>
    //  </thead>
    //  <tbody id = "groupid-###" >
    //  <tr class="group-heading thead-dark">
    //    <th colspan = "6" > Kids Volunteers</th>
    //  </tr>
    //  <tr class="location-row">
    //    <th scope = "row" > Bears Room</th>
    //    <td>
    //      <ul class="location-list">
    //        <li class="person attending">Cras justo odio</li>
    //      </ul>
    //    </td>
    //  </tr>
    // </tbody>
    // </table>
    private void RenderResults()
    {
        List<Tuple<string, DateTime>> tupleHeaderDates = new List<Tuple<string, DateTime>>();
        StringBuilder sbTable = new StringBuilder();
        StringBuilder sbBody = new StringBuilder();

        BuildHeader( tupleHeaderDates, sbTable );

        var distinctGroups = SelectDistinctGroups();

        foreach ( var group in distinctGroups )
        {
            BuildGroupAndLocationRowHeader( sbBody, group );
            BuildBodyColumnsAndRows( sbBody, tupleHeaderDates, group );
        }

        sbTable.AppendLine( sbBody.ToString() );
        sbTable.AppendLine( "</table>" );

        this.ltContentPlaceholer.Text = sbTable.ToString();
        this.ltContentPlaceholer.Visible = true;
    }

    /// <summary>
    /// Builds the header.
    /// </summary>
    /// <param name="columnDateTimes">The column date times.</param>
    /// <param name="sbTable">The sb head.</param>
    private void BuildHeader( List<Tuple<string, DateTime>> columnScheduleDates, StringBuilder sbTable )
    {
        // Head
        sbTable.AppendLine( "<table class='table'>" );
        sbTable.AppendLine( "<thead>" );
        sbTable.AppendLine( "<tr>" );
        sbTable.AppendLine( " <th scope='col'></th>" );

        int index = 0;

        foreach ( var column in _occurrenceScheduleCells )
        {
            if ( !columnScheduleDates.Where( tup => tup.Item2 == column.OccurrenceDate && tup.Item1 == column.Schedule.Name ).Any() )
            {
                columnScheduleDates.Add( new Tuple<string, DateTime>( column.Schedule.Name, ( DateTime ) column.OccurrenceDate ) );
            }
        }

        foreach ( var tupleHeaderDate in columnScheduleDates )
        {
            sbTable.AppendLine( "<th scope='col'>" );
            sbTable.AppendLine( string.Format( "<span class='date'>{0}</span>", tupleHeaderDate.Item2.ToString( "MMMM dd, yyyy" ) ) );
            sbTable.AppendLine( "</br>" );
            sbTable.AppendLine( string.Format( "<span class='day-time'>{0}</span>", tupleHeaderDate.Item1 ) );
            sbTable.AppendLine( "</th>" );
            index++;
        }

        sbTable.AppendLine( "</tr>" );
        sbTable.AppendLine( "</thead>" );
    }

    /// <summary>
    /// Builds the body columns and rows.
    /// </summary>
    /// <param name="sbBody">The sb body.</param>
    /// <param name="columnDate">The column date times.</param>
    /// <param name="uniqueLocations">The unique location.</param>
    private void BuildBodyColumnsAndRows( StringBuilder sbBody, List<Tuple<string, DateTime>> columnScheduleDates, StatusCell cell )
    {
        // list of locations based on a group
        var grouplocations = _occurrenceScheduleCells
            .Where( c => c.Group.Id == cell.Group.Id )
            .Select( res => res.GroupLocations ).FirstOrDefault();

        foreach ( var groupLocation in grouplocations )
        {
            // build out row header of locations 
            sbBody.AppendLine( "<tr class='location-row'>" );
            sbBody.AppendLine( string.Format( "<th scope= 'row'>{0}</th>", groupLocation.Location.Name ) );
            var cellsByLocation = _occurrenceScheduleCells.Where( cel => cel.Location == groupLocation.Location );

            //itterate through list of schedule names and associated date
            foreach ( var scheduleDate in columnScheduleDates )
            {

                // cell by schedule name and occurrance date
                var scheduleOccurrenceCell = cellsByLocation
                    .Where( cel => cel.OccurrenceDate == scheduleDate.Item2 && cel.Schedule.Name == scheduleDate.Item1 )
                    .Select( cel => cel ).FirstOrDefault();

                // maybe null since no occurrence was create for this location and date
                var currentOccurrenceSchedule = scheduleOccurrenceCell != null ? scheduleOccurrenceCell.Schedule : null;

                var groupLocationConfiguration = currentOccurrenceSchedule == null ? null : scheduleOccurrenceCell.GroupLocationConfigs
                    .Where( cnfg => cnfg.Schedule == currentOccurrenceSchedule )
                    .Select( cnfg => cnfg ).FirstOrDefault();

                // get the capicities that have been defined for this schedule and date
                // if Desired has not been configured then use Minium otherwise use 0 
                var capacityRequested = groupLocationConfiguration == null ? 0 : groupLocationConfiguration.DesiredCapacity != null ? groupLocationConfiguration.DesiredCapacity : groupLocationConfiguration.MinimumCapacity;

                if ( scheduleOccurrenceCell != null && scheduleOccurrenceCell.OccurrencePersons != null && scheduleOccurrenceCell.OccurrencePersons.Count > 0 )
                {
                    var neededPersons = capacityRequested - scheduleOccurrenceCell.OccurrencePersons.Count;

                    var prepend = "Person".PluralizeIf( neededPersons != 1 );
                    // build the list of scheduled persons
                    sbBody.Append( "<td>" );
                    sbBody.AppendLine( "<ul class='location-list'>" );

                    foreach ( var attendance in scheduleOccurrenceCell.OccurrencePersons )
                    {
                        string status;
                        switch ( attendance.Attendance.RSVP )
                        {
                            case RSVP.No:
                                status = "person declined";
                                break;
                            case RSVP.Yes:
                                status = "person attending";
                                break;
                            case RSVP.Maybe:
                                status = "person pending";
                                break;
                            case RSVP.Unknown:
                                status = "person pending";
                                break;
                            default:
                                status = "person pending";
                                break;
                        }

                        sbBody.AppendLine( string.Format( "<li class='{0}'>{1} {2}</li>", status, attendance.NickName, attendance.LastName ) );
                    };

                    //if more are still needed
                    if ( neededPersons > 0 )
                    {
                        sbBody.AppendLine( string.Format( "<li class='unassigned unassigned-meta'>{0} {1} Needed</li>", neededPersons, prepend ) );
                    }

                    sbBody.AppendLine( "</ul>" );
                    sbBody.AppendLine( "</td>" );
                }
                else
                {
                    // no pepole scheduled handle capacity  
                    if ( capacityRequested > 0 )
                    {
                        sbBody.Append( "<td>" );
                        sbBody.AppendLine( "<ul class='location-list'>" );
                        sbBody.AppendLine( string.Format( "<li class='unassigned unassigned-meta'>{0} {1} Needed</li>", capacityRequested, "Peopole" ) );
                        sbBody.AppendLine( "</ul>" );
                        sbBody.AppendLine( "</td>" );
                    }
                    else
                    { //no occurrence exists
                        sbBody.AppendLine( "<td></td>" );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Builds the group and location row header.
    /// </summary>
    /// <param name="sbBody">The sb body.</param>
    /// <param name="groupLocation">The group location.</param>
    private void BuildGroupAndLocationRowHeader( StringBuilder sbBody, StatusCell groupLocation )
    {
        int colspan = _occurrenceScheduleCells.Count + 1;
        sbBody.AppendLine( string.Format( "<tbody id='groupid-{0}'>", groupLocation.Group.Id ) );
        sbBody.AppendLine( "<tr class='group-heading thead-dark'>" );
        sbBody.AppendLine( string.Format( "<th colspan='{0}'>{1}</th>", colspan, groupLocation.Group.Name ) );
        sbBody.AppendLine( "</tr>" );
    }

    #endregion

    #region Events

    /// <summary>
    /// Handles the Click event of the btnGroups control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected void btnGroups_Click( object sender, EventArgs e )
    {
        dlgGroups.Show();
    }

    /// <summary>
    /// Handles the Click event of the btnDates control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected void btnDates_Click( object sender, EventArgs e )
    {
        dlgDateRangeSlider.Show();
    }

    protected void dlgGroups_SaveClick( object sender, EventArgs e )
    {
        var selectedGroup = gpGroups.SelectedValues.ToList().AsDelimited( "," );
        if ( selectedGroup != null )
        {
            this.SetUserPreference( UserPreferenceKeys.VolunteerScheduleStatusGroups, selectedGroup.ToString() );
            nbGroupsWarning.Visible = false;
        }
        _numberOfWeeks = rsDateRange.SelectedValue;
        UpdateGroupAndDateRangeControls();
        dlgGroups.Hide();
    }

    protected void dlgDateRangeSlider_SaveClick( object sender, EventArgs e )
    {
        _numberOfWeeks = rsDateRange.SelectedValue;

        if ( _numberOfWeeks.HasValue )
        {
            this.SetUserPreference( UserPreferenceKeys.VolunteerScheduleMaxWeeks, _numberOfWeeks.ToString() );
        }

        UpdateGroupAndDateRangeControls();
        dlgDateRangeSlider.Hide();
    }

    private void UpdateGroupAndDateRangeControls()
    {
        SetSelectedGroup();
        SetSelectedNumberOfWeeks();
        GetDataAndRenderResults();
    }

    #endregion
}