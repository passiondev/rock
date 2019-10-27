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
using System.Linq;
using System.Net;

using Newtonsoft.Json;
using RestSharp;

using Rock.Data;
using Rock.Model;

namespace Rock.Utility
{
    /// <summary>
    /// Manages the collation and submission of a Rock Survey.
    /// A Rock Survey provides metrics and usage data for a specific Rock installation.
    /// </summary>
    public partial class RockSurveyManager
    {
        private List<Notification> _Notifications = new List<Notification>();
        private RockContext _DataContext;
        private RockSurveyData _Survey = new RockSurveyData();

        /// <summary>
        /// The base URL of the Spark Survey API.
        /// </summary>
        public string SparkSurveyApiUrl { get; set; }

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="dataContext"></param>
        public RockSurveyManager( RockContext dataContext )
        {
            _DataContext = dataContext;

            // Set default client URL.
            this.SparkSurveyApiUrl = "https://api/org.sparkdevnetwork/RockSurvey";
        }

        /// <summary>
        /// The most recent Rock Survey built by this manager.
        /// </summary>
        public RockSurveyData Survey
        {
            get
            {
                return _Survey;
            }
        }

        /// <summary>
        /// Notification messages from the most recent action.
        /// </summary>
        public List<Notification> Notifications
        {
            get
            {
                return _Notifications;
            }
        }

        /// <summary>
        /// Creates a new State of Rock Survey and populates it with metrics for the current Rock installation.
        /// The survey must be manually completed before it is submitted to Spark.
        /// </summary>
        /// <returns></returns>
        public RockSurveyData CreateSurvey()
        {
            var builder = new RockSurveyFactory( _DataContext );

            _Survey = builder.CreateNewSurvey();

            return _Survey;
        }

        /// <summary>
        /// Verify that the current survey is complete and ready to submit.
        /// </summary>
        /// <returns></returns>
        public bool ValidateSurvey()
        {
            this.ClearNotifications();

            if ( _Survey == null )
            {
                _Notifications.Add( new Notification { Title = "Invalid Rock Survey", Message = "Create a new survey before attempting to validate it." } );
            }
            else
            {
                if ( _Survey.RockInstanceId == Guid.Empty )
                {
                    _Notifications.Add( new Notification { Title = "Invalid Rock Instance ID", Message = "An instance identifier has not been configured. Set a unique identifier for this Rock instance in the application settings." } );
                }

                if ( string.IsNullOrWhiteSpace( _Survey.CompletedBy ) )
                {
                    _Notifications.Add( new Notification { Title = "Invalid Survey Details", Message = "The Completed By field should indicate the name of the person responsible for completing the survey." } );
                }
            }

            return !_Notifications.Any();
        }

        /// <summary>
        /// Sends the current survey data to Spark.
        /// </summary>
        /// <param name="validateBeforeSend">A flag to indicate if the survey should be validated prior to being submitted.</param>
        public bool SendSurveyToSpark( bool validateBeforeSend = true )
        {
            ClearNotifications();

            if ( string.IsNullOrWhiteSpace( this.SparkSurveyApiUrl ) )
            {
                _Notifications.Add( new Notification()
                {
                    Title = "Send Survey Failed",
                    Message = "A Server URL must be specified.",
                    Classification = NotificationClassification.Danger
                } );

                return false;
            }

            var isValid = true;

            if ( validateBeforeSend )
            {
                isValid = ValidateSurvey();

                if ( !isValid )
                {
                    return false;
                }
            }

            // Post the survey data and process the reponse.
            var requestJson = JsonConvert.SerializeObject( _Survey );

            var client = new RestClient( $"{ this.SparkSurveyApiUrl }/Update" );

            var request = new RestRequest( Method.POST );

            request.AddParameter( "application/json", requestJson, ParameterType.RequestBody );

            try
            {
                var response = client.Execute( request );

                if ( response.StatusCode == HttpStatusCode.Created )
                {
                    _Notifications.Add( new Notification()
                    {
                        Title = "Survey Created",
                        Message = "A new survey was added for the organization.",
                        Classification = NotificationClassification.Success
                    } );
                }
                else if ( response.StatusCode == HttpStatusCode.OK )
                {
                    _Notifications.Add( new Notification()
                    {
                        Title = "Survey Updated",
                        Message = "The survey was updated.",
                        Classification = NotificationClassification.Success
                    } );
                }
                else if ( response.StatusCode == HttpStatusCode.BadRequest )
                {
                    isValid = false;

                    _Notifications.Add( new Notification()
                    {
                        Title = "Send Survey Failed",
                        Message = response.Content,
                        Classification = NotificationClassification.Danger
                    } );
                }
                else
                {
                    isValid = false;

                    _Notifications.Add( new Notification()
                    {
                        Title = "Send Survey Failed",
                        Message = string.Format( "Could not contact the server at \"{0}\".\n{1}", this.SparkSurveyApiUrl, response.ErrorMessage ),
                        Classification = NotificationClassification.Danger
                    } );
                }
            }
            catch ( Exception ex )
            {
                isValid = false;

                _Notifications.Add( new Notification()
                {
                    Title = "Send Survey Failed",
                    Message = ex.Message,
                    Classification = NotificationClassification.Danger
                } );
            }

            return isValid;
        }

        /// <summary>
        /// Clear the notification log.
        /// </summary>
        private void ClearNotifications()
        {
            if ( _Notifications == null )
            {
                _Notifications = new List<Notification>();
            }
            else
            {
                _Notifications.Clear();
            }
        }
    }
}