// <copyright>
// Copyright by the Spark Development Network
//,
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,,,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
namespace RockWeb.Blocks.Groups
{

    [DisplayName( "Group Schedule Confirmation" )]
    [Category( "Groups" )]
    [CodeEditorField( "Confirm Heading Template", "Text to display when volunteer confirms a schedule RSVP. <span class='tip tip-lava'></span>", Rock.Web.UI.Controls.CodeEditorMode.Lava, Rock.Web.UI.Controls.CodeEditorTheme.Rock, 200, false,
    @"<h2>You’re confirmed to serve</h2><p>Thanks for letting us know.  You’re confirmed for:</p><p>{{ Group.Name }}<br>{{ ScheduledItem.Location.Name }} {{ScheduledItem.Schedule.Name }}<br></p>
<p>Thanks again!</p>
<p>{{ Group.Scheduler.FullName }}<br>{{ 'Global' | Attribute:'OrganizationName' }}</p>", "", 1, "ConfirmHeadingTemplate" )]
    [CodeEditorField( "Decline Heading Template", "Text to display when volunteer confirms a schedule RSVP. <span class='tip tip-lava'></span>", Rock.Web.UI.Controls.CodeEditorMode.Lava, Rock.Web.UI.Controls.CodeEditorTheme.Rock, 200, false,
    @"<h2>Can’t make it?</h2><p>Thanks for letting us know.  We’ll try to schedule another volunteer for:</p>
<p>{{ Group.Name }}<br>
{{ ScheduledItem.Location.Name }} {{ ScheduledItem.Schedule.Name }}<br></p>", "", 2, "DeclineHeadingTemplate" )]
    [BooleanField( "Scheduler Receive Confirmation Emails", "If checked, the scheduler will receive an email response for each confirmation or decline.", false, "", 3 )]
    [BooleanField( "Enable Decline Reasons", "If checked, a volunteer must choose one of the ‘Decline Reasons’ to submit their decline status.", true, "", 4 )]
    [BooleanField( "Enable Decline Note", "If checked, a note will be shown for the volunteer to elaborate on why they cannot attend.", false, "", 5 )]
    [BooleanField( "Require Decline Note", "If checked, a custom note response will be required in order to save their decline status.", false, "", 6 )]
    [TextField( "Decline Note Title", "A custom title for the decline elaboration note.", false, "Please elaborate on why you cannot attend.", "", 7 )]
    public partial class GroupScheduleConfirmation : Rock.Web.UI.RockBlock
    {

        #region Fields
        // used for private variables
        bool _receiveConfirmationEmail = false;
        #endregion

        #region Properties
        /// <summary>
        /// The personID of the currently logged in user.  If user is not logged in, returns null
        /// </summary>
        public int? CurrentPersonId
        {
            get { return RockPage.CurrentPersonId; }
        }
        #endregion

        #region Base Control Methods
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbError.Visible = false;

            if ( !Page.IsPostBack )
            {
                LoadBlockSettings();
                SetAttendanceOnLoad();
            }
        }

        private void LoadBlockSettings()
        {
            _receiveConfirmationEmail = GetAttributeValue( "SchedulerReceiveConfirmationEmails" ).AsBoolean();

            LoadDeclineReasons();
            // Decline reason drop down always visible
            ddlDeclineReason.Visible = true;
            // block setting drives if requred
            ddlDeclineReason.Required = GetAttributeValue( "EnableDeclineReasons" ).AsBoolean();
            this.btnSubmit.Visible = true;
            //decline Note
            dtbDeclineReasonNote.Label = GetAttributeValue( "DeclineNoteTitle" ).ToString();
            dtbDeclineReasonNote.Visible = GetAttributeValue( "EnableDeclineNote" ).AsBoolean();
            dtbDeclineReasonNote.Required = GetAttributeValue( "RequireDeclineNote" ).AsBoolean();
        }

        /// <summary>
        /// Sets the attendance by query string.
        /// </summary>
        private void SetAttendanceOnLoad()
        {
            using ( var rockContext = new RockContext() )
            {
                // otherwise use the currently logged in person
                if ( CurrentPerson == null )
                {
                    nbError.Visible = true;
                }

                ShowDetails();

                var request = Context.Request;
                var attendanceId = GetAttendanceIdFromParameters();
                if ( attendanceId != null )
                {
                    var attendance = new AttendanceService( rockContext ).Queryable().AsNoTracking().Where( a => a.Id == attendanceId.Value && a.PersonAlias.PersonId == CurrentPerson.Id ).FirstOrDefault();

                    if ( attendance == null )
                    {
                        HandleNotAuthorized();
                        return;
                    }

                    HandleMergFieldsAndDisplay( attendance );
                }
                else
                {
                    HandleNotAuthorized();
                    return;
                }
            }

        }

        private int? GetAttendanceIdFromParameters()
        {
            if ( PageParameter( "attendanceId" ).IsNotNullOrWhiteSpace() )
            {
                var attendanceId = PageParameter( "attendanceId" ).AsIntegerOrNull();
                //_targetPerson null if Log out is was called
                if ( attendanceId != null && CurrentPerson != null )
                {
                    return attendanceId;
                }
            }
            return null;
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the btnSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSubmit_Click( object sender, EventArgs e )
        {
            UpdateAttendanceDeclineReason();
        }

        private void UpdateAttendanceDeclineReason()
        {
            var attendanceId = GetAttendanceIdFromParameters();
            if ( attendanceId != null )
            {
                using ( var rockContext = new RockContext() )
                {
                    var attendance = new AttendanceService( rockContext ).Queryable().Where( a => a.Id == attendanceId.Value && a.PersonAlias.PersonId == CurrentPerson.Id ).FirstOrDefault();
                    if ( attendance != null )
                    {
                        var declineResonId = ddlDeclineReason.SelectedItem.Value.AsInteger();
                        attendance.DeclineReasonValueId = declineResonId;

                        if ( !dtbDeclineReasonNote.Text.IsNullOrWhiteSpace() )
                        {
                            attendance.Note = dtbDeclineReasonNote.Text;
                        }

                        rockContext.SaveChanges();
                    }
                    UpdateDeclineMessageAfterSubmit( attendance );
                }
            }

            pnlDeclineReason.Visible = false;

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
        /// Gets the occurrence details.
        /// </summary>
        /// <param name="attendance">The attendance.</param>
        /// <returns></returns>
        protected string GetOccurrenceDetails( Attendance attendance )
        {
            return string.Format( "{0} - {1} - {2}", attendance.Occurrence.OccurrenceDate.ToShortDateString(), attendance.Occurrence.Group.Name, attendance.Occurrence.Location );
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
        #endregion

        #region Methods
        /// <summary>
        /// Shows the details.
        /// </summary>
        private void ShowDetails()
        {
            BindPendingConfirmations();
        }

        /// <summary>
        /// Binds the pending confirmations.
        /// </summary>
        private void BindPendingConfirmations()
        {
            pnlPendingConfirmation.Visible = true;
            if ( !CurrentPersonId.HasValue )
            {
                return;
            }

            var rockContext = new RockContext();
            var qryPendingConfirmations = new AttendanceService( rockContext ).GetPendingScheduledConfirmations()
                .Where( a => a.PersonAlias.PersonId == this.CurrentPersonId.Value )
                .OrderBy( a => a.Occurrence.OccurrenceDate );

            if ( qryPendingConfirmations.Count() > 0 )
            {
                nbError.Visible = false;
            }
            rptPendingConfirmations.DataSource = qryPendingConfirmations.ToList();
            rptPendingConfirmations.DataBind();
        }

        /// <summary>
        /// Handles the not authorized.
        /// </summary>
        private void HandleNotAuthorized()
        {
            nbError.Visible = rptPendingConfirmations.Items.Count < 1;
        }

        private void UpdateDeclineMessageAfterSubmit( Attendance attendance )
        {
            //Hide Template
            lResponse.Visible = false;
            nbError.Title = "Thank you";
            nbError.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Success;
            nbError.Text = string.Format( "Thanks for letting us know.  We’ll try to schedule another volunteer for: {0}", attendance.Occurrence.Group.Name );
            nbError.Visible = true;
        }
        /// <summary>
        /// Handles the merg fields and ui display values.
        /// </summary>
        /// <param name="attendances">The attendances.</param>
        private void HandleMergFieldsAndDisplay( Attendance attendance )
        {
            var request = Context.Request;
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            var group = attendance.Occurrence.Group;
            mergeFields.Add( "Group", group );
            mergeFields.Add( "ScheduledItem", attendance );

            if ( attendance.Note.IsNotNullOrWhiteSpace() )
            {
                dtbDeclineReasonNote.Text = attendance.Note;
            }

            if ( attendance.DeclineReasonValueId != null )
            {
                ddlDeclineReason.SelectedValue = attendance.DeclineReasonValueId.ToString();
            }

            if ( request.QueryString["isConfirmed"].AsBoolean() )
            {
                ShowConfirmationHeading( mergeFields );
            }
            else
            {
                ShowDeclineHeading( mergeFields );
            }

            lBlockTitle.Text = "Email Confirmation";
        }

        /// <summary>
        /// Shows the confirmation heading.
        /// </summary>
        /// <param name="mergeFields">The merge fields.</param>
        private void ShowConfirmationHeading( IDictionary<string, object> mergeFields )
        {
            lResponse.Text = GetAttributeValue( "ConfirmHeadingTemplate" ).ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );
            lResponse.Visible = true;
            pnlDeclineReason.Visible = false;
            pnlPendingConfirmation.Visible = true;
        }

        /// <summary>
        /// Shows the decline heading.
        /// </summary>
        /// <param name="mergeFields">The merge fields.</param>
        private void ShowDeclineHeading( IDictionary<string, object> mergeFields )
        {
            pnlDeclineReason.Visible = true;
            lResponse.Text = GetAttributeValue( "DeclineHeadingTemplate" ).ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );
            lResponse.Visible = true;

        }

        /// <summary>
        /// Loads the decline reasons.
        /// </summary>
        private void LoadDeclineReasons()
        {
            var defineTypeVolunteerScheduleReason = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.VOLUNTEER_SCHEDULE_DECLINE_REASON );
            var definedValues = defineTypeVolunteerScheduleReason.DefinedValues;


            ddlDeclineReason.DataSource = definedValues;
            ddlDeclineReason.DataBind();
            ddlDeclineReason.Items.Insert( 0, new ListItem() );

        }
        #endregion

    }
}