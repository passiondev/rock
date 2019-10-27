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
using System.ComponentModel;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Administration
{
    /// <summary>
    /// Block that can load sample data into your Rock database.
    /// Dev note: You can set the XML Document URL setting to your local
    /// file when you're testing new data.  Something like C:\Misc\Rock\Documentation\sampledata.xml
    /// </summary>
    [DisplayName( "State of Rock" )]
    [Category( "Administration" )]
    [Description( "Sends aggregate data and usage patterns for your Rock system to the Spark Development Network." )]

    #region Block Attributes

    [TextField( "Submit URL",
        Description = "The server URL to which the State of Rock data will be sent.",
        Key = AttributeKey.SubmitURL,
        DefaultValue = "https://api/org.sparkdevnetwork/RockSurvey/Update",
        Order = 1 )]

    #endregion

    public partial class StateOfRock : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        private static class AttributeKey
        {
            public const string SubmitURL = "StateOfRockSubmitUrl";
        }

        #endregion Attribute Keys

        #region Properties

        private StateOfRockBlockViewModel _ViewModel;

        public StateOfRockBlockViewModel ViewModel { get { return _ViewModel; } }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // Initialize the ViewModel.
            _ViewModel = new StateOfRockBlockViewModel();

            if ( this.CurrentPerson != null )
            {
                _ViewModel.CurrentUser = this.CurrentPerson;
            }

            _ViewModel.SparkSurveyApiUrl = GetAttributeValue( AttributeKey.SubmitURL );

            // Add event handler for block updates
            this.BlockUpdated += Block_BlockUpdated;

            this.AddConfigurationUpdateTrigger( upnlContent );

            RockPage.AddScriptLink( "~/Scripts/jquery.signalR-2.2.0.min.js", false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                tbAttendance.Focus();
            }

            if ( IsPostBack )
            {
                this.BindModel();
            }
            else
            {
                this.BindControls();
            }
        }

        #endregion

        /// <summary>
        /// Bind the ViewModel properties to the View.
        /// </summary>
        private void BindControls()
        {
            this.DataBind();

            // Perform any complex binding that cannot be handled in the markup.
        }

        /// <summary>
        /// Set the ViewModel properties from the View controls.
        /// </summary>
        private void BindModel()
        {
            if ( this.IsPostBack )
            {
                _ViewModel.AverageWeeklyAttendance = tbAttendance.Text;
            }
        }

        #region Events

        /// <summary>
        /// This is the entry point for when the user clicks the "load data" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void bbtnSendData_Click( object sender, EventArgs e )
        {
            _ViewModel.SendData();

            this.BindControls();
        }

        /// <summary>
        /// This is the entry point for when the user clicks the "load data" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void bbtnResend_Click( object sender, EventArgs e )
        {
            _ViewModel.Reset();

            this.BindControls();
        }

        /// <summary>
        /// Handles the BlockUpdated event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            _ViewModel.Reset();
        }

        #endregion
    }

    /// <summary>
    /// Contains the data and logic required for the functions of the State of Rock block.
    /// </summary>
    public class StateOfRockBlockViewModel
    {
        public enum ProcessStateSpecifier
        {
            UserInput,
            SendingData,
            SendCompleted,
            SendFailed
        }

        public StateOfRockBlockViewModel()
        {
            this.Reset();
        }

        public Person CurrentUser { get; set; }

        private ProcessStateSpecifier _ProcessState = ProcessStateSpecifier.UserInput;

        public ProcessStateSpecifier ProcessState { get { return _ProcessState; } }

        public string SparkSurveyApiUrl { get; set; }

        public string ReportedBy
        {
            get
            {
                if ( this.CurrentUser == null )
                {
                    return "(unknown)";
                }
                else
                {
                    return this.CurrentUser.FullName;
                }
            }
        }

        public string AverageWeeklyAttendance { get; set; }

        public bool ValidationMessagePanelIsVisible
        {
            get
            {
                return this.ValidationNotification != null;
            }
        }

        /// <summary>
        /// Set the view model to an initial state.
        /// </summary>
        public void Reset()
        {
            _ProcessState = ProcessStateSpecifier.UserInput;

            this.CompletedNotification = new ViewModelUserNotification();
            this.ValidationNotification = new ViewModelUserNotification();

            this.SetToUserInputState();
        }

        public bool SendData()
        {
            _ProcessState = ProcessStateSpecifier.SendingData;

            var dataContext = new RockContext();

            // Create a new Rock Survey.
            var manager = new RockSurveyManager( dataContext );

            manager.SparkSurveyApiUrl = this.SparkSurveyApiUrl;

            var survey = manager.CreateSurvey();

            // Add user-entered data.
            survey.CompletedBy = this.ReportedBy;

            var attendance = this.AverageWeeklyAttendance.AsInteger();

            survey.AverageWeekendAttendance = attendance;

            // Validate the survey content.
            var isValid = manager.ValidateSurvey();

            if ( !isValid )
            {
                var validationNotification = new ViewModelUserNotification();

                validationNotification.NotificationType = NotificationBoxType.Validation;
                validationNotification.Title = "Please correct the following issues before re-submitting your survey.";

                foreach ( var message in manager.Notifications )
                {
                    validationNotification.Message += string.Format( "<br>{0}: {1}", message.Title, message.Message );
                }

                this.ValidationNotification = validationNotification;

                return false;
            }

            // Validate the server configuration.
            if ( string.IsNullOrWhiteSpace( this.SparkSurveyApiUrl ) )
            {
                var validationNotification = new ViewModelUserNotification();

                validationNotification.NotificationType = NotificationBoxType.Validation;
                validationNotification.Title = "Please correct the following issues before re-submitting your survey.";
                validationNotification.Message = "A Server URL must be configured.";

                this.ValidationNotification = validationNotification;

                return false;
            }

            // Send the survey.
            isValid = manager.SendSurveyToSpark();

            this.CompletedNotification = new ViewModelUserNotification();

            if ( isValid )
            {
                this.CompletedNotification.NotificationType = NotificationBoxType.Success;
                this.CompletedNotification.Message = string.Format( "State of Rock data was sent on {0:d} by {1}.<br>Select the Resend button below to update the data.", survey.CreatedDateTime, survey.CompletedBy );

                _ProcessState = ProcessStateSpecifier.SendCompleted;
            }
            else
            {
                this.CompletedNotification.NotificationType = NotificationBoxType.Danger;
                this.CompletedNotification.Message = "State of Rock data could not be sent.<br>There was a problem submitting the data to the server.";

                _ProcessState = ProcessStateSpecifier.SendFailed;
            }

            return isValid;
        }

        public void SetToUserInputState()
        {
            _ProcessState = ProcessStateSpecifier.UserInput;
        }

        public bool SendPanelIsVisible
        {
            get
            {
                return ( !CompletedPanelIsVisible );
            }
        }

        public bool CompletedPanelIsVisible
        {
            get
            {
                return _ProcessState == ProcessStateSpecifier.SendCompleted || _ProcessState == ProcessStateSpecifier.SendFailed;
            }
        }
        public bool SurveyDataWarningIsVisible
        {
            get
            {
                return _ProcessState == ProcessStateSpecifier.UserInput;
            }
        }

        public ViewModelUserNotification CompletedNotification { get; set; }

        public ViewModelUserNotification ValidationNotification { get; set; }
    }

    /// <summary>
    /// A user notification that can be managed by a ViewModel for presentation by a View.
    /// </summary>
    public class ViewModelUserNotification
    {
        /// <summary>
        /// Gets or sets the title of the user notification.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the type of the user notification.
        /// </summary>
        public NotificationBoxType NotificationType { get; set; }

        /// <summary>
        /// Gets or sets the user notification message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Create a new user notification.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ViewModelUserNotification New( string title, string message, NotificationBoxType type = NotificationBoxType.Info )
        {
            var notification = new ViewModelUserNotification();

            if ( string.IsNullOrWhiteSpace( title ) )
            {
                if ( type == NotificationBoxType.Danger )
                {
                    title = "Error";
                }
                else if ( type == NotificationBoxType.Validation )
                {
                    title = "Invalid Value";
                }
            }

            notification.Title = title;
            notification.Message = message;
            notification.NotificationType = type;

            return notification;
        }
    }
}