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
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using DDay.iCal;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Groups
{
    /// <summary>
    ///
    /// </summary>
    [DisplayName( "Group Schedule Toolbox" )]
    [Category( "Groups" )]
    [Description( "Allows management of group scheduling for a specific person (worker)." )]

    [ContextAware( typeof( Rock.Model.Person ) )]

    [IntegerField(
        "Number of Future Weeks To Show",
        Description = "The number of weeks into the future to allow users to signup for a schedule.",
        IsRequired = true,
        DefaultValue = "6",
        Order = 0,
        Key = AttributeKeys.FutureWeeksToShow )]

    public partial class GroupScheduleToolbox : RockBlock
    {
        protected class AttributeKeys
        {
            public const string FutureWeeksToShow = "FutureWeeksToShow";
        }

        protected const string ALL_GROUPS_STRING = "All Groups";

        List<PersonScheduleSignup> availableGroupLocationSchedules;

        /// <summary>
        /// Tab menu options
        /// </summary>
        public enum GroupScheduleToolboxTab
        {
            /// <summary>
            /// My Schedule tab
            /// </summary>
            MySchedule = 0,

            /// <summary>
            /// Preferences tab
            /// </summary>
            Preferences = 1,

            /// <summary>
            /// Sign-up tab
            /// </summary>
            SignUp = 2
        }

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gBlackoutDates.GridRebind += gBlackoutDates_GridRebind;
            gBlackoutDates.Actions.AddClick += gBlackoutDates_AddClick;
            gBlackoutDates.IsDeleteEnabled = true;
            gBlackoutDates.AllowPaging = false;
            gBlackoutDates.AllowSorting = false;
            gBlackoutDates.Actions.ShowAdd = true;
            gBlackoutDates.Actions.ShowExcelExport = false;
            gBlackoutDates.Actions.ShowMergeTemplate = false;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            // Setup for being able to copy text to clipboard
            RockPage.AddScriptLink( this.Page, "~/Scripts/clipboard.js/clipboard.min.js" );

            string script = string.Format( @"
    new ClipboardJS('#{0}');
    $('#{0}').tooltip();
", btnCopyToClipboard.ClientID );

            ScriptManager.RegisterStartupScript( btnCopyToClipboard, btnCopyToClipboard.GetType(), "share-copy", script, true );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            List<string> errors = new List<string>();

            string postbackArgs = Request.Params["__EVENTARGUMENT"];
            if ( !string.IsNullOrWhiteSpace( postbackArgs ) )
            {
                string previousAssetSelected = string.Empty;

                string[] occurrences = postbackArgs.Split( new char[] { '|' } );
                foreach ( string occurrence in occurrences )
                {
                    int? groupId = null;
                    int? locationId = null;
                    int? scheduleId = null;
                    DateTime? date = null;

                    try
                    {
                        string[] props = occurrence.Split( new char[] { ',' } );
                        groupId = props[0].AsIntegerOrNull();
                        locationId = props[1].AsIntegerOrNull();
                        scheduleId = props[2].AsIntegerOrNull();
                        date = props[3].AsDateTime();
                        AttendanceOccurrence attendanceOccurrence = null;

                        using ( var rockContext = new RockContext() )
                        {
                            var attendanceOccurrenceService = new AttendanceOccurrenceService( rockContext );

                            attendanceOccurrence = attendanceOccurrenceService.Get( date.Value.Date, groupId, locationId, scheduleId );

                            // Create the occurrence if needed
                            if ( attendanceOccurrence == null )
                            {
                                attendanceOccurrence = attendanceOccurrenceService.CreateMissingAttendanceOccurrences( date.Value, scheduleId.Value, locationId.Value, groupId.Value ).FirstOrDefault();
                                attendanceOccurrenceService.Add( attendanceOccurrence );
                                rockContext.SaveChanges();
                            }
                        }

                        using ( var rockContext = new RockContext() )
                        {

                            var attendanceService = new AttendanceService( rockContext );
                            var attendance = attendanceService.ScheduledPersonAssign( CurrentPerson.Id, attendanceOccurrence.Id, CurrentPersonAlias );
                            rockContext.SaveChanges();

                            attendanceService.ScheduledPersonConfirm( attendance.Id );
                            rockContext.SaveChanges();
                        }
                    }
                    catch ( Exception ex )
                    {
                        // If there is a problem then log it and move on to the next schedule
                        errors.Add( string.Format( "There was a problem signing up for one or more schedules." ) );
                        ExceptionLogService.LogException( ex );
                        continue;
                    }
                }

                // After the save is complete rebuild the signup controls
                CreateSignupControls();
            }

            if ( !Page.IsPostBack )
            {
                LoadDropDowns();
                ShowDetails();
            }
        }

        #endregion Base Control Methods

        #region Block Events and Methods

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the bgTabs control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bgTabs_SelectedIndexChanged( object sender, EventArgs e )
        {
            ShowSelectedTab();
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetails()
        {
            ShowSelectedTab();

            // My Schedule
            BindPendingConfirmations();
            BindUpcomingSchedules();

            // Preferences
            BindBlackoutDates();
            BindGroupPreferencesRepeater();

            // Signup
            CreateSignupControls();
        }

        /// <summary>
        /// Shows selected person details.
        /// </summary>
        private void ShowPersonDetails()
        {
            // My Schedule
            BindPendingConfirmations();
            BindUpcomingSchedules();

            // Preferences
            BindBlackoutDates();
            BindGroupPreferencesRepeater();

            // Signup
            CreateSignupControls();
        }


        /// <summary>
        /// Shows the selected tab.
        /// </summary>
        private void ShowSelectedTab()
        {
            var selectedTab = bgTabs.SelectedValueAsEnum<GroupScheduleToolboxTab>();
            pnlMySchedule.Visible = selectedTab == GroupScheduleToolboxTab.MySchedule;
            pnlPreferences.Visible = selectedTab == GroupScheduleToolboxTab.Preferences;
            pnlSignup.Visible = selectedTab == GroupScheduleToolboxTab.SignUp;

            if ( selectedTab == GroupScheduleToolboxTab.SignUp )
            {
                CreateSignupControls();
            }
        }

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns()
        {
            bgTabs.Items.Clear();
            bgTabs.Items.Add( new ListItem( "My Schedule", GroupScheduleToolboxTab.MySchedule.ConvertToInt().ToString() ) { Selected = true } );
            bgTabs.Items.Add( new ListItem( "Preferences", GroupScheduleToolboxTab.Preferences.ConvertToInt().ToString() ) );
            bgTabs.Items.Add( new ListItem( "Sign-up", GroupScheduleToolboxTab.SignUp.ConvertToInt().ToString() ) );
        }

        #endregion Block Events and Methods

        #region My Schedule Tab

        /// <summary>
        /// Gets the occurrence details (Date, Group Name, Location)
        /// </summary>
        /// <param name="attendance">The attendance.</param>
        /// <returns></returns>
        protected string GetOccurrenceDetails( Attendance attendance )
        {
            return string.Format( "{0} - {1} - {2}", attendance.Occurrence.OccurrenceDate.ToShortDateString(), attendance.Occurrence.Group.Name, attendance.Occurrence.Location );
        }

        /// <summary>
        /// Gets the occurrence time.
        /// </summary>
        /// <param name="attendance">The attendance.</param>
        /// <returns></returns>
        protected string GetOccurrenceTime( Attendance attendance )
        {
            return attendance.Occurrence.Schedule.GetCalenderEvent().DTStart.Value.TimeOfDay.ToTimeString();
        }

         /// <summary>
        /// Handles the ItemDataBound event of the rptUpcomingSchedules control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptUpcomingSchedules_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var lConfirmedOccurrenceDetails = e.Item.FindControl( "lConfirmedOccurrenceDetails" ) as Literal;
            var lConfirmedOccurrenceTime = e.Item.FindControl( "lConfirmedOccurrenceTime" ) as Literal;
            var btnCancelConfirmAttending = e.Item.FindControl( "btnCancelConfirmAttending" ) as LinkButton;
            var attendance = e.Item.DataItem as Attendance;

            lConfirmedOccurrenceDetails.Text = GetOccurrenceDetails( attendance );
            lConfirmedOccurrenceTime.Text = GetOccurrenceTime( attendance );

            btnCancelConfirmAttending.CommandName = "AttendanceId";
            btnCancelConfirmAttending.CommandArgument = attendance.Id.ToString();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptPendingConfirmations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptPendingConfirmations_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var lPendingOccurrenceDetails = e.Item.FindControl( "lPendingOccurrenceDetails" ) as Literal;
            var lPendingOccurrenceTime = e.Item.FindControl( "lPendingOccurrenceTime" ) as Literal;
            var btnConfirmAttending = e.Item.FindControl( "btnConfirmAttending" ) as LinkButton;
            var btnDeclineAttending = e.Item.FindControl( "btnDeclineAttending" ) as LinkButton;
            var attendance = e.Item.DataItem as Attendance;

            lPendingOccurrenceDetails.Text = GetOccurrenceDetails( attendance );
            lPendingOccurrenceTime.Text = GetOccurrenceTime( attendance );
            btnConfirmAttending.CommandName = "AttendanceId";
            btnConfirmAttending.CommandArgument = attendance.Id.ToString();

            btnDeclineAttending.CommandName = "AttendanceId";
            btnDeclineAttending.CommandArgument = attendance.Id.ToString();
        }

        /// <summary>
        /// Handles the Click event of the btnCancelConfirmAttending control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancelConfirmAttending_Click( object sender, EventArgs e )
        {
            var btnCancelConfirmAttending = sender as LinkButton;
            int? attendanceId = btnCancelConfirmAttending.CommandArgument.AsIntegerOrNull();
            if ( attendanceId.HasValue )
            {
                var rockContext = new RockContext();
                new AttendanceService( rockContext ).ScheduledPersonConfirmCancel( attendanceId.Value );
                rockContext.SaveChanges();
            }

            ShowPersonDetails();
        }

        /// <summary>
        /// Handles the Click event of the btnConfirmAttending control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfirmAttending_Click( object sender, EventArgs e )
        {
            var btnConfirmAttending = sender as LinkButton;
            int? attendanceId = btnConfirmAttending.CommandArgument.AsIntegerOrNull();
            if ( attendanceId.HasValue )
            {
                var rockContext = new RockContext();
                new AttendanceService( rockContext ).ScheduledPersonConfirm( attendanceId.Value );
                rockContext.SaveChanges();
            }

            ShowPersonDetails();
        }

        /// <summary>
        /// Handles the Click event of the btnDeclineAttending control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDeclineAttending_Click( object sender, EventArgs e )
        {
            var btnDeclineAttending = sender as LinkButton;
            int? attendanceId = btnDeclineAttending.CommandArgument.AsIntegerOrNull();
            if ( attendanceId.HasValue )
            {
                var rockContext = new RockContext();

                // TODO
                int? declineReasonValueId = null;

                new AttendanceService( rockContext ).ScheduledPersonDecline( attendanceId.Value, declineReasonValueId );
                rockContext.SaveChanges();
            }

            ShowPersonDetails();
        }

        /// <summary>
        /// Binds the Upcoming Schedules grid.
        /// </summary>
        private void BindUpcomingSchedules()
        {
            var currentDateTime = RockDateTime.Now;
            var rockContext = new RockContext();

            var qryPendingConfirmations = new AttendanceService( rockContext ).GetConfirmedScheduled()
                .Where( a => a.PersonAlias.PersonId == CurrentPerson.Id )
                .Where( a => a.Occurrence.OccurrenceDate >= currentDateTime )
                .OrderBy( a => a.Occurrence.OccurrenceDate );

            rptUpcomingSchedules.DataSource = qryPendingConfirmations.ToList();
            rptUpcomingSchedules.DataBind();

            var personAliasService = new PersonAliasService( rockContext );
            var paguid = CurrentPersonAlias.Guid;

            // Set URL in feed button
            var globalAttributes = Rock.Web.Cache.GlobalAttributesCache.Get();
            btnCopyToClipboard.Attributes["data-clipboard-text"] = string.Format(
                "{0}GetPersonGroupScheduleFeed.ashx?paguid={1}",
                globalAttributes.GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash(),
                CurrentPersonAlias.Guid );
            btnCopyToClipboard.Disabled = false;
        }

        /// <summary>
        /// Binds the Pending Confirmations grid.
        /// </summary>
        private void BindPendingConfirmations()
        {
            lPendingConfirmations.Visible = false;

            using ( var rockContext = new RockContext() )
            {
                var pendingConfirmations = new AttendanceService( rockContext ).GetPendingScheduledConfirmations()
                    .Where( a => a.PersonAlias.PersonId == CurrentPerson.Id )
                    .OrderBy( a => a.Occurrence.OccurrenceDate )
                    .ToList();

                if ( pendingConfirmations.Any() )
                {
                    lPendingConfirmations.Visible = true;
                    rptPendingConfirmations.DataSource = pendingConfirmations;
                    rptPendingConfirmations.DataBind();
                }
            }
        }


        #endregion My Schedule Tab

        #region Preferences Tab

        /// <summary>
        /// Binds the group preferences repeater with a list of groups where the GroupType has scheduling enabled.
        /// </summary>
        protected void BindGroupPreferencesRepeater()
        {
            using ( var rockContext = new RockContext() )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groups = groupMemberService
                    .Queryable()
                    .AsNoTracking()
                    .Where( x => x.PersonId == CurrentPerson.Id )
                    .Where( x => x.Group.GroupType.IsSchedulingEnabled == true )
                    .Select( x => x.Group )
                    .OrderBy( x => x.Name )
                    .Distinct() // if they are in the group twice with different roles we only want one record.
                    .ToList();

                rptGroupPreferences.DataSource = groups;
                rptGroupPreferences.DataBind();
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlSendRemindersDaysOffset control.
        /// Saves the ScheduleReminderEmailOffsetDays for each GroupMember that matches the Group/Person.
        /// In most cases that will be one GroupMember unless the person has multiple roles in the group
        /// (e.g. Leader and Member).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlSendRemindersDaysOffset_SelectedIndexChanged( object sender, EventArgs e )
        {
            var repeaterItem = ( ( DropDownList ) sender ).BindingContainer as RepeaterItem;
            var hfGroupId = repeaterItem.FindControl( "hfPreferencesGroupId" ) as HiddenField;
            var groupId = hfGroupId.ValueAsInt();

            int? days = ( ( DropDownList ) sender ).SelectedValueAsInt( true );

            using ( var rockContext = new RockContext() )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groupMembers = groupMemberService
                    .Queryable()
                    .Where( x => x.GroupId == groupId )
                    .Where( x => x.PersonId == CurrentPerson.Id )
                    .ToList();

                // in most cases the will be only one unless the person has multiple roles in the group (e.g. leader and member)
                foreach( var groupMember in groupMembers )
                {
                    groupMember.ScheduleReminderEmailOffsetDays = days;
                }

                rockContext.SaveChanges();
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlGroupMemberScheduleTemplate control.
        /// Saves the ScheduleTemplateId for each GroupMember that matches the Group/Person.
        /// In most cases taht will be one GroupMember unless the person has multiple roles in the group
        /// (e.g. Leader and Member)
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlGroupMemberScheduleTemplate_SelectedIndexChanged( object sender, EventArgs e )
        {
            // Save the preference. For now this acts as a note to the scheduler and does not effect the list of assignments presented to the user.
            var repeaterItem = ( ( DropDownList ) sender ).BindingContainer as RepeaterItem;
            var hfGroupId = repeaterItem.FindControl( "hfPreferencesGroupId" ) as HiddenField;
            var groupId = hfGroupId.ValueAsInt();

            using ( var rockContext = new RockContext() )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groupMembers = groupMemberService.GetByGroupIdAndPersonId( groupId, CurrentPerson.Id ).ToList();

                // In most cases there will be only one unless the person has multiple roles in the group (e.g. Leader and Member)
                foreach ( var groupMember in groupMembers )
                {
                    groupMember.ScheduleTemplateId = ( ( DropDownList ) sender ).SelectedValueAsInt( true );
                }

                rockContext.SaveChanges();
            }
        }

        protected void rptGroupPreferences_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var group = ( Group ) e.Item.DataItem;
            if (group == null )
            {
                return;
            }

            var lGroupPreferencesGroupName = ( Literal ) e.Item.FindControl( "lGroupPreferencesGroupName" );
            var hfPreferencesGroupId = ( HiddenField ) e.Item.FindControl( "hfPreferencesGroupId" );
            var rptGroupPreferenceAssignments = ( Repeater ) e.Item.FindControl( "rptGroupPreferenceAssignments" );

            hfPreferencesGroupId.Value = group.Id.ToString();

            rptGroupPreferencesBindDropDowns( group, e );

            // bind repeater rptGroupPreferenceAssignments
            using ( var rockContext = new RockContext() )
            {
                var groupLocationService = new GroupLocationService( rockContext );
                var schedules = groupLocationService
                    .Queryable()
                    .AsNoTracking()
                    .Where( g => g.GroupId == group.Id )
                    .SelectMany( g => g.Schedules )
                    .Distinct()
                    .ToList();

                // The time is locked up in the iCal string in the schedule. So use the list of schedules, get the times from them, and create a new list
                // of ScheduleTime so we can sort by time.
                var scheduleTime = schedules
                    .Select( s => new ScheduleTime ( s ) )
                    .OrderBy( st => st.Time );

                rptGroupPreferenceAssignments.DataSource = scheduleTime;
                rptGroupPreferenceAssignments.DataBind();

            }

        }

        protected void rptGroupPreferenceAssignments_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var scheduleTime = ( ScheduleTime ) e.Item.DataItem;
            if ( scheduleTime == null )
            {
                return;
            }

            var hfScheduleId = ( HiddenField ) e.Item.FindControl( "hfScheduleId" );
            hfScheduleId.Value = scheduleTime.ScheduleForTime.Id.ToString();

            var cbGroupPreferenceAssignmentScheduleTime = ( CheckBox ) e.Item.FindControl( "cbGroupPreferenceAssignmentScheduleTime" );

            var repeaterItemGroup = ( ( Repeater ) sender ).BindingContainer as RepeaterItem;
            var hfPreferencesGroupId = ( HiddenField ) repeaterItemGroup.FindControl( "hfPreferencesGroupId" );
            
            var rockContext = new RockContext();

            // TODO: If the person has multiple roles in the Group the same settings will be saved for each of those group members so we only need to get the first one
            int groupMemberId = new GroupMemberService( rockContext )
                .GetByGroupIdAndPersonId( hfPreferencesGroupId.ValueAsInt(), CurrentPerson.Id )
                .AsNoTracking()
                .Select( gm => gm.Id )
                .FirstOrDefault();

            var groupMemberAssignmentService = new GroupMemberAssignmentService( rockContext );
            var groupmemberAssignment = groupMemberAssignmentService
                .Queryable()
                .AsNoTracking()
                .Where( x => x.GroupMemberId == groupMemberId )
                .Where( x => x.ScheduleId == scheduleTime.ScheduleForTime.Id )
                .FirstOrDefault();

            cbGroupPreferenceAssignmentScheduleTime.Text = scheduleTime.Time.ToShortTimeString();
            cbGroupPreferenceAssignmentScheduleTime.ToolTip = scheduleTime.ScheduleForTime.Name;
            cbGroupPreferenceAssignmentScheduleTime.Checked = groupmemberAssignment != null;

            var ddlGroupPreferenceAssignmentLocation = ( DropDownList ) e.Item.FindControl( "ddlGroupPreferenceAssignmentLocation" );
            var locations = new LocationService( rockContext ).GetByGroupSchedule( scheduleTime.ScheduleForTime.Id, hfPreferencesGroupId.ValueAsInt() ).ToList();
            ddlGroupPreferenceAssignmentLocation.DataSource = locations;
            ddlGroupPreferenceAssignmentLocation.DataValueField = "Id";
            ddlGroupPreferenceAssignmentLocation.DataTextField = "Name";
            ddlGroupPreferenceAssignmentLocation.DataBind();
            ddlGroupPreferenceAssignmentLocation.Items.Insert( 0, new ListItem( string.Empty, string.Empty) );
            ddlGroupPreferenceAssignmentLocation.Enabled = groupmemberAssignment != null;

            if ( groupmemberAssignment != null )
            {
                ddlGroupPreferenceAssignmentLocation.SelectedValue = groupmemberAssignment.LocationId.ToStringSafe();
                ddlGroupPreferenceAssignmentLocation.Items[0].Text = "No Preference";
            }
        }

        protected void cbGroupPreferenceAssignmentScheduleTime_CheckedChanged( object sender, EventArgs e )
        {
            var scheduleCheckBox = ( CheckBox ) sender;
            var repeaterItemSchedule = scheduleCheckBox.BindingContainer as RepeaterItem;
            var ddlGroupPreferenceAssignmentLocation = ( DropDownList ) repeaterItemSchedule.FindControl( "ddlGroupPreferenceAssignmentLocation" );
            var hfScheduleId = ( HiddenField ) repeaterItemSchedule.FindControl( "hfScheduleId" );

            ddlGroupPreferenceAssignmentLocation.Enabled = scheduleCheckBox.Checked;
            ddlGroupPreferenceAssignmentLocation.Items[0].Text = scheduleCheckBox.Checked ? "No Preference" : string.Empty;

            var repeaterItemGroup = repeaterItemSchedule.Parent.Parent as RepeaterItem;
            var hfPreferencesGroupId = ( HiddenField ) repeaterItemGroup.FindControl( "hfPreferencesGroupId" );

            using ( var rockContext = new RockContext() )
            {
                List<int> groupMemberIds = new GroupMemberService( rockContext )
                    .GetByGroupIdAndPersonId( hfPreferencesGroupId.ValueAsInt(), CurrentPerson.Id )
                    .AsNoTracking()
                    .Select( gm => gm.Id )
                    .ToList();

                var groupMemberAssignmentService = new GroupMemberAssignmentService( rockContext );

                foreach ( var groupMemberId in groupMemberIds )
                {
                    if ( scheduleCheckBox.Checked )
                    {
                        groupMemberAssignmentService.AddOrUpdate( groupMemberId, hfScheduleId.ValueAsInt() );
                    }
                    else
                    {
                        groupMemberAssignmentService.DeleteByGroupMemberAndSchedule( groupMemberId, hfScheduleId.ValueAsInt() );
                    }

                    rockContext.SaveChanges();
                }
            }
        }

        
        protected void ddlGroupPreferenceAssignmentLocation_SelectedIndexChanged( object sender, EventArgs e )
        {
            var locationDropDownList = ( DropDownList ) sender;
            var repeaterItemSchedule = locationDropDownList.BindingContainer as RepeaterItem;
            var hfScheduleId = ( HiddenField ) repeaterItemSchedule.FindControl( "hfScheduleId" );

            var repeaterItemGroup = repeaterItemSchedule.Parent.Parent as RepeaterItem;
            var hfPreferencesGroupId = ( HiddenField ) repeaterItemGroup.FindControl( "hfPreferencesGroupId" );

            using ( var rockContext = new RockContext() )
            {
                List<int> groupMemberIds = new GroupMemberService( rockContext )
                    .GetByGroupIdAndPersonId( hfPreferencesGroupId.ValueAsInt(), CurrentPerson.Id )
                    .AsNoTracking()
                    .Select( gm => gm.Id )
                    .ToList();

                var groupMemberAssignmentService = new GroupMemberAssignmentService( rockContext );

                foreach ( var groupMemberId in groupMemberIds )
                {
                    groupMemberAssignmentService.AddOrUpdate( groupMemberId, hfScheduleId.ValueAsInt(), locationDropDownList.SelectedValueAsInt() );
                }

                rockContext.SaveChanges();
            }
        }

        /// <summary>
        /// Populates the DropDownLists ddlGroupMemberScheduleTemplate and ddlSendRemindersDaysOffset and
        /// sets the value for the current Person/Group
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptGroupPreferencesBindDropDowns( Group group, RepeaterItemEventArgs e )
        {
            var ddlGroupMemberScheduleTemplate = e.Item.FindControl( "ddlGroupMemberScheduleTemplate" ) as RockDropDownList;
            var ddlSendRemindersDaysOffset = e.Item.FindControl( "ddlSendRemindersDaysOffset" ) as RockDropDownList;

            using ( var rockContext = new RockContext() )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groupMember = groupMemberService.GetByGroupIdAndPersonId( group.Id, CurrentPerson.Id ).FirstOrDefault();

                // The items for this are hard coded in the markup, so just set the selected value.
                ddlSendRemindersDaysOffset.SelectedValue = groupMember.ScheduleReminderEmailOffsetDays == null ? string.Empty : groupMember.ScheduleReminderEmailOffsetDays.ToString();

                // Templates for all and this group type.
                var groupMemberScheduleTemplateService = new GroupMemberScheduleTemplateService( rockContext );
                var groupMemberScheduleTemplates = groupMemberScheduleTemplateService
                    .Queryable()
                    .AsNoTracking()
                    .Where( x => x.GroupTypeId == null || x.GroupTypeId == group.GroupTypeId )
                    .Select( x => new { Value = (int?)x.Id, Text = x.Name } )
                    .ToList();

                groupMemberScheduleTemplates.Insert( 0, new { Value = (int?)null, Text = "No Schedule" } );

                ddlGroupMemberScheduleTemplate.DataSource = groupMemberScheduleTemplates;
                ddlGroupMemberScheduleTemplate.DataValueField = "Value";
                ddlGroupMemberScheduleTemplate.DataTextField = "Text";
                ddlGroupMemberScheduleTemplate.DataBind();
                ddlGroupMemberScheduleTemplate.SelectedValue = groupMember.ScheduleTemplateId == null ? string.Empty : groupMember.ScheduleTemplateId.ToString();
            }
        }

        private class ScheduleTime
        {
            public ScheduleTime( Schedule schedule )
            {
                ScheduleForTime = schedule;
                var OccurrenceDateTime = schedule.GetOccurrences( DateTime.Now.AddDays( -1 ), DateTime.Now.AddDays( 365 ) ).FirstOrDefault().Period.StartTime.Value;

                // We need to sort by time, so set the dates all the same and just use the time.
                _time = DateTime.MinValue.Add( OccurrenceDateTime.TimeOfDay );


            }
            /// <summary>
            /// Gets the time for the Schedule. The date part is the min date, so this is just for the time.
            /// </summary>
            /// <value>
            /// The time.
            /// </value>
            public DateTime Time {
                get
                {
                    
                    return _time;
                }
            }

            private DateTime _time;

            /// <summary>
            /// Gets or sets the schedule.
            /// </summary>
            /// <value>
            /// The schedule for time.
            /// </value>
            public Schedule ScheduleForTime { get; set; }
        }

        #region Preferences Tab Blackout
        protected void BindBlackoutDates()
        {
            gBlackoutDates_BindGrid();
        }

        protected void gBlackoutDates_AddClick( object sender, EventArgs e )
        {
            ShowDialog( "mdAddBlackoutDates" );
        }

        protected void gBlackoutDatesDelete_Click( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var personScheduleExclusionService = new PersonScheduleExclusionService( rockContext );
                var personScheduleExclusion = personScheduleExclusionService.Get( e.RowKeyId );
                if ( personScheduleExclusion != null )
                {
                    personScheduleExclusionService.Delete( personScheduleExclusion );
                    rockContext.SaveChanges();
                    BindBlackoutDates();
                }
            }
        }

        protected void gBlackoutDates_GridRebind( object sender, GridRebindEventArgs e )
        {
            gBlackoutDates_BindGrid();
        }

        protected void gBlackoutDates_BindGrid()
        {
            var currentDate = DateTime.Now.Date;

            using ( var rockContext = new RockContext() )
            {
                List<int> familyMemberAliasIds = new PersonService( rockContext )
                    .GetFamilyMembers( CurrentPerson.Id, true )
                    .Select( m => m.Person.Aliases.FirstOrDefault( a => a.PersonId == m.PersonId ) )
                    .Select( a => a.Id )
                    .ToList();

                var personScheduleExclusionService = new PersonScheduleExclusionService( rockContext );
                var personScheduleExclusions = personScheduleExclusionService
                    .Queryable()
                    .AsNoTracking()
                    .Where( e => familyMemberAliasIds.Contains( e.PersonAliasId.Value ) )
                    .Where( e => e.StartDate >= currentDate || e.EndDate >= currentDate )
                    .OrderBy( e => e.StartDate )
                    .ThenBy( e => e.EndDate )
                    .Select( e => new BlackoutDate
                     {
                         ExclusionId = e.Id,
                         PersonAliasId = e.PersonAliasId.Value,
                         StartDate = DbFunctions.TruncateTime( e.StartDate ).Value,
                         EndDate = DbFunctions.TruncateTime( e.EndDate ).Value,
                         FullName = e.PersonAlias.Person.NickName + " " + e.PersonAlias.Person.LastName,
                         GroupName = e.Group.Name
                     } );

                gBlackoutDates.DataSource = personScheduleExclusions.ToList();
                gBlackoutDates.DataBind();
            }
        }

        /// <summary>
        /// POCO to hold blackout dates for the grid.
        /// </summary>
        private class BlackoutDate
        {
            public int ExclusionId { get; set; }

            public int PersonAliasId { get; set; }

            public DateTime StartDate { get; set; }

            public DateTime EndDate { get; set; }

            public string DateRange
            {
                get
                {
                    return StartDate.ToString( "M/d/yyyy" ) + " - " + EndDate.ToString( "M/d/yyyy" );
                }
            }

            public string FullName { get; set; }

            public string GroupName
            {
                get
                {
                    return _groupName.IsNullOrWhiteSpace() ? ALL_GROUPS_STRING : _groupName;
                }
                set
                {
                    _groupName = value;
                }
            }

            private string _groupName;

        }

         #region Blackout Dates Modal

        private void mdAddBlackoutDates_ddlBlackoutGroups_Bind()
        {
            using ( var rockContext = new RockContext() )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groups = groupMemberService
                    .Queryable()
                    .AsNoTracking()
                    .Where( g => g.PersonId == CurrentPerson.Id )
                    .Where( g => g.Group.GroupType.IsSchedulingEnabled == true )
                    .Select( g => new { Value = (int?)g.GroupId, Text = g.Group.Name } )
                    .ToList();

                groups.Insert( 0, new { Value = (int?)null, Text = ALL_GROUPS_STRING } );

                ddlBlackoutGroups.DataSource = groups;
                ddlBlackoutGroups.DataValueField = "Value";
                ddlBlackoutGroups.DataTextField = "Text";
                ddlBlackoutGroups.DataBind();
            }
        }

        private void mdAddBlackoutDates_cblBlackoutPersons_Bind()
        {
            using ( var rockContext = new RockContext() )
            {
                var personService = new PersonService( rockContext );

                var familyMemberAliasIds = new PersonService( rockContext )
                    .GetFamilyMembers( CurrentPerson.Id )
                    .Select( m => m.Person.Aliases.FirstOrDefault( a => a.PersonId == m.PersonId ) )
                    .Select( a => new { Value = a.Id, Text = a.Person.NickName + " " + a.Person.LastName } )
                    .ToList();

                familyMemberAliasIds.Insert( 0, new { Value = CurrentPersonAlias.Id, Text = CurrentPerson.FullName + " (you)" } );

                cblBlackoutPersons.DataSource = familyMemberAliasIds;
                cblBlackoutPersons.DataValueField = "Value";
                cblBlackoutPersons.DataTextField = "Text";
                cblBlackoutPersons.DataBind();

            }


        }

        protected void mdAddBlackoutDates_SaveClick( object sender, EventArgs e )
        {
            // parse the date range and add to query
            if ( drpBlackoutDateRange.DelimitedValues.IsNullOrWhiteSpace() )
            {
                // show error
                return;
            }

            var dateRange = DateRangePicker.CalculateDateRangeFromDelimitedValues( drpBlackoutDateRange.DelimitedValues );
            if ( !dateRange.Start.HasValue || !dateRange.End.HasValue )
            {
                // show error
                return;
            }

            int? parentId = null;

            foreach ( ListItem item in cblBlackoutPersons.Items )
            {
                if ( !item.Selected )
                {
                    continue;
                }

                var personScheduleExclusion = new PersonScheduleExclusion
                {
                    PersonAliasId = item.Value.AsInteger(),
                    StartDate = dateRange.Start.Value.Date,
                    EndDate = dateRange.End.Value.Date,
                    GroupId = ddlBlackoutGroups.SelectedValueAsId(),
                    ParentPersonScheduleExclusionId = parentId
                };

                using ( var rockContext = new RockContext() )
                {
                    new PersonScheduleExclusionService( rockContext ).Add( personScheduleExclusion );
                    rockContext.SaveChanges();

                    if ( parentId == null )
                    {
                        parentId = personScheduleExclusion.Id;
                    }
                }
            }

            HideDialog();
            BindBlackoutDates();
        }

        private void ShowDialog( string dialog, bool setValues = false )
        {
            hfActiveDialog.Value = dialog.ToUpper().Trim();
            ShowDialog( setValues );
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( bool setValues = false )
        {
            switch ( hfActiveDialog.Value )
            {
                case "MDADDBLACKOUTDATES":
                    mdAddBlackoutDates.Show();
                    mdAddBlackoutDates_ddlBlackoutGroups_Bind();
                    mdAddBlackoutDates_cblBlackoutPersons_Bind();
                    break;
            }
        }

        /// <summary>
        /// Hides the dialog.
        /// </summary>
        private void HideDialog()
        {
            switch ( hfActiveDialog.Value )
            {
                case "MDADDBLACKOUTDATES":
                    mdAddBlackoutDates.Hide();
                    break;
            }

            hfActiveDialog.Value = string.Empty;
        }

        #endregion Blackout Dates Modal

        #endregion Preferences Tab Blackout

        #endregion Preferences Tab

        #region Signup Tab

        /// <summary>
        /// Creates the dynamic controls for the sign-up tab.
        /// </summary>
        protected void CreateSignupControls()
        {
            int currentGroupId = -1;
            DateTime currentOccurrenceDate = DateTime.MinValue;
            int currentScheduleId = -1;

            availableGroupLocationSchedules = GetScheduleData().OrderBy( s => s.GroupId ).ThenBy( s => s.OccurrenceDate.Date ).ToList();
            var availableSchedules = availableGroupLocationSchedules
                .GroupBy( s => new { s.GroupId, s.ScheduleId, s.OccurrenceDate.Date } )
                .Select( s => s.First() )
                .ToList();

            foreach( var availableSchedule in availableSchedules )
            {
                if ( availableSchedule.GroupId != currentGroupId )
                {
                    currentGroupId = availableSchedule.GroupId;
                    CreateGroupHeader( availableSchedule.GroupName );
                }

                if ( availableSchedule.OccurrenceDate.Date != currentOccurrenceDate.Date )
                {
                    if (currentScheduleId != -1 )
                    {
                        phSignUpSchedules.Controls.Add( new LiteralControl( "</div>" ) );
                    }

                    currentOccurrenceDate = availableSchedule.OccurrenceDate.Date;
                    CreateDateHeader( availableSchedule.OccurrenceDate );
                }

                if( availableSchedule.ScheduleId != currentScheduleId )
                {
                    currentScheduleId = availableSchedule.ScheduleId;
                    CreateScheduleRow( availableSchedule );
                }
            }
        }

        /// <summary>
        /// Creates the group section header for the sign-up tab controls
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        private void CreateGroupHeader( string groupName )
        {
            LiteralControl lc = new LiteralControl( string.Format("<h3>{0} Schedules</h3>", groupName) );
            phSignUpSchedules.Controls.Add( lc );
        }

        /// <summary>
        /// Creates the date section header for the sign-up tab controls
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        private void CreateDateHeader( DateTime dateTime )
        {
            string date = dateTime.ToShortDateString();
            string dayOfWeek = dateTime.DayOfWeek.ToString();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( "<div class='form-control-group'>" );
            sb.AppendLine( string.Format( "<label class='control-label'>{0}&nbsp;({1})</label><br /><br />", date, dayOfWeek ) );
            phSignUpSchedules.Controls.Add( new LiteralControl( sb.ToStringSafe() ) );
        }

        /// <summary>
        /// Creates a row for a schedule with a checkbox for the time and a dll to select a location.
        /// </summary>
        /// <param name="personScheduleSignup">The person schedule signup.</param>
        private void CreateScheduleRow( PersonScheduleSignup personScheduleSignup )
        {
            var container = new HtmlGenericContainer();
            container.Attributes.Add( "class", "row" );
            container.AddCssClass( "js-person-schedule-signup-row" );

            var cbContainer = new HtmlGenericContainer();
            cbContainer.Attributes.Add( "class", "col-md-1" );

            var cb = new RockCheckBox();
            cb.ID = "dbSignupSchedule";
            cb.Text = personScheduleSignup.OccurrenceDate.ToString("hh:mm tt");
            cb.ToolTip = personScheduleSignup.ScheduleName;
            cb.Attributes.Add( "style", "float: left;" );
            cb.AddCssClass( "js-person-schedule-signup-checkbox" );
            cb.Checked = false;
            cbContainer.Controls.Add( cb );

            var locations = availableGroupLocationSchedules
                .Where( x => x.GroupId == personScheduleSignup.GroupId )
                .Where( x => x.ScheduleId == personScheduleSignup.ScheduleId )
                .Where( x => x.OccurrenceDate.Date == personScheduleSignup.OccurrenceDate.Date )
                .Select( x => new { Text = x.LocationName, Value = x.GroupId + "," + x.LocationId + "," + x.ScheduleId + "," + x.OccurrenceDate + "|" } )
                .ToList();

            var ddl = new RockDropDownList();
            ddl.ID = "ddlSignupLocations";
            ddl.Attributes.Add( "style", "width:200px" );
            ddl.DataSource = locations;
            ddl.DataTextField = "Text";
            ddl.DataValueField = "Value";
            ddl.DataBind();
            ddl.AddCssClass( "js-person-schedule-signup-ddl" );
            ddl.Items.Insert( 0, new ListItem( string.Empty, string.Empty ) );

            var ddlContainer = new HtmlGenericContainer();
            ddlContainer.Attributes.Add( "class", "col-md-11" );
            ddlContainer.Attributes.Add( "style", "padding-top: 7px;" );
            ddlContainer.Controls.Add( ddl );

            var notificationLabel = new Label();
            notificationLabel.Style.Add( "display", "none" );
            notificationLabel.Style.Add( "padding-left", "10px" );
            notificationLabel.AddCssClass( "label label-warning" );// Needs styling here
            notificationLabel.AddCssClass( "js-person-schedule-signup-notification" );
            notificationLabel.Text = "The time checkbox must be checked and a location selected in order to signup";
            ddlContainer.Controls.Add( notificationLabel );

            container.Controls.Add( cbContainer );
            container.Controls.Add( ddlContainer );
            phSignUpSchedules.Controls.Add( container );
        }

        /// <summary>
        /// Gets a list of available schedules for the group the current person belongs to.
        /// </summary>
        /// <returns></returns>
        protected List<PersonScheduleSignup> GetScheduleData()
        {
            List<PersonScheduleSignup> personScheduleSignups = new List<PersonScheduleSignup>();
            int numOfWeeks = GetAttributeValue( AttributeKeys.FutureWeeksToShow ).AsIntegerOrNull() ?? 6;
            var startDate = DateTime.Now.AddDays( 1 );
            var endDate = DateTime.Now.AddDays( numOfWeeks * 7 );

            using ( var rockContext = new RockContext() )
            {
                var scheduleService = new ScheduleService( rockContext );
                var attendanceService = new AttendanceService( rockContext );

                // Get a list of schedules that a person can sign up for
                var schedules = scheduleService.GetAvailableScheduleSignupsForPerson( CurrentPerson.Id )
                    .Tables[0]
                    .AsEnumerable()
                    .Select( s => new PersonScheduleSignup
                    {
                        GroupId = s.Field<int>("GroupId"),
                        GroupName = s.Field<string>("GroupName"),
                        LocationId = s.Field<int>("LocationId"),
                        LocationName = s.Field<string>("LocationName"),
                        ScheduleId = s.Field<int>("ScheduleId"),
                        ScheduleName = s.Field<string>("ScheduleName"),
                        ICalendarContent = s.Field<string>("ICalendarContent"),
                        Occurrences = scheduleService.Get( s.Field<int>("ScheduleId") ).GetOccurrences( startDate, endDate )
                    } )
                    .ToList();

                foreach( PersonScheduleSignup schedule in schedules )
                {
                    foreach ( var occurrence in schedule.Occurrences )
                    {
                        if ( attendanceService.IsScheduled( occurrence.Period.StartTime.Value, schedule.ScheduleId, CurrentPerson.Id ) )
                        {
                            // If the person is scheduled for any group/location for this date/schedule then do not include in the sign-up list.
                            continue;
                        }

                        // Add to master list personScheduleSignups
                        personScheduleSignups.Add( new PersonScheduleSignup
                        {
                            GroupId = schedule.GroupId,
                            GroupName = schedule.GroupName,
                            LocationId = schedule.LocationId,
                            LocationName = schedule.LocationName,
                            ScheduleId = schedule.ScheduleId,
                            ScheduleName = schedule.ScheduleName,
                            ICalendarContent = schedule.ICalendarContent,
                            OccurrenceDate = occurrence.Period.StartTime.Value
                        } );
                    }
                }

                // TODO: Remove Blackout dates for person/family

                return personScheduleSignups;
            }
        }

        /// <summary>
        /// POCO class to hold data created from the iCal object in the schedule table and group by date
        /// </summary>
        protected class PersonScheduleSignup
        {
            /// <summary>
            /// Gets or sets the group identifier.
            /// </summary>
            /// <value>
            /// The group identifier.
            /// </value>
            public int GroupId { get; set; }

            /// <summary>
            /// Gets or sets the name of the group.
            /// </summary>
            /// <value>
            /// The name of the group.
            /// </value>
            public string GroupName { get; set; }

            /// <summary>
            /// Gets or sets the location identifier.
            /// </summary>
            /// <value>
            /// The location identifier.
            /// </value>
            public int LocationId { get; set; }

            /// <summary>
            /// Gets or sets the name of the location.
            /// </summary>
            /// <value>
            /// The name of the location.
            /// </value>
            public string LocationName { get; set; }

            /// <summary>
            /// Gets or sets the schedule identifier.
            /// </summary>
            /// <value>
            /// The schedule identifier.
            /// </value>
            public int ScheduleId { get; set; }

            /// <summary>
            /// Gets or sets the name of the schedule.
            /// </summary>
            /// <value>
            /// The name of the schedule.
            /// </value>
            public string ScheduleName { get; set; }

            /// <summary>
            /// Gets or sets the string representation of iCal object
            /// </summary>
            /// <value>
            /// The content of the i calendar.
            /// </value>
            public string ICalendarContent { get; set; }

            /// <summary>
            /// Gets or sets the occurrence date.
            /// </summary>
            /// <value>
            /// The occurrence date.
            /// </value>
            public DateTime OccurrenceDate { get; set; }

            /// <summary>
            /// Gets or sets the let of Occurrences calculated from the iCal object.
            /// </summary>
            /// <value>
            /// The occurrences.
            /// </value>
            public IList<Occurrence> Occurrences { get; set; }
        }

        #endregion Signup Tab





    }
}