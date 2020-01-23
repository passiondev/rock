﻿// <copyright>
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
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Communication
{
    /// <summary>
    /// User control for managing the system emails
    /// </summary>
    [DisplayName( "Sms Pipeline List" )]
    [Category( "Communication" )]
    [Description( "Lists the sms pipelines currently in the system." )]

    [LinkedPage( "Detail Page" )]
    public partial class SmsPipelineList : RockBlock, ICustomGridColumns
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( IsUserAuthorized( Authorization.ADMINISTRATE ) )
            {
                gSmsPipelines.DataKeyNames = new string[] { "Id" };
                gSmsPipelines.Actions.ShowAdd = true;
                gSmsPipelines.Actions.AddClick += gSmsPipeline_AddClick;
                gSmsPipelines.GridRebind += gSmsPipelines_GridRebind;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            nbMessage.Visible = false;

            if ( IsUserAuthorized( Authorization.ADMINISTRATE ) )
            {
                if ( !Page.IsPostBack )
                {
                    BindGrid();
                }
            }
            else
            {
                gSmsPipelines.Visible = false;
                nbMessage.Text = WarningMessage.NotAuthorizedToEdit( SystemEmail.FriendlyTypeName );
                nbMessage.Visible = true;
            }

            base.OnLoad( e );
        }

        #endregion

        #region Grid Events

        /// <summary>
        /// Handles the AddClick event of the gSmsPipelines control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gSmsPipeline_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "smsPipelineId", 0 );
        }

        /// <summary>
        /// Handles the Edit event of the gSmsPipelines control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gSmsPipeline_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "smsPipelineId", e.RowKeyId );
        }

        /// <summary>
        /// Handles the Delete event of the gSmsPipeline control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gSmsPipelines_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            var smsPipelineService = new SmsPipelineService( rockContext );
            var smsPipeline = smsPipelineService.Get( e.RowKeyId );
            if (smsPipeline != null )
            {
                smsPipelineService.Delete( smsPipeline );
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gEmailTemplates control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gSmsPipelines_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            var smsPipelineService = new SmsPipelineService( new RockContext() );
            SortProperty sortProperty = gSmsPipelines.SortProperty;

            var smsPipelines = smsPipelineService.Queryable();

            if ( sortProperty != null )
            {
                gSmsPipelines.DataSource = smsPipelines.Sort( sortProperty ).ToList();
            }
            else
            {
                gSmsPipelines.DataSource = smsPipelines.OrderBy( a => a.Name ).ToList();
            }

            gSmsPipelines.EntityTypeId = EntityTypeCache.Get<Rock.Model.SmsPipeline>().Id;
            gSmsPipelines.DataBind();
        }

        #endregion
    }
}