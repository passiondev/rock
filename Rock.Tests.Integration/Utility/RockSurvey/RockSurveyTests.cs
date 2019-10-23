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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rock.Data;
using Rock.Model;
using Rock.Utility;

namespace Rock.Tests.Integration.Utility.RockSurvey
{
    /// <summary>
    /// Integration tests for the Rock Survey feature.
    /// </summary>
    [TestClass]
    public class RockSurveyTests
    {
        #region Test Settings

        // Set this value to the URL of the Spark RockSurvey API.
        private readonly string _SparkSurveyApiUrl = "http://localhost:57822/api/org.sparkdevnetwork/RockSurvey";

        // Set this value to the name of the person credited with submitting the Rock Survey.
        // If necessary, this value can be used to identify a specific batch of test records.
        private readonly string _SparkSurveyCompletedBy = "Integration Test";

        #endregion

        /// <summary>
        /// Instructions for feature testing.
        /// </summary>
        [TestMethod]
        [TestProperty( "Feature", TestFeatures.RockSurvey )]
        public void _ReadMe()
        {
            /*
             * Integration tests for the Rock Survey API require a test installation that includes the plugin "org.sparknetwork.core".
             */
        }

        [TestMethod]
        [TestProperty( "Feature", TestFeatures.RockSurvey )]
        public void RockSurveyManager_CreateSurveyAction_CreatesNewSurvey()
        {
            // Create a default survey, and assign a new Rock Instance Id.
            var surveyManager = GetConfiguredSurveyManager();

            var survey = CreateNewDefaultSurvey( surveyManager );

            // Verify Survey information.
            Assert.IsTrue( survey.RockInstanceId != null );
            Assert.IsTrue( survey.OrganizationName != null );

            // Verify key data elements to ensure the survey executed correctly.
            Assert.IsTrue( survey.NumberOfPersons > 0 );
            Assert.IsTrue( survey.NumberOfActivePersons > 0 );
            Assert.IsTrue( survey.NumberOfFamilies > 0 );

            Assert.IsTrue( survey.NumberOfConnectionStatuses > 0 );
            Assert.IsTrue( survey.NumberOfPersonPhotos > 0 );
            Assert.IsTrue( survey.NumberOfAssessments > 0 );

            Assert.IsTrue( survey.NumberOfGroups > 0 );
            Assert.IsTrue( survey.NumberOfActiveGroups > 0 );
            Assert.IsTrue( survey.NumberOfGroupTypes > 0 );
            Assert.IsTrue( survey.NumberOfPersonAttributes > 0 );

            Assert.IsTrue( survey.EnabledDataAutomationActivities.Any() );
        }

        [TestMethod]
        [TestProperty( "Feature", TestFeatures.RockSurvey )]
        public void RockSurveyApi_UpdateSurveyNewInstance_CreatesNewSurvey()
        {
            // Create a default survey, and assign a new Rock Instance Id.
            var surveyManager = GetConfiguredSurveyManager();

            var survey = CreateNewDefaultSurvey( surveyManager );

            survey.RockInstanceId = Guid.NewGuid();

            var success = surveyManager.SendSurveyToSpark();

            // Verify result.
            Assert.IsTrue( success, "Request failed." );

            var notification = surveyManager.Notifications.FirstOrDefault();
            Assert.IsTrue( notification.Classification == NotificationClassification.Success, "Success notification expected but not found." );
            Assert.IsTrue( notification.Message.Contains( " added" ), "API notification does not indicate that the survey was created." );
        }

        [TestMethod]
        [TestProperty( "Feature", TestFeatures.RockSurvey )]
        public void RockSurveyApi_UpdateSurveyExistingInstance_UpdatesExistingSurvey()
        {
            var surveyManager = GetConfiguredSurveyManager();

            // Create a default survey, and assign a new Rock Instance Id.
            var survey = CreateNewDefaultSurvey( surveyManager );

            survey.RockInstanceId = Guid.NewGuid();

            survey.AverageWeekendAttendance = 1000;

            var success = surveyManager.SendSurveyToSpark();

            Assert.IsTrue( success, "Request failed." );

            // Change some of the Survey details and resend.
            survey.AverageWeekendAttendance = 1001;

            success = surveyManager.SendSurveyToSpark();

            // Verify result.
            Assert.IsTrue( success, "Request failed." );

            var notification = surveyManager.Notifications.FirstOrDefault();
            Assert.IsTrue( notification.Classification == NotificationClassification.Success, "Success notification expected but not found." );
            Assert.IsTrue( notification.Message.Contains( " updated" ), "API notification does not indicate that the survey was updated." );
        }

        [TestMethod]
        [TestProperty( "Feature", TestFeatures.RockSurvey )]
        public void RockSurveyApi_UpdateSurveyEmptyCompletedBy_FailsWithNotification()
        {
            var surveyManager = GetConfiguredSurveyManager();

            var survey = surveyManager.CreateSurvey();

            survey.CompletedBy = "";

            // Submit the unvalidated survey to the Spark API to test for a correct failure response.
            var success = surveyManager.SendSurveyToSpark( validateBeforeSend: false );

            var notification = surveyManager.Notifications.FirstOrDefault();

            // Verify result.
            Assert.IsFalse( success, "Request succeeded, failure expected." );
            Assert.IsTrue( notification.Classification == NotificationClassification.Danger );
        }

        [TestMethod]
        [TestProperty( "Feature", TestFeatures.RockSurvey )]
        public void RockSurveyApi_UpdateSurveyEmptyOrganizationName_FailsWithNotification()
        {
            var surveyManager = GetConfiguredSurveyManager();

            var survey = surveyManager.CreateSurvey();

            survey.OrganizationName = "";

            // Submit the unvalidated survey to the Spark API to test for a correct failure response.
            var success = surveyManager.SendSurveyToSpark( validateBeforeSend: false );

            var notification = surveyManager.Notifications.FirstOrDefault();

            // Verify result.
            Assert.IsFalse( success, "Request succeeded, failure expected." );
            Assert.IsTrue( notification.Classification == NotificationClassification.Danger );
        }

        [TestMethod]
        [TestProperty( "Feature", TestFeatures.RockSurvey )]
        public void RockSurveyApi_UpdateSurveyInvalidInstanceId_FailsWithNotification()
        {
            var surveyManager = GetConfiguredSurveyManager();

            var survey = surveyManager.CreateSurvey();

            survey.RockInstanceId = Guid.Empty;

            var success = surveyManager.SendSurveyToSpark( validateBeforeSend: false );

            var notification = surveyManager.Notifications.FirstOrDefault();

            // Verify result.
            Assert.IsFalse( success, "Request succeeded, failure expected." );
            Assert.IsTrue( notification.Classification == NotificationClassification.Danger );
        }

        /// <summary>
        /// Get a SurveyManager instance that is configured for the test database.
        /// </summary>
        /// <returns></returns>
        private RockSurveyManager GetConfiguredSurveyManager()
        {
            var dataContext = new RockContext();

            var surveyManager = new RockSurveyManager( dataContext );

            surveyManager.SparkSurveyApiUrl = _SparkSurveyApiUrl;

            return surveyManager;
        }

        private static Guid? _RockInstanceId = null;
        private static string _OrganizationNameSuffix = null;

        /// <summary>
        /// Get a new survey with default test values.
        /// </summary>
        /// <param name="surveyManager"></param>
        /// <returns></returns>
        private RockSurveyData CreateNewDefaultSurvey( RockSurveyManager surveyManager )
        {
            var survey = surveyManager.CreateSurvey();

            survey.CompletedBy = _SparkSurveyCompletedBy;

            if ( _RockInstanceId == null )
            {
                _RockInstanceId = Guid.NewGuid();
                _OrganizationNameSuffix = DateTime.Now.ToString( "yyyyMMdd" );
            }

            survey.RockInstanceId = _RockInstanceId.Value;
            survey.OrganizationName = $"{ survey.OrganizationName }_{ _OrganizationNameSuffix }";

            return survey;
        }

    }
}