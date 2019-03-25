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
   "Date Range",
   Description = "How many weeks into the future should be displayed.",
   IsRequired = false,
   DefaultValue = "Next|6|Week||",
   EnabledSlidingDateRangeTypes = "Next,Upcoming,Current",
   Order = 0,
   Key = AttributeKeys.DateRange )]

[GroupField(
    "Groups",
    Description = "A parent group to start from when allowing someone to pick one or more groups to view.",
    IsRequired = false,
    Order = 0,
    Key = AttributeKeys.Groups )]

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
    /// Capicity deatail is use to bring results of Attendance Occurrence
    /// Group Group Schedule Config into information required for support of rendering status
    /// </summary>
    public class CapacityDetail
    {
        public int ScheduleId { get; set; }
        public int? CapcityMinimum { get; set; }
        public int? CapacityDesired { get; set; }
        public int? CapityMaximum { get; set; }
    }

    /// <summary>
    /// Group Info is used bring results of Attendance Occurrence
    /// record into information required to render header and header rows
    /// </summary>
    public class GroupInfo
    {
        internal DateTime DateTime;
        public DateTime Occurancedate { get; set; }
        public List<GroupInfoDetail> Groups { get; internal set; }
    }

    /// <summary>
    /// Group info detail is used to bring results of the entity Attendance Occurrence
    /// record into information required for support of rendering the body
    /// </summary>
    public class GroupInfoDetail
    {
        public int OccurrenceId { get; set; }
        public string ScheduleName { get; set; }
        public string GroupName { get; set; }
        public string LocationName { get; set; }
        public int? ScheduleId { get; internal set; }
        public int? GroupId { get; internal set; }
        public int? LocationId { get; internal set; }
        public DateTime? SortDateTime { get; internal set; }
        public Schedule Schedule { get; internal set; }
        public List<List<CapacityDetail>> CapcityDetail { get; set; }
    }

    /// <summary>
    /// Occurrence Person is used to bring person display information together
    /// </summary>
    private class OccurrencePerson
    {
        public string NickName { get; set; }
        public string LastName { get; set; }
        public RSVP RSVP { get; set; }
    }

    /// <summary>
    /// Status Cell is used to bring together
    /// the information of the Columns and Row into a single object
    /// </summary>
    private class StatusCell
    {
        public Guid Guid { get; set; }
        public DateTime OccurrenceDate { get; set; }
        public DateTime? SortDateTime { get; set; }
        public string GroupName { get; set; }
        public int? GroupId { get; set; }
        public string LocationName { get; set; }
        public int? LocationId { get; set; }
        public string ScheduleName { get; set; }
        public int? ScheduleId { get; set; }
        public CapacityDetail CapacityDetail { get; set; }
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
        }
    }
    #endregion

    #region Methods

    /// <summary>
    /// Call get user preference and set the selected group. on Group Picker 
    /// </summary>
    private void SetSelectedGroup()
    {
        nbGroupsWarning.Visible = false;

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
    /// Call get user preference and set the  number of weeks
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
            //use user preference if different;
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
        this.ltContentPlaceholer.Text = string.Empty;
        if ( _selectedGroupIds.Length != 0 )
        {
            using ( var rockContext = new RockContext() )
            {
                int[] groupIds = _selectedGroupIds.AsIntegerList().ToArray();

                // limit groups to 40
                if ( groupIds.Count() > 40 )
                {
                    nbGroupsWarning.Text = "You have exceeded maximum number of groups. Please select less than 40 groups.";
                    nbGroupsWarning.Visible = true;
                    return;
                }

                // handle response when count exceeds 50000 which returns null from service
                var occurrences = new AttendanceOccurrenceService( rockContext ).GetOccurrencesGroupedByGroupsAndDateRange( groupIds, _dateRange );
                if ( occurrences == null )
                {
                    nbGroupsWarning.Text = "You have exceeded the maximum number of records. Please select fewer groups or reduce your date range.";
                    nbGroupsWarning.Visible = true;
                    return;
                }
                BuildDataSourceCells( occurrences );
            }
        }
        else
        {
            nbGroupsWarning.Text = "Please select at least one group.";
            nbGroupsWarning.Visible = true;
        }
    }

    /// <summary>
    /// Builds the data source cells.
    /// Pass in the Occurrences that are grouped by date
    /// fill list of data source Schedule Cells
    /// </summary>
    /// <param name="occurrences">The occurrences.</param>
    private void BuildDataSourceCells( IQueryable<IGrouping<DateTime, AttendanceOccurrence>> occurrences )
    {
        List<StatusCell> cells = new List<StatusCell>();
        var allDates = occurrences.Select( k => k.Key ).ToList();

        List<Attendance> allAttendances = occurrences.SelectMany( a => a.Select( x => x.Attendees ) ).SelectMany( s => s ).ToList();
        List<KeyValuePair<int, Attendance>> attendanceLookup = allAttendances.Select( k => new KeyValuePair<int, Attendance>( k.OccurrenceId, k ) ).ToList();

        var groupsInfos = occurrences.Select( a => new GroupInfo
        {
            DateTime = a.Key,
            Occurancedate = a.Key,
            Groups = a.Select( x => new GroupInfoDetail
            {
                OccurrenceId = x.Id,
                ScheduleId = x.ScheduleId,
                ScheduleName = x.Schedule.Name,
                GroupId = x.GroupId,
                GroupName = x.Group.Name,
                LocationName = x.Location.Name,
                LocationId = x.LocationId,
                Schedule = x.Schedule,
                CapcityDetail = x.Group.GroupLocations
                .Where( s => s.Schedules.Where( i => i.Id == x.ScheduleId ).Any() && s.GroupId == x.GroupId )
                .Select( cnf => cnf.GroupLocationScheduleConfigs
                .Select( r => new CapacityDetail
                {
                    ScheduleId = r.ScheduleId,
                    CapcityMinimum = r.MinimumCapacity,
                    CapacityDesired = r.DesiredCapacity,
                    CapityMaximum = r.MaximumCapacity
                } ) ).Select( r => r.ToList() ).ToList()

            } ).ToList()
        } ).ToList();

        foreach ( var date in allDates )
        {
            GroupInfo currentGroupInfo = groupsInfos.Where( k => k.DateTime == date ).FirstOrDefault();
            foreach ( var occurrenceGroup in currentGroupInfo.Groups )
            {
                var occurrenceId = occurrenceGroup.OccurrenceId;
                var cell = new StatusCell
                {
                    Guid = Guid.NewGuid(),
                    OccurrenceDate = currentGroupInfo.DateTime,
                    GroupId = occurrenceGroup.GroupId,
                    GroupName = occurrenceGroup.GroupName,
                    LocationId = occurrenceGroup.LocationId,
                    LocationName = occurrenceGroup.LocationName,
                    OccurrencePersons = BuildListOfOccurencePersons( attendanceLookup, occurrenceId ),
                    ScheduleId = occurrenceGroup.ScheduleId,
                    ScheduleName = occurrenceGroup.ScheduleName,
                    SortDateTime = occurrenceGroup.Schedule.GetNextStartDateTime( currentGroupInfo.DateTime ),
                    CapacityDetail = GetCapacityDetail( occurrenceGroup.ScheduleId, occurrenceGroup.CapcityDetail.Select( c => c ) )
                };

                cells.Add( cell );
            }
        }

        _occurrenceScheduleCells.AddRange( cells.OrderBy( ord => ord.SortDateTime ) );
    }

    /// <summary>
    /// Gets the capacity detail from IEnumerable<List<CapacityDetail>>
    /// that match on scheduleId
    /// </summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="enumerable">The enumerable.</param>
    /// <returns></returns>
    private CapacityDetail GetCapacityDetail( int? scheduleId, IEnumerable<List<CapacityDetail>> enumerable )
    {
        return enumerable.FirstOrDefault().Where( cap => cap.ScheduleId == scheduleId ).FirstOrDefault();
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
            if ( !returnList.Where( l => l.GroupId == statusColumn.GroupId && l.LocationId == statusColumn.LocationId ).Any() )
            {
                returnList.Add( statusColumn );
            }
        }

        var group = returnList.GroupBy( g => g.GroupId ).Select( g => g.FirstOrDefault() ).ToList();
        return group;
    }

    /// <summary>
    /// Builds the list of configurations by group locations.
    /// </summary>
    /// <param name="groupLocations">The group locations.</param>
    /// <returns></returns>
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
    /// Builds the list of occurrence persons from Attendances.
    /// </summary>
    /// <param name="attendances">The attendances.</param>
    /// <returns></returns>
    private List<OccurrencePerson> BuildListOfOccurencePersons( List<KeyValuePair<int, Attendance>> attendances, int occurrenceId )
    {
        var attendies = attendances.Where( k => k.Key == occurrenceId ).Select( a => a.Value ).ToList();

        List<OccurrencePerson> persons = new List<OccurrencePerson>();
        foreach ( var attendance in attendies )
        {
            persons.Add( new OccurrencePerson
            {
                NickName = attendance.PersonAlias.Person.NickName,
                LastName = attendance.PersonAlias.Person.LastName,
            } );
        }
        return persons;
    }

    #endregion

    #region Render Methods

    /// <summary>
    /// Gets the data and calls render results.
    /// </summary>
    private void GetDataAndRenderResults()
    {
        GetData();
        // if we have cells then render results 
        if ( _occurrenceScheduleCells.Count() > 0 )
        {
            RenderResults();
        }
        else
        {
            ltContentPlaceholer.Visible = true;
            ltContentPlaceholer.Text = "<i class='text-muted'>no data for the groups and dates selected</i>";
        }
    }

    /// <summary>
    /// Structure of Result:
    ///  <table class='table'>
    ///  <thead>
    ///  <tr>
    ///    <th scope = "col" ></ th >
    ///    <th scope="col">
    ///      <span class="date">Jul 8, 2018</span>
    ///      <span class="day-time">Sunday 8am</span>
    ///    </th>
    ///  </thead>
    ///  <tbody id = "groupid-###" >
    ///  <tr class="group-heading thead-dark">
    ///    <th colspan = "6" > Kids Volunteers</th>
    ///  </tr>
    ///  <tr class="location-row">
    ///    <th scope = "row" > Bears Room</th>
    ///    <td>
    ///      <ul class="location-list">
    ///        <li class="person attending">Ted Decker</li>
    ///      </ul>
    ///    </td>
    ///  </tr>
    /// </tbody>
    /// </table>
    /// </summary>
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
    /// <param name="sbTable">The table.</param>
    private void BuildHeader( List<Tuple<string, DateTime>> columnScheduleDates, StringBuilder sbTable )
    {
        // Head
        sbTable.AppendLine( "<table class='table'>" );
        sbTable.AppendLine( "<thead>" );
        sbTable.AppendLine( "<tr>" );
        sbTable.AppendLine( "<th scope='col'></th>" );

        int index = 0;

        foreach ( var column in _occurrenceScheduleCells )
        {
            if ( !columnScheduleDates.Where( tup => tup.Item2 == column.OccurrenceDate && tup.Item1 == column.ScheduleName ).Any() )
            {
                columnScheduleDates.Add( new Tuple<string, DateTime>( column.ScheduleName, ( DateTime ) column.OccurrenceDate ) );
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
             .Where( c => c.GroupId == cell.GroupId )
             .Select( res => new { res.LocationId, res.LocationName, res.GroupId, res.GroupName } ).Distinct();
        var distinctLocations = grouplocations.GroupBy( gl => gl.LocationId ).ToList();

        foreach ( var groupLocation in distinctLocations )
        {
            var currentLocation = groupLocation.Select( l => l ).FirstOrDefault();
            // build out row header of locations 
            sbBody.AppendLine( "<tr class='location-row'>" );
            sbBody.AppendLine( string.Format( "<th scope= 'row'>{0}</th>", currentLocation.LocationName ) );
            var cellsByLocation = _occurrenceScheduleCells.Where( cel => cel.LocationId == currentLocation.LocationId );

            // iterate through list of schedule names and associated date
            foreach ( var scheduleDate in columnScheduleDates )
            {
                // cell by schedule name and occurrence date
                var scheduleOccurrenceCell = cellsByLocation
                     .Where( cel => cel.OccurrenceDate == scheduleDate.Item2 && cel.ScheduleName == scheduleDate.Item1 )
                     .Select( cel => cel ).FirstOrDefault();
                if ( scheduleOccurrenceCell != null )
                {
                    // get the capacities that have been defined for this schedule and date
                    // if Desired has not been configured then use Minimum otherwise use 0 
                    var capacityDetail = scheduleOccurrenceCell.CapacityDetail;
                    var capacityRequested = capacityDetail == null ? 0 : capacityDetail.CapacityDesired != null ? capacityDetail.CapacityDesired : capacityDetail.CapcityMinimum;
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
                            switch ( attendance.RSVP )
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

                        // if more are still needed
                        if ( neededPersons > 0 )
                        {
                            sbBody.AppendLine( string.Format( "<li class='unassigned unassigned-meta'>{0} {1} Needed</li>", neededPersons, prepend ) );
                        }

                        sbBody.AppendLine( "</ul>" );
                        sbBody.AppendLine( "</td>" );
                    }
                    else
                    {
                        // no people scheduled handle capacity  
                        if ( capacityRequested > 0 )
                        {
                            sbBody.Append( "<td>" );
                            sbBody.AppendLine( "<ul class='location-list'>" );
                            sbBody.AppendLine( string.Format( "<li class='unassigned unassigned-meta'>{0} {1} Needed</li>", capacityRequested, "People" ) );
                            sbBody.AppendLine( "</ul>" );
                            sbBody.AppendLine( "</td>" );
                        }
                        else
                        {
                            // no occurrence exists
                            sbBody.AppendLine( "<td></td>" );
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Builds the group and location row header.
    /// </summary>
    /// <param name="sbBody">The table body of the table.</param>
    /// <param name="groupLocation">The group location.</param>
    private void BuildGroupAndLocationRowHeader( StringBuilder sbBody, StatusCell groupLocation )
    {
        int colspan = _occurrenceScheduleCells.Count + 1;
        sbBody.AppendLine( string.Format( "<tbody id='groupid-{0}'>", groupLocation.GroupId ) );
        sbBody.AppendLine( "<tr class='group-heading thead-dark'>" );
        sbBody.AppendLine( string.Format( "<th colspan='{0}'>{1}</th>", colspan, groupLocation.GroupName ) );
        sbBody.AppendLine( "</tr>" );
    }

    /// <summary>
    /// Updates the group and date range controls.
    /// and renders the results
    /// </summary>
    private void UpdateGroupAndDateRangeControls()
    {
        SetSelectedGroup();
        SetSelectedNumberOfWeeks();
        GetDataAndRenderResults();
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

    /// <summary>
    /// Handles the SaveClick event of the dlgGroups control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected void dlgGroups_SaveClick( object sender, EventArgs e )
    {

        dlgGroups.Hide();
        var groupsSelected = gpGroups.SelectedValues.ToList();
        var delimitedGroups = groupsSelected.AsDelimited( "," );
        if ( delimitedGroups != null && delimitedGroups != "0" )
        {
            this.SetUserPreference( UserPreferenceKeys.VolunteerScheduleStatusGroups, delimitedGroups.ToString() );
            nbGroupsWarning.Visible = false;
        }
        _numberOfWeeks = rsDateRange.SelectedValue;
        UpdateGroupAndDateRangeControls();
    }

    /// <summary>
    /// Handles the SaveClick event of the dlgDateRangeSlider control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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

    #endregion
}
