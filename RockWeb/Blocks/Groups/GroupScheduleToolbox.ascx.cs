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
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Groups
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Group Schedule Toolbox" )]
    [Category( "Groups" )]
    [Description( "Allows management of group scheduling for a specific person (worker)." )]
    public partial class GroupScheduleToolbox : RockBlock
    {
        #region Enum

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

        #endregion Enum

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

            this.Page.Trace.IsEnabled = true;
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
                ShowDetail();
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

        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail()
        {
            LoadDropDowns();
            ShowSelectedTab();
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

        
    }
}