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
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Groups
{
    /// <summary>
    ///
    /// </summary>
    [DisplayName( "Group Scheduler" )]
    [Category( "Groups" )]
    [Description( "Allows volunteer schedules for groups and locations to be managed by a scheduler." )]

    [IntegerField(
        "Number of Weeks To Show",
        Description = "The number of weeks out that can scheduled.",
        IsRequired = true,
        DefaultValue = "6",
        Order = 0,
        Key = AttributeKeys.FutureWeeksToShow )]

    public partial class GroupScheduler : RockBlock
    {
        /// <summary>
        /// 
        /// </summary>
        protected class AttributeKeys
        {
            /// <summary>
            /// The future weeks to show
            /// </summary>
            public const string FutureWeeksToShow = "FutureWeeksToShow";
        }

        #region UserPreferenceKeys

        /// <summary>
        /// Keys to use for UserPreferences
        /// </summary>
        protected static class UserPreferenceKey
        {
            /// <summary>
            /// The selected group identifier
            /// </summary>
            public const string SelectedGroupId = "SelectedGroupId";

            /// <summary>
            /// The selected date
            /// </summary>
            public const string SelectedDate = "SelectedDate";

            /// <summary>
            /// The selected schedule id
            /// </summary>
            public const string SelectedScheduleId = "SelectedScheduleId";

            /// <summary>
            /// The selected group location ids
            /// </summary>
            public const string SelectedGroupLocationIds = "SelectedGroupLocationIds";

            /// <summary>
            /// The selected resource list source type
            /// </summary>
            public const string SelectedResourceListSourceType = "SelectedResourceListSourceType";

            /// <summary>
            /// The group member filter type
            /// </summary>
            public const string GroupMemberFilterType = "GroupMemberFilterType";

            /// <summary>
            /// The alternate group identifier
            /// </summary>
            public const string AlternateGroupId = "AlternateGroupId";

            /// <summary>
            /// The data view identifier
            /// </summary>
            public const string DataViewId = "DataViewId";
        }

        #endregion UserPreferanceKeys

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/dragula.min.js", true );
            RockPage.AddCSSLink( "~/Themes/Rock/Styles/group-scheduler.css", true );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            LoadDropDowns();
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
                LoadFilterFromUserPreferences();
                ApplyFilter();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns()
        {
            bgResourceListSource.BindToEnum<SchedulerResourceListSourceType>();
            rblGroupMemberFilter.BindToEnum<SchedulerResourceGroupMemberFilterType>();

            int numOfWeeks = GetAttributeValue( AttributeKeys.FutureWeeksToShow ).AsIntegerOrNull() ?? 6;

            ddlWeek.Items.Clear();

            var sundayDate = RockDateTime.Now.SundayDate();
            int weekNum = 0;
            while ( weekNum < numOfWeeks )
            {
                string weekTitle = string.Format( "Week of {0} to {1}", sundayDate.AddDays( -6 ).ToShortDateString(), sundayDate.ToShortDateString() );
                ddlWeek.Items.Add( new ListItem( weekTitle, sundayDate.ToISO8601DateString() ) );
                weekNum++;
                sundayDate = sundayDate.AddDays( 7 );
            }
        }

        /// <summary>
        /// Updates the list of schedules for the selected group
        /// </summary>
        private void UpdateScheduleList()
        {
            Group group = GetSelectedGroup();

            if ( group == null )
            {
                pnlScheduler.Visible = false;
                return;
            }

            bool canSchedule = group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) || group.IsAuthorized( Authorization.SCHEDULE, this.CurrentPerson );
            if ( !canSchedule )
            {
                nbNotice.Heading = "Sorry";
                nbNotice.Text = "<p>You're not authorized to schedule resources for the selected group.</p>";
                nbNotice.NotificationBoxType = NotificationBoxType.Warning;
                nbNotice.Visible = true;
                pnlScheduler.Visible = false;
                return;
            }
            else
            {
                nbNotice.Visible = false;
            }

            nbGroupWarning.Visible = false;
            pnlGroupScheduleLocations.Visible = false;
            pnlScheduler.Visible = false;

            if ( group != null )
            {
                var groupLocations = group.GroupLocations.ToList();
                var groupSchedules = groupLocations.SelectMany( a => a.Schedules ).DistinctBy( a => a.Guid ).ToList();
                if ( !groupSchedules.Any() )
                {
                    nbGroupWarning.Text = "Group does not have any locations or schedules";
                    nbGroupWarning.Visible = true;
                }
                else
                {
                    pnlGroupScheduleLocations.Visible = true;
                    pnlScheduler.Visible = true;
                    rblSchedule.Items.Clear();
                    foreach ( var schedule in groupSchedules )
                    {
                        var listItem = new ListItem();
                        if ( schedule.Name.IsNotNullOrWhiteSpace() )
                        {
                            listItem.Text = schedule.Name;
                        }
                        else
                        {
                            listItem.Text = schedule.FriendlyScheduleText;
                        }

                        listItem.Value = schedule.Id.ToString();
                        rblSchedule.Items.Add( listItem );
                    }
                }
            }
        }

        /// <summary>
        /// Gets the selected group.
        /// </summary>
        /// <returns></returns>
        private Group GetSelectedGroup()
        {
            var groupId = hfGroupId.Value.AsIntegerOrNull();
            var rockContext = new RockContext();
            Group group = null;
            if ( groupId.HasValue )
            {
                group = new GroupService( rockContext ).GetNoTracking( groupId.Value );
            }

            return group;
        }

        /// <summary>
        /// Loads the filter from user preferences.
        /// </summary>
        private void LoadFilterFromUserPreferences()
        {
            var selectedSundayDate = this.GetBlockUserPreference( UserPreferenceKey.SelectedDate ).AsDateTime();
            var selectedWeekItem = ddlWeek.Items.FindByValue( selectedSundayDate.ToISO8601DateString() );
            if ( selectedWeekItem != null )
            {
                selectedWeekItem.Selected = true;
            }
            else
            {
                ddlWeek.SelectedIndex = 0;
            }

            hfGroupId.Value = this.GetBlockUserPreference( UserPreferenceKey.SelectedGroupId );
            gpGroup.SetValue( hfGroupId.Value.AsIntegerOrNull() );

            UpdateScheduleList();
            rblSchedule.SetValue( this.GetBlockUserPreference( UserPreferenceKey.SelectedScheduleId ).AsIntegerOrNull() );

            UpdateGroupLocationList();
            cblGroupLocations.SetValues( this.GetBlockUserPreference( UserPreferenceKey.SelectedGroupLocationIds ).SplitDelimitedValues().AsIntegerList() );

            var resouceListSourceType = this.GetBlockUserPreference( UserPreferenceKey.SelectedResourceListSourceType ).ConvertToEnumOrNull<SchedulerResourceListSourceType>() ?? SchedulerResourceListSourceType.Group;
            bgResourceListSource.SetValue( resouceListSourceType.ConvertToInt() );

            var groupMemberFilterType = this.GetBlockUserPreference( UserPreferenceKey.GroupMemberFilterType ).ConvertToEnumOrNull<SchedulerResourceGroupMemberFilterType>() ?? SchedulerResourceGroupMemberFilterType.ShowMatchingPreference;
            rblGroupMemberFilter.SetValue( groupMemberFilterType.ConvertToInt() );

            gpResourceListAlternateGroup.SetValue( this.GetBlockUserPreference( UserPreferenceKey.AlternateGroupId ).AsIntegerOrNull() );
            dvpResourceListDataView.SetValue( this.GetBlockUserPreference( UserPreferenceKey.DataViewId ).AsIntegerOrNull() );
        }

        /// <summary>
        /// Saves the user preferences and updates the resource list and locations based on the filter
        /// </summary>
        private void ApplyFilter()
        {
            this.SetBlockUserPreference( UserPreferenceKey.SelectedGroupId, hfGroupId.Value );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedDate, ddlWeek.SelectedValue );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedGroupLocationIds, cblGroupLocations.SelectedValues.AsDelimited( "," ) );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedScheduleId, rblSchedule.SelectedValue );

            var resourceListSourceType = bgResourceListSource.SelectedValueAsEnum<SchedulerResourceListSourceType>();
            this.SetBlockUserPreference( UserPreferenceKey.SelectedResourceListSourceType, resourceListSourceType.ToString() );

            var groupMemberFilterType = rblGroupMemberFilter.SelectedValueAsEnum<SchedulerResourceGroupMemberFilterType>();
            this.SetBlockUserPreference( UserPreferenceKey.GroupMemberFilterType, groupMemberFilterType.ToString() );

            this.SetBlockUserPreference( UserPreferenceKey.AlternateGroupId, gpResourceListAlternateGroup.SelectedValue );
            this.SetBlockUserPreference( UserPreferenceKey.DataViewId, dvpResourceListDataView.SelectedValue );

            pnlResourceFilterGroup.Visible = resourceListSourceType == SchedulerResourceListSourceType.Group;
            pnlResourceFilterAlternateGroup.Visible = resourceListSourceType == SchedulerResourceListSourceType.AlternateGroup;
            pnlResourceFilterDataView.Visible = resourceListSourceType == SchedulerResourceListSourceType.DataView;

            InitResourceList();
            BindAttendanceOccurrences();
        }

        /// <summary>
        /// Updates the list of group locations for the selected group
        /// </summary>
        private void UpdateGroupLocationList()
        {
            Group group = GetSelectedGroup();

            if ( group == null )
            {
                pnlScheduler.Visible = false;
                return;
            }

            if ( group != null )
            {
                bool canSchedule = group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) || group.IsAuthorized( Authorization.SCHEDULE, this.CurrentPerson );
                if ( !canSchedule )
                {
                    nbNotice.Heading = "Sorry";
                    nbNotice.Text = "<p>You're not authorized to schedule resources for the selected group.</p>";
                    nbNotice.NotificationBoxType = NotificationBoxType.Warning;
                    nbNotice.Visible = true;
                    pnlScheduler.Visible = false;
                    return;
                }
                else
                {
                    nbNotice.Visible = false;
                }

                pnlScheduler.Visible = true;

                var groupLocations = group.GroupLocations.OrderBy( a => a.Order ).ThenBy( a => a.Location.Name ).ToList();

                // get the selected group location ids just in case any of them are the same after repopulated
                var selectedGroupLocationIds = cblGroupLocations.SelectedValuesAsInt;

                cblGroupLocations.Items.Clear();
                foreach ( var groupLocation in groupLocations )
                {
                    cblGroupLocations.Items.Add( new ListItem( groupLocation.Location.ToString(), groupLocation.Id.ToString() ) );
                }

                cblGroupLocations.SetValues( selectedGroupLocationIds );
            }
        }

        /// <summary>
        /// Set the Resource List hidden fields which groupScheduler.js uses to populate the Resource List
        /// </summary>
        private void InitResourceList()
        {
            int groupId = hfGroupId.Value.AsInteger();
            int? resourceGroupId = null;
            int? resourceDataViewId = null;
            int scheduleId = rblSchedule.SelectedValue.AsInteger();
            hfResourceAdditionalPersonIds.Value = string.Empty;

            var resourceListSourceType = bgResourceListSource.SelectedValueAsEnum<SchedulerResourceListSourceType>();
            switch ( resourceListSourceType )
            {
                case SchedulerResourceListSourceType.Group:
                    {
                        resourceGroupId = hfGroupId.Value.AsInteger();
                        // todo matching vs all
                        break;
                    }
                case SchedulerResourceListSourceType.AlternateGroup:
                    {
                        resourceGroupId = gpResourceListAlternateGroup.SelectedValue.AsInteger();
                        break;
                    }
                case SchedulerResourceListSourceType.DataView:
                    {
                        resourceDataViewId = dvpResourceListDataView.SelectedValue.AsInteger();
                        break;
                    }
            }

            hfOccurrenceGroupId.Value = hfGroupId.Value;
            hfOccurrenceScheduleId.Value = rblSchedule.SelectedValue;
            hfOccurrenceSundayDate.Value = ddlWeek.SelectedValue.AsDateTime().ToISO8601DateString();

            hfResourceGroupId.Value = resourceGroupId.ToString();

            // note, SchedulerResourceGroupMemberFilterType only applies when resourceListSourceType is Group.
            if ( resourceListSourceType == SchedulerResourceListSourceType.Group )
            {
                hfResourceGroupMemberFilterType.Value = rblGroupMemberFilter.SelectedValueAsEnum<SchedulerResourceGroupMemberFilterType>().ConvertToInt().ToString();
            }
            else
            {
                hfResourceGroupMemberFilterType.Value = SchedulerResourceGroupMemberFilterType.ShowAllGroupMembers.ConvertToInt().ToString();
            }

            hfResourceDataViewId.Value = resourceDataViewId.ToString();
            hfResourceAdditionalPersonIds.Value = string.Empty;
        }

        /// <summary>
        /// Binds the Attendance Occurrences ( Which shows the Location for the Attendance Occurrence for the selected Group + DateTime + Location ).
        /// groupScheduler.js will populate these with the assigned resources
        /// </summary>
        private void BindAttendanceOccurrences()
        {
            var occurrenceSundayDate = hfOccurrenceSundayDate.Value.AsDateTime().Value.Date;
            var occurrenceSundayWeekStartDate = occurrenceSundayDate.AddDays( -6 );

            var scheduleId = rblSchedule.SelectedValueAsId();


            var rockContext = new RockContext();
            var occurrenceSchedule = new ScheduleService( rockContext ).GetNoTracking( scheduleId ?? 0 );

            if ( occurrenceSchedule == null )
            {
                btnAutoSchedule.Visible = false;
                return;
            }

            var scheduleOccurrenceDateTime = occurrenceSchedule.GetNextStartDateTime( occurrenceSundayWeekStartDate );


            if ( scheduleOccurrenceDateTime == null )
            {
                btnAutoSchedule.Visible = false;
                return;
            }

            var occurrenceDate = scheduleOccurrenceDateTime.Value.Date;
            btnAutoSchedule.Visible = true;

            
            var attendanceOccurrenceService = new AttendanceOccurrenceService( rockContext );
            var selectedGroupLocationIds = cblGroupLocations.SelectedValuesAsInt;

            var missingAttendanceOccurrences = attendanceOccurrenceService.CreateMissingAttendanceOccurrences( occurrenceDate, scheduleId.Value, selectedGroupLocationIds );
            if ( missingAttendanceOccurrences.Any() )
            {
                attendanceOccurrenceService.AddRange( missingAttendanceOccurrences );
                rockContext.SaveChanges();
            }

            var attendanceOccurrenceGroupLocationScheduleConfigQuery = attendanceOccurrenceService.AttendanceOccurrenceGroupLocationScheduleConfigJoinQuery( occurrenceDate, scheduleId.Value, selectedGroupLocationIds );

            var attendanceOccurrencesOrderedList = attendanceOccurrenceGroupLocationScheduleConfigQuery.AsNoTracking()
                .OrderBy( a => a.GroupLocation.Order ).ThenBy( a => a.GroupLocation.Location.Name )
                .Select( a => new AttendanceOccurrenceRowItem
                {
                    LocationName = a.AttendanceOccurrence.Location.Name,
                    AttendanceOccurrenceId = a.AttendanceOccurrence.Id,
                    CapacityInfo = new CapacityInfo
                    {
                        MinimumCapacity = a.GroupLocationScheduleConfig.MinimumCapacity,
                        DesiredCapacity = a.GroupLocationScheduleConfig.DesiredCapacity,
                        MaximumCapacity = a.GroupLocationScheduleConfig.MaximumCapacity
                    }
                } ).ToList();

            rptAttendanceOccurrences.DataSource = attendanceOccurrencesOrderedList;
            rptAttendanceOccurrences.DataBind();
        }

        /// <summary>
        ///
        /// </summary>
        private class CapacityInfo
        {
            public int? MinimumCapacity { get; set; }
            public int? DesiredCapacity { get; set; }
            public int? MaximumCapacity { get; set; }
        }

        /// <summary>
        ///
        /// </summary>
        private class AttendanceOccurrenceRowItem
        {
            public int AttendanceOccurrenceId { get; set; }
            public CapacityInfo CapacityInfo { get; set; }
            public string LocationName { get; internal set; }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptAttendanceOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptAttendanceOccurrences_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var attendanceOccurrenceRowItem = e.Item.DataItem as AttendanceOccurrenceRowItem;
            var attendanceOccurrenceId = attendanceOccurrenceRowItem.AttendanceOccurrenceId;
            var hfAttendanceOccurrenceId = e.Item.FindControl( "hfAttendanceOccurrenceId" ) as HiddenField;
            var hfLocationScheduleMinimumCapacity = e.Item.FindControl( "hfLocationScheduleMinimumCapacity" ) as HiddenField;
            var hfLocationScheduleDesiredCapacity = e.Item.FindControl( "hfLocationScheduleDesiredCapacity" ) as HiddenField;
            var hfLocationScheduleMaximumCapacity = e.Item.FindControl( "hfLocationScheduleMaximumCapacity" ) as HiddenField;
            var lLocationTitle = e.Item.FindControl( "lLocationTitle" ) as Literal;
            hfAttendanceOccurrenceId.Value = attendanceOccurrenceId.ToString();

            if ( attendanceOccurrenceRowItem.CapacityInfo != null )
            {
                hfLocationScheduleMinimumCapacity.Value = attendanceOccurrenceRowItem.CapacityInfo.MinimumCapacity.ToString();
                hfLocationScheduleDesiredCapacity.Value = attendanceOccurrenceRowItem.CapacityInfo.DesiredCapacity.ToString();
                hfLocationScheduleMaximumCapacity.Value = attendanceOccurrenceRowItem.CapacityInfo.MaximumCapacity.ToString();
            }

            lLocationTitle.Text = attendanceOccurrenceRowItem.LocationName;
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
            //
        }

        /// <summary>
        /// Handles the ValueChanged event of the gpGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gpGroup_ValueChanged( object sender, EventArgs e )
        {
            hfGroupId.Value = gpGroup.SelectedValue.AsIntegerOrNull().ToString();
            UpdateScheduleList();
            UpdateGroupLocationList();
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlWeek control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlWeek_SelectedIndexChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblSchedule control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblSchedule_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateGroupLocationList();
            ApplyFilter();
        }

        protected void cblGroupLocations_SelectedIndexChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the bgResourceListSource control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bgResourceListSource_SelectedIndexChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the ValueChanged event of the gpResourceListAlternateGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        protected void gpResourceListAlternateGroup_ValueChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblGroupMemberFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rblGroupMemberFilter_SelectedIndexChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the ValueChanged event of the dvpResourceListDataView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dvpResourceListDataView_ValueChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the Click event of the btnAutoSchedule control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAutoSchedule_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();

            var groupId = hfGroupId.Value.AsInteger();
            var scheduleId = rblSchedule.SelectedValue.AsInteger();
            var selectedGroupLocationIds = cblGroupLocations.SelectedValuesAsInt;

            var attendanceService = new AttendanceService( rockContext );

            var sundayDate = ddlWeek.SelectedValue.AsDateTime();
            if ( sundayDate.HasValue )
            {

                // NOTE: Partially functional
                attendanceService.SchedulePersonsAutomatically( groupId, sundayDate.Value, this.CurrentPersonAlias );
                rockContext.SaveChanges();
            }

            // NOTE: If SchedulePersonsAutomatically ended up scheduling anybody, they'll now show up in the UI. (JavaScript+REST takes care of populating it)
        }

        #endregion

        /// <summary>
        /// Handles the SelectPerson event of the ppAddResource control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppAddResource_SelectPerson( object sender, EventArgs e )
        {
            var additionPersonIds = hfResourceAdditionalPersonIds.Value.SplitDelimitedValues().AsIntegerList();
            if ( ppAddResource.PersonId.HasValue )
            {
                additionPersonIds.Add( ppAddResource.PersonId.Value );
            }

            hfResourceAdditionalPersonIds.Value = additionPersonIds.AsDelimited( "," );

            // clear on the selected person
            ppAddResource.SetValue( null );
        }
    }
}