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
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Groups
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Group Scheduler Analytics" )]
    [Category( "Groups" )]
    [Description( "Provides some visibility into scheduling accountability. Shows check-ins, missed confirmations, declines, and decline reasons with ability to filter by group, date range, data view, and person." )]
    public partial class GroupSchedulerAnalytics : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // NOTE: moment.js needs to be loaded before chartjs
            RockPage.AddScriptLink( "~/Scripts/moment.min.js", true );
            RockPage.AddScriptLink( "~/Scripts/Chartjs/Chart.js", true );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            //this.BlockUpdated += Block_BlockUpdated;
            //this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        
            if( !Page.IsPostBack )
            {
                LoadLocations();
                LoadSchedules();
                ShowBarGraph();
                ShowDoughnutGraph();
                ShowTable();
            }
        }

        /// <summary>
        /// Populates the locations checkbox list
        /// </summary>
        protected void LoadLocations()
        {
            using ( var rockContext = new RockContext() )
            {
                var locationService = new LocationService( rockContext );
                var locations = locationService
                    .Queryable()
                    .Where( l => l.IsActive == true )
                    //.Select( l => new { l.Id, l.Name } )
                    .OrderBy( l => l.Name ).ToList();

                cblLocations.DataSource = locations;
                cblLocations.DataBind();
            }
        }

        /// <summary>
        /// Populates the schedules checkbox list
        /// </summary>
        protected void LoadSchedules()
        {
            using ( var rockContext = new RockContext() )
            {

            }
        }

        protected void ShowBarGraph()
        {

        }

        protected void ShowDoughnutGraph()
        {

        }

        protected void ShowTable()
        {

        }

    }
}