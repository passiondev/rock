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

        List<PersonScheduleSignup> availableGroupLocationSchedules;

        /// <summary>
        /// Gets or sets the selected person identifier.
        /// </summary>
        /// <value>
        /// The selected person identifier.
        /// </value>
        public int? SelectedPersonId
        {
            get
            {
                return hfSelectedPersonId.Value.AsIntegerOrNull();
            }

            set
            {
                hfSelectedPersonId.Value = value.ToString();
            }
        }

        /// <summary>
        /// 
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

            if ( !Page.IsPostBack )
            {
                var targetPerson = this.ContextEntity<Person>();
                if ( targetPerson != null )
                {
                    this.SelectedPersonId = targetPerson.Id;
                }
                else
                {
                    this.SelectedPersonId = this.CurrentPersonId;

                    // DEBUG Cindy Decker
                    this.SelectedPersonId = 58;
                    ppSelectedPerson.SetValue( new PersonService( new RockContext() ).GetNoTracking( 58 ) );
                }

                LoadDropDowns();
                ShowDetails();
            }
        }

        #endregion

        #region Events

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
        /// Handles the SelectPerson event of the ppSelectedPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppSelectedPerson_SelectPerson( object sender, EventArgs e )
        {
            this.SelectedPersonId = ppSelectedPerson.PersonId;
            ShowPersonDetails();
        }

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

        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetails()
        {
            ShowSelectedTab();

            BindPendingConfirmations();
            BindUpcomingSchedules();
            CreateSignupControls();
        }

        /// <summary>
        /// Shows selected person details.
        /// </summary>
        private void ShowPersonDetails()
        {
            BindPendingConfirmations();
            BindUpcomingSchedules();
            CreateSignupControls();
        }

        /// <summary>
        /// Binds the Pending Confirmations grid.
        /// </summary>
        private void BindPendingConfirmations()
        {
            if ( !this.SelectedPersonId.HasValue )
            {
                return;
            }

            var rockContext = new RockContext();
            var qryPendingConfirmations = new AttendanceService( rockContext ).GetPendingScheduledConfirmations()
                .Where( a => a.PersonAlias.PersonId == this.SelectedPersonId.Value )
                .OrderBy( a => a.Occurrence.OccurrenceDate );

            rptPendingConfirmations.DataSource = qryPendingConfirmations.ToList();
            rptPendingConfirmations.DataBind();
        }

        /// <summary>
        /// Binds the Upcoming Schedules grid.
        /// </summary>
        private void BindUpcomingSchedules()
        {
            if ( !this.SelectedPersonId.HasValue )
            {
                return;
            }

            var currentDateTime = RockDateTime.Now;

            var rockContext = new RockContext();
            var qryPendingConfirmations = new AttendanceService( rockContext ).GetConfirmedScheduled()
                .Where( a => a.PersonAlias.PersonId == this.SelectedPersonId.Value )
                .Where( a => a.Occurrence.OccurrenceDate >= currentDateTime )
                .OrderBy( a => a.Occurrence.OccurrenceDate );

            rptUpcomingSchedules.DataSource = qryPendingConfirmations.ToList();
            rptUpcomingSchedules.DataBind();
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

        #endregion

        protected void ddlGroupMemberScheduleTemplate_SelectedIndexChanged( object sender, EventArgs e )
        {
            // TODO
        }

        protected void rptGroupPreferences_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var lGroupPreferencesGroupName = e.Item.FindControl( "lGroupPreferencesGroupName" ) as Literal;
            var ddlGroupMemberScheduleTemplate = e.Item.FindControl( "ddlGroupMemberScheduleTemplate" ) as RockDropDownList;

            // TODO
        }

        protected void rptGroupPreferenceAssignments_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {

        }

        protected void cbGroupPreferenceAssignmentScheduleTime_CheckedChanged( object sender, EventArgs e )
        {

        }

        #region Signup Tab

        protected void CreateSignupControls()
        {
            int currentGroupId = -1;
            DateTime currentOccurrenceDate = DateTime.MinValue;
            int currentScheduleId = -1;

            availableGroupLocationSchedules = GetScheduleData().OrderBy( s => s.GroupId ).ThenBy( s => s.OccurrenceDate.Date ).ToList();

            foreach( var availableGroupLocationSchedule in availableGroupLocationSchedules )
            {
                if ( availableGroupLocationSchedule.GroupId != currentGroupId )
                {
                    currentGroupId = availableGroupLocationSchedule.GroupId;
                    CreateGroupHeader( availableGroupLocationSchedule.GroupName );
                }

                if ( availableGroupLocationSchedule.OccurrenceDate.Date != currentOccurrenceDate.Date )
                {
                    if (currentScheduleId != -1 )
                    {
                        phSignUpSchedules.Controls.Add( new LiteralControl( "</div>" ) );
                    }

                    currentOccurrenceDate = availableGroupLocationSchedule.OccurrenceDate.Date;
                    CreateDateHeader( availableGroupLocationSchedule.OccurrenceDate );
                }

                if( availableGroupLocationSchedule.ScheduleId != currentScheduleId )
                {
                    currentScheduleId = availableGroupLocationSchedule.ScheduleId;
                    CreateScheduleRow( availableGroupLocationSchedule );
                }
            }
        }

        private void CreateGroupHeader( string groupName )
        {
            LiteralControl lc = new LiteralControl( string.Format("<h3>{0} Schedules</h3>", groupName) );
            phSignUpSchedules.Controls.Add( lc );
        }


        private void CreateDateHeader( DateTime dateTime )
        {
            string date = dateTime.ToShortDateString();
            string dayOfWeek = dateTime.DayOfWeek.ToString();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( "<div class='form-control-group'>" );
            sb.AppendLine( string.Format( "<label class='control-label'>{0}&nbsp;({1})</label><br /><br />", date, dayOfWeek ) );
            phSignUpSchedules.Controls.Add( new LiteralControl( sb.ToStringSafe() ) );
        }

        private void CreateScheduleRow( PersonScheduleSignup personScheduleSignup )
        {
            var container = new HtmlGenericContainer();
            container.Attributes.Add( "class", "row" );

            var cbContainer = new HtmlGenericContainer();
            cbContainer.Attributes.Add( "class", "col-md-1" );

            var cb = new CheckBox();
            cb.Text = personScheduleSignup.OccurrenceDate.ToString("hh:mm tt");
            cb.ToolTip = personScheduleSignup.ScheduleName;
            cb.Width = 200;
            cb.Attributes.Add( "style", "float: left;" );
            cbContainer.Controls.Add( cb );
            
            var locations = availableGroupLocationSchedules
                .Where( x => x.GroupId == personScheduleSignup.GroupId )
                .Where( x => x.ScheduleId == personScheduleSignup.ScheduleId )
                .Where( x => x.OccurrenceDate.Date == personScheduleSignup.OccurrenceDate.Date )
                .Select( x => new { Text = x.LocationName, Value = x.LocationId } )
                .ToList();

            var ddl = new DropDownList();
            ddl.Attributes.Add( "style", "width:200px" );
            ddl.DataSource = locations;
            ddl.DataTextField = "Text";
            ddl.DataValueField = "Value";
            ddl.DataBind();
            ddl.Items.Insert( 0, new ListItem( string.Empty, string.Empty ) );

            var ddlContainer = new HtmlGenericContainer();
            ddlContainer.Attributes.Add( "class", "col-md-11" );
            ddlContainer.Attributes.Add( "style", "padding-top: 7px;" );
            ddlContainer.Controls.Add( ddl );

            container.Controls.Add( cbContainer );
            container.Controls.Add( ddlContainer );

            //string validationFunction = @"";

            //CustomValidator cv = new CustomValidator
            //{
            //    ID = this.ID + "_cfv",
            //    ClientValidationFunction = validationFunction,
            //    ErrorMessage = "The time checkbox must be check and a location must be selected.",
            //    CssClass = "validation-error help-inline",
            //    Enabled = true,
            //    Display = ValidatorDisplay.Dynamic
            //};
            //container.Controls.Add( cv );

            
            phSignUpSchedules.Controls.Add( container );
        }


        protected List<PersonScheduleSignup> GetScheduleData()
        {
            if (this.SelectedPersonId == null )
            {
                return null;
            }

            List<PersonScheduleSignup> personScheduleSignups = new List<PersonScheduleSignup>();
            int numOfWeeks = GetAttributeValue( AttributeKeys.FutureWeeksToShow ).AsIntegerOrNull() ?? 6;
            var startDate = DateTime.Now.AddDays( 1 );
            var endDate = DateTime.Now.AddDays( numOfWeeks * 7 );

            using ( var rockContext = new RockContext() )
            {
                var scheduleService = new ScheduleService( rockContext );

                // Get a list of schedules that a person can sign up for
                var schedules = scheduleService.GetAvailableScheduleSignupsForPerson( this.SelectedPersonId.Value )
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
        /// POCO class to hold data created from the iCal object in the schedule table and group by date, group
        /// </summary>
        protected class PersonScheduleSignup
        {
            public int GroupId { get; set; }
	        public string GroupName { get; set; }
	        public int LocationId { get; set; }
	        public string LocationName { get; set; }
	        public int ScheduleId { get; set; }
	        public string ScheduleName { get; set; }
            public string ICalendarContent { get; set; }
            public DateTime OccurrenceDate { get; set; }
            public IList<Occurrence> Occurrences { get; set; }
        }

        #endregion Signup Tab

        protected void btnSaveSignups_Click( object sender, EventArgs e )
        {
            foreach ( Control control in pnlSignup.FindControl( phSignUpSchedules.ClientID ).Controls )
            {

                if ( control is HtmlGenericContainer )
                {
                    if (control is CheckBox)
                    {
                        if ( ( ( CheckBox ) control ).Checked )
                        {

                        }
                    }
                }

                
            }
        }
    }
}