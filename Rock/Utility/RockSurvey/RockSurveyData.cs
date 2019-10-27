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
using System.Runtime.Serialization;

namespace Rock.Utility
{
    /// <summary>
    /// Contains summary metrics and data points for a specific Rock installation.
    /// </summary>
    [DataContract]
    public class RockSurveyData
    {
        #region Survey Information

        /// <summary>
        /// Gets or sets the Rock instance identifier.
        /// </summary>
        [DataMember]
        public Guid RockInstanceId { get; set; }

        /// <summary>
        /// Gets or sets the Rock version.
        /// </summary>
        [DataMember]
        public string RockVersion { get; set; }

        /// <summary>
        /// Gets or sets the full name of the person who submitted the survey response.
        /// </summary>
        [DataMember]
        public string CompletedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time on which the survey was compiled.
        /// </summary>
        [DataMember]
        public DateTime? CreatedDateTime { get; set; }

        #endregion

        #region Organization Information

        /// <summary>
        /// Gets or sets the name of the organization.
        /// </summary>
        [DataMember]
        public string OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the organization address.
        /// </summary>
        [DataMember]
        public string OrganizationAddress { get; set; }

        /// <summary>
        /// Gets or sets the average weekly attendance for this organization.
        /// This measure provides an indication of the size of the organization, and is provided by the respondent.
        /// </summary>
        [DataMember]
        public int AverageWeekendAttendance { get; set; }

        #endregion

        #region CRM Module Data

        /// <summary>
        /// Gets or sets the total number of Person records.
        /// </summary>
        [DataMember]
        public int NumberOfPersons { get; set; }

        /// <summary>
        /// Gets or sets the total number of active Person records.
        /// </summary>
        [DataMember]
        public int NumberOfActivePersons { get; set; }

        /// <summary>
        /// Gets or sets the total number of Business records.
        /// </summary>
        [DataMember]
        public int NumberOfBusinesses { get; set; }

        /// <summary>
        /// Gets or sets the total number of Families.
        /// </summary>
        [DataMember]
        public int NumberOfFamilies { get; set; }

        /// <summary>
        /// Gets or sets the total number of Connection Statuses that can be used to classify a person's relationship with the organization.
        /// </summary>
        [DataMember]
        public int NumberOfConnectionStatuses { get; set; }

        /// <summary>
        /// Gets or sets the total number of stored person photos.
        /// </summary>
        [DataMember]
        public int NumberOfPersonPhotos { get; set; }

        /// <summary>
        /// Gets or sets the total number of people marked as followed.
        /// </summary>
        [DataMember]
        public int NumberOfFollowedPeople { get; set; }

        /// <summary>
        /// Gets or sets the total number of Assessments.
        /// </summary>
        [DataMember]
        public int NumberOfAssessments { get; set; }

        /// <summary>
        /// Gets or sets the total number of background checks conducted for people.
        /// </summary>
        [DataMember]
        public int NumberOfBackgroundChecks { get; set; }

        /// <summary>
        /// Gets or sets the total number of duplicate person records.
        /// </summary>
        [DataMember]
        public int NumberOfPersonDuplicates { get; set; }

        /// <summary>
        /// Get the most recent run date for the National Change of Address (NCOA) report.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public DateTime? LastNCOARunDate { get; set; }

        /// <summary>
        /// Gets the total number of Tags marked as having Organization scope.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfOrganisationTags { get; set; }

        /// <summary>
        /// Gets the total number of Tags marked as having Personal scope.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfPersonalTags { get; set; }

        /// <summary>
        /// Gets the total number Person Attributes.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfPersonAttributes { get; set; }

        #endregion

        #region CRM Module/Groups Feature

        /// <summary>
        /// Gets or sets the total number of Group records.
        /// </summary>
        [DataMember]
        public int NumberOfGroups { get; set; }

        /// <summary>
        /// Gets or sets the total number of Group Type records.
        /// </summary>
        [DataMember]
        public int NumberOfGroupTypes { get; set; }

        /// <summary>
        /// Gets or sets the total number of active Group records.
        /// </summary>
        [DataMember]
        public int NumberOfActiveGroups { get; set; }

        /// <summary>
        /// Gets or sets the total number of archived Group records.
        /// </summary>
        [DataMember]
        public int NumberOfArchivedGroups { get; set; }

        #endregion

        #region Attendance/Checkin Module

        /// <summary>
        /// Gets the total number of Attendance records.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfAttendanceRecords { get; set; }

        /// <summary>
        /// Gets or sets the total number of Schedules.
        /// </summary>
        /// <returns></returns>        
        [DataMember]
        public int NumberOfSchedules { get; set; }

        #endregion

        #region CMS Module

        /// <summary>
        /// Gets or sets the total number of websites.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfSites { get; set; }

        /// <summary>
        /// Gets or sets the total number of website pages.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfPages { get; set; }

        /// <summary>
        /// Gets or sets the total number of content channels.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfContentChannels { get; set; }

        /// <summary>
        /// Gets or sets the total number of items in content channels.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfContentChannelItems { get; set; }

        /// <summary>
        /// Gets or sets the total number of Lava shortcodes.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfShortcodes { get; set; }

        #endregion

        #region Core Module

        /// <summary>
        /// Gets or sets the total number of History records.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfHistoryRecords { get; set; }

        /// <summary>
        /// Gets or sets the total number of File Types used to store external data.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfBinaryFileTypes { get; set; }

        /// <summary>
        /// Gets or sets the total number of Note Types that have been defined for custom notes associated with Rock entities.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfNoteTypes { get; set; }

        /// <summary>
        /// Gets or sets the total number of Attribute Matrix Templates that have been defined.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfAttributeMatrixTemplates { get; set; }

        #endregion

        #region Workflows Module

        /// <summary>
        /// Gets the total number of defined Workflow Types.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfWorkflowTypes { get; set; }

        /// <summary>
        /// Gets the total number of Workflow instances.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfWorkflowInstances { get; set; }

        /// <summary>
        /// Gets the total number of Workflow instances that are currently flagged as active.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfActiveWorkflowInstances { get; set; }

        /// <summary>
        /// Gets the total number of defined Workflow Types.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfWorkflowTriggers { get; set; }

        #endregion

        #region Database Information

        /// <summary>
        /// Gets or sets the total size of the current database.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public decimal DatabaseSizeMB { get; set; }

        /// <summary>
        /// Gets or sets the unallocated space available in the current database.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public decimal DatabaseSizeUnallocatedMB { get; set; }

        /// <summary>
        /// Gets or sets a collection of metadata entries for the tables in the current database.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public List<DatabaseTableInfo> DatabaseTableSizes { get; set; }

        /// <summary>
        /// Gets or sets the number of tables in the current database that are used to store plug-in data.
        /// </summary>
        [DataMember]
        public int NumberOfDatabasePluginTables { get; set; }

        /// <summary>
        /// Metadata for a table in the current database.
        /// </summary>
        [DataContract]
        public class DatabaseTableInfo
        {
            /// <summary>
            /// The name of the table.
            /// </summary>
            [DataMember]
            public string TableName { get; set; }
            /// <summary>
            /// The number of rows in the table.
            /// </summary>
            [DataMember]
            public long RowCount { get; set; }
            /// <summary>
            /// The size of the table in megabytes (MB).
            /// </summary>
            [DataMember]
            public decimal SizeMB { get; set; }
        }

        #endregion

        #region Communications Module

        /// <summary>
        /// Get or sets the number of Communications.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfCommunications { get; set; }

        /// <summary>
        /// Get or sets the number of Communication Recipients.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfCommunicationRecipients { get; set; }

        /// <summary>
        /// Get or sets the number of Communications Lists.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfCommunicationLists { get; set; }

        /// <summary>
        /// Get or sets the number of SMS Sender accounts that are available for sending SMS.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfSmsSenderNumbers { get; set; }

        #endregion

        #region Connections Module

        /// <summary>
        /// Get or sets the total number of defined Connection Types.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfConnectionTypes { get; set; }

        /// <summary>
        /// Get or sets the total number of Connection Opportunties.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfConnectionOpportunities { get; set; }

        /// <summary>
        /// Get or sets the total number of Connection Requests.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfConnectionRequests { get; set; }

        /// <summary>
        /// Get or sets the total number of Connection Requests that are currently flagged as active.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfActiveConnectionRequests { get; set; }

        #endregion

        #region Events Module

        /// <summary>
        /// Get or sets the total number of Event Templates.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfEventRegistrationTemplates { get; set; }

        /// <summary>
        /// Get or sets the total number of individuals that have registered for events.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfRegistrationInstances { get; set; }

        /// <summary>
        /// Get or sets the total number of Registrations received for events.
        /// A registration may include one or more individual registrants.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfEventRegistrations { get; set; }

        /// <summary>
        /// Get or sets the total number of Event Calendars.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfCalendars { get; set; }

        /// <summary>
        /// Get or sets the total number of scheduled Events.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfEvents { get; set; }

        #endregion

        #region Finance Module

        /// <summary>
        /// Get or sets the number of active Financial Gateways.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfActiveFinancialGateways { get; set; }

        /// <summary>
        /// Get or sets the total number of financial transaction batches.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfFinancialBatches { get; set; }

        /// <summary>
        /// Get or sets the total number of financial transactions.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfFinancialTransactions { get; set; }

        /// <summary>
        /// Get or sets the total number of financial pledges.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfFinancialPledges { get; set; }

        #endregion

        #region Interactions

        /// <summary>
        /// Get or sets the total number of Interactions recorded on all channels.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfInteractions { get; set; }

        /// <summary>
        /// Get or sets the total number of channels used to track different types of interactions.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfInteractionChannels { get; set; }

        /// <summary>
        /// Get or sets the total number of components that are configured to track interactions.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfInteractionComponents { get; set; }

        #endregion

        #region Organization

        /// <summary>
        /// Get or sets the total number of Campuses.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfCampuses { get; set; }

        #endregion

        #region Prayer Requests Module

        /// <summary>
        /// Get or sets the total number of Prayer Requests.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfPrayerRequests { get; set; }

        #endregion

        #region Reporting Module

        /// <summary>
        /// Get or sets the total number of Data Views.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfDataViews { get; set; }

        /// <summary>
        /// Get or sets the total number of Reports.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfReports { get; set; }

        /// <summary>
        /// Get or sets the total number of Metrics.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfMetrics { get; set; }

        /// <summary>
        /// Get or sets the total number of values recorded for Metrics.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfMetricValues { get; set; }

        #endregion

        #region Security

        /// <summary>
        /// Get or sets the total number Person Attributes.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfUserLogins { get; set; }

        /// <summary>
        /// Get the number of Security Groups.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int NumberOfSecurityGroups { get; set; }

        #endregion

        #region Utility Features

        /// <summary>
        /// Gets or sets a collection of names of enabled data automation activities.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public List<string> EnabledDataAutomationActivities { get; set; }

        #endregion
    }
}