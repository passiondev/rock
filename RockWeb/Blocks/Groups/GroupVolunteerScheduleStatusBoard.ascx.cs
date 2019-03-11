using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using System.Web.UI.WebControls;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

[DisplayName( "Group Volunteer Schedule Status Board" )]
[Category( "Groups" )]
[Description( "Scheduler can see overview of current schedules by groups and dates." )]
[SlidingDateRangeField( "Date Range", "How many weeks into the future should bedisplayed.", false, "Next|6|Week||", enabledSlidingDateRangeTypes: "Next,Upcoming,Current", order: 0 )]
[GroupField( "Groups", "A parent group to start from when allowing someone to pick one or more groups to view.", false, order: 0 )]
public partial class GroupVolunteerScheduleStatusBoard : RockBlock
{
    #region Fields

    private List<StatusColumn> _dataSourceStatusColumns = new List<StatusColumn>();

    /// <summary>
    /// Status Column is used to bring together
    /// the information of the Columns and Row into a single object
    /// </summary>
    private struct StatusColumn
    {
        public Guid Id { get; set; }
        public DateTime StartDate { get; set; }
        public List<GroupLocation> GroupLocation { get; set; }
        public Group Group { get; set; }
        public Location Location { get; set; }
        public List<Attendance> Attendances { get; set; }
    }

    #endregion

    #region Base Control Methods

    /// <summary>
    /// Handles the Load event of the Page control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected void Page_Load( object sender, EventArgs e )
    {
        base.OnInit( e );

        GetDataRenderResults();
    }

    #endregion

    #region Properties
    #endregion

    #region Methods

    /// <summary>
    /// Gets the data and call render results.
    /// </summary>
    private void GetDataRenderResults()
    {
        var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( GetAttributeValue( "DateRange" ) ?? "-1||" );
        using ( var rockContext = new RockContext() )
        {
            var groups = new AttendanceService( rockContext ).GetAttendanceGroupedByDate( dateRange );

            BuildDataSourceColumns( groups );
            RenderResults();
        }
    }

    /// <summary>
    /// Source Data
    /// By default Status Columns date, group location information
    /// groups and location will be redundant 
    /// Selects the distinct group and locations.
    /// </summary>
    /// <returns></returns>
    private List<StatusColumn> SelectDistinctGroupAndLocations()
    {
        List<StatusColumn> returnList = new List<StatusColumn>();
        foreach ( var statusColumn in _dataSourceStatusColumns )
        {
            if ( !returnList.Where( l => l.Group.Id == statusColumn.Group.Id && l.Location.Id == statusColumn.Location.Id ).Any() )
            {
                returnList.Add( statusColumn );
            }
        }

        var group = returnList.GroupBy( g => g.Group.Id ).Select( g => g.FirstOrDefault() ).ToList();
        return group;
    }

    /// <summary>
    /// Source Data
    /// Builds the data source columns.
    /// </summary>
    /// <param name="groups">The groups.</param>
    private void BuildDataSourceColumns( IQueryable<IGrouping<DateTime, Attendance>> groups )
    {
        // get all groups by date range
        foreach ( var attendances in groups )
        {
            //sort all attendances by location 
            var currentAttendances = attendances.Select( a => a ).GroupBy( l => l.Occurrence.LocationId ).ToList();
            foreach ( var attendanceLocation in currentAttendances )
            {
                var currentAttendance = attendanceLocation.Select( a => a ).FirstOrDefault();
                var column = new StatusColumn
                {
                    Id = Guid.NewGuid(),
                    StartDate = currentAttendance.StartDateTime,
                    Group = currentAttendance.Occurrence.Group,
                    Location = currentAttendance.Occurrence.Location,
                    Attendances = attendances.Select( a => a ).ToList(),
                    GroupLocation = currentAttendance.Occurrence.Group.GroupLocations.ToList()
                };

                _dataSourceStatusColumns.Add( column );
            }
        }
    }

    /// <summary>
    /// Builds the unique locations.
    /// </summary>
    /// <param name="groupLocation">The group location.</param>
    /// <returns></returns>
    private List<GroupLocation> BuildUniqueLocations( StatusColumn groupLocation )
    {
        List<GroupLocation> results = new List<GroupLocation>();
        foreach ( var location in groupLocation.GroupLocation )
        {
            if ( !results.Where( l => l.Id == location.Id ).Any() )
            {
                results.Add( location );
            }
        }
        return results;
    }

    /// <summary>
    /// Renders the results.
    /// Transforms the Source Data to HTML 
    /// </summary>
    private void RenderResults()
    {
        List<DateTime> columnDateTimes = new List<DateTime>();
        StringBuilder sbHead = new StringBuilder();
        StringBuilder sbBody = new StringBuilder();

        BuildHeader(columnDateTimes,sbHead);

        var distingGroupsAndLocations = SelectDistinctGroupAndLocations();

        foreach ( var groupLocation in distingGroupsAndLocations )
        {
            List<GroupLocation> uniqueLocation = BuildUniqueLocations( groupLocation );

            BuildGroupAndLocationRowHeader(sbBody,groupLocation);
            BuildBodyColumnsAndRows( sbBody,columnDateTimes, uniqueLocation );

        }

        sbHead.AppendLine( "</tr>" );
        sbHead.AppendLine( "</thead>" );
        sbHead.AppendLine( sbBody.ToString() );
        sbHead.AppendLine( "</table>" );

        this.ltContentPlaceholer.Text = sbHead.ToString();
    }

    /// <summary>
    /// Builds the header.
    /// </summary>
    /// <param name="columnDateTimes">The column date times.</param>
    /// <param name="sbHead">The sb head.</param>
    private void BuildHeader( List<DateTime> columnDateTimes, StringBuilder sbHead )
    {
        // Head
        sbHead.AppendLine( "<table class='table'>" );
        sbHead.AppendLine( "<thead>" );
        sbHead.AppendLine( "<tr>" );
        sbHead.AppendLine( " <th scope='col'></th>" );
        int index = 0;

        foreach ( var column in _dataSourceStatusColumns )
        {
            if ( !columnDateTimes.Contains( column.StartDate ) )
            {
                columnDateTimes.Add( column.StartDate );
            }

        }
        foreach ( var date in columnDateTimes )
        {
            sbHead.AppendLine( "<th scope='col'>" );
            sbHead.AppendLine( string.Format( "<span class='date'>{0}</span>", date.ToString( "MMMM dd, yyyy" ) ) );
            sbHead.AppendLine( "</br>" );
            sbHead.AppendLine( string.Format( "<span class='day-time'>{0} {1}</span>", date.ToString( "dddd" ), date.ToShortTimeString() ) );
            sbHead.AppendLine( "</th>" );

            index++;
        }
    }

    /// <summary>
    /// Builds the body columns and rows.
    /// </summary>
    /// <param name="sbBody">The sb body.</param>
    /// <param name="columnDateTimes">The column date times.</param>
    /// <param name="uniqueLocation">The unique location.</param>
    private void BuildBodyColumnsAndRows( StringBuilder sbBody, List<DateTime> columnDateTimes, List<GroupLocation> uniqueLocation )
    {
        foreach ( var item in uniqueLocation )
        {
            sbBody.AppendLine( "<tr class='location-row'>" );
            sbBody.AppendLine( string.Format( "<th scope= 'row'>{0}</th>", item.Location.Name ) );

            var statusColumnsByLocation = _dataSourceStatusColumns.Where( x => x.Group.Id == item.GroupId && x.Location.Id == item.Location.Id );
            foreach ( var dateColumn in columnDateTimes )
            {
                var attendances = statusColumnsByLocation.Where( sc => sc.StartDate == dateColumn && sc.Location.Id == item.LocationId ).Select( sc => sc.Attendances ).FirstOrDefault();
                if ( attendances != null )
                {
                    // filter attendace by location since they are added by group
                    var attendanceAtLocation = attendances.Where( a => a.Occurrence.Location.Id == item.Location.Id );
                    sbBody.Append( "<td>" );
                    sbBody.AppendLine( "<ul class='location-list'>" );

                    foreach ( var attendance in attendanceAtLocation )
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

                        sbBody.AppendLine( string.Format( "<li class='{0}'>{1} {2}</li>", status, attendance.PersonAlias.Person.FirstName, attendance.PersonAlias.Person.LastName ) );
                    }

                    sbBody.AppendLine( "</ul>" );
                    sbBody.AppendLine( "</td>" );
                }
                else
                {
                    sbBody.AppendLine( "<td></td>" );
                }
            }
            sbBody.AppendLine( "</tr>" );
        }

        sbBody.AppendLine( "</tbody>" );
    }

    /// <summary>
    /// Builds the group and location row header.
    /// </summary>
    /// <param name="sbBody">The sb body.</param>
    /// <param name="groupLocation">The group location.</param>
    private void BuildGroupAndLocationRowHeader( StringBuilder sbBody, StatusColumn groupLocation )
    {
        int colspan = _dataSourceStatusColumns.Count + 1;

        sbBody.AppendLine( string.Format( "<tbody id='groupid-{0}'>", groupLocation.Group.Id ) );
        sbBody.AppendLine( "<tr class='group-heading thead-dark'>" );
        sbBody.AppendLine( string.Format( "<th colspan='{0}'>{1}</th>", colspan, groupLocation.Group.Name ) );
        sbBody.AppendLine( "</tr>" );
        ;
    }

    #endregion

    #region Events

    protected void btnGroups_Click( object sender, EventArgs e )
    {
    }

    protected void btnDates_Click( object sender, EventArgs e )
    {
    }

    #endregion


}