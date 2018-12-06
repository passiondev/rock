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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Groups
{
    /// <summary>
    ///
    /// </summary>
    [DisplayName( "Group Scheduler" )]
    [Category( "Groups" )]
    [Description( "Allows volunteer schedules for groups and locations to be managed by a scheduler." )]
    public partial class GroupScheduler : RockBlock
    {
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
            /// The selected location ids
            /// </summary>
            public const string SelectedLocationIds = "SelectedLocationIds";

            /// <summary>
            /// The selected resource list source type
            /// </summary>
            public const string SelectedResourceListSourceType = "SelectedResourceListSourceType";
        }

        #endregion UserPreferanceKeys

        #region enums

        /// <summary>
        /// 
        /// </summary>
        private enum ResourceListSourceType
        {
            Group,
            AlternateGroup,
            DataView
        }

        #endregion

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
            bgResourceListSource.BindToEnum<ResourceListSourceType>();
        }

        /// <summary>
        /// Updates the list of schedules for the selected group
        /// </summary>
        private void UpdateSchedules()
        {
            Group group = GetSelectedGroup();

            nbGroupWarning.Visible = false;
            pnlGroupScheduleLocations.Visible = false;

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
            else
            {
                nbGroupWarning.Text = "Please select a group";
                nbGroupWarning.Visible = true;
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
            dpDate.SelectedDate = this.GetBlockUserPreference( UserPreferenceKey.SelectedDate ).AsDateTime();
            hfGroupId.Value = this.GetBlockUserPreference( UserPreferenceKey.SelectedGroupId );
            gpGroup.SetValue( hfGroupId.Value.AsIntegerOrNull() );

            UpdateSchedules();
            rblSchedule.SetValue( this.GetBlockUserPreference( UserPreferenceKey.SelectedScheduleId ).AsIntegerOrNull() );

            UpdateLocations();
            cblLocations.SetValues( this.GetBlockUserPreference( UserPreferenceKey.SelectedLocationIds ).SplitDelimitedValues().AsIntegerList() );

            var resouceListSourceType = this.GetBlockUserPreference( UserPreferenceKey.SelectedResourceListSourceType ).ConvertToEnumOrNull<ResourceListSourceType>() ?? ResourceListSourceType.Group;
            bgResourceListSource.SetValue( resouceListSourceType.ConvertToInt() );
        }

        /// <summary>
        /// Saves the user preferences and updates the resource list and locations based on the filter
        /// </summary>
        private void ApplyFilter()
        {
            this.SetBlockUserPreference( UserPreferenceKey.SelectedGroupId, hfGroupId.Value );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedDate, dpDate.SelectedDate.ToISO8601DateString() );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedLocationIds, cblLocations.SelectedValues.AsDelimited(",") );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedScheduleId, rblSchedule.SelectedValue );

            var resourceListSourceType = bgResourceListSource.SelectedValueAsEnum<ResourceListSourceType>();
            this.SetBlockUserPreference( UserPreferenceKey.SelectedResourceListSourceType, resourceListSourceType.ToString() );

            pnlResourceFilterGroup.Visible = resourceListSourceType == ResourceListSourceType.Group;
            pnlResourceFilterAlternateGroup.Visible = resourceListSourceType == ResourceListSourceType.AlternateGroup;
            pnlResourceFilterDataView.Visible = resourceListSourceType == ResourceListSourceType.DataView;
        }

        /// <summary>
        /// Updates the list of locations for the selected group
        /// </summary>
        private void UpdateLocations()
        {
            Group group = GetSelectedGroup();

            if ( group != null )
            {
                var groupLocations = group.GroupLocations.ToList();

                // get the selected location ids just in case any of them are the same after repopulated
                var selectedLocationIds = cblLocations.SelectedValuesAsInt;

                cblLocations.Items.Clear();
                foreach ( var groupLocation in groupLocations )
                {
                    cblLocations.Items.Add( new ListItem( groupLocation.Location.ToString(), groupLocation.Id.ToString() ) );
                }

                cblLocations.SetValues( selectedLocationIds );
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
            UpdateSchedules();
            UpdateLocations();
            ApplyFilter();
        }

        /// <summary>
        /// Handles the ValueChanged event of the dpDate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dpDate_ValueChanged( object sender, EventArgs e )
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
            UpdateLocations();
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblLocations_SelectedIndexChanged( object sender, EventArgs e )
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

        protected void gpAlternateGroup_ValueChanged( object sender, EventArgs e )
        {
            // TODO
        }

        protected void rblGroupMemberFilter_SelectedIndexChanged( object sender, EventArgs e )
        {
            // TODO
        }

        protected void dvpResourceListDataView_ValueChanged( object sender, EventArgs e )
        {
            // TODO
        }

        #endregion

        protected void btnSelectAllResource_Click( object sender, EventArgs e )
        {

        }

        protected void btnAddResource_Click( object sender, EventArgs e )
        {

        }
    }
}