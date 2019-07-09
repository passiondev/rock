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
using System.IO;
using System.Web.UI;
using System.Xml;
using System.Xml.Serialization;
using RestSharp;
using Rock.Attribute;
using Rock.Financial;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Finance
{
    [DisplayName( "Test NMI Hosted Gateway" )]
    [Category( "Finance" )]
    [Description( "Test NMI Hosted Gateway." )]


    [TextField(
        "API Key",
        Key = "APIKey"
        )]
    public partial class TestNMIHostedGateway : RockBlock
    {
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
                // added for your convenience

                // to show the created/modified by date time details in the PanelDrawer do something like this:
                // pdAuditDetails.SetEntity( <YOUROBJECT>, ResolveRockUrl( "~" ) );


                acTest.SetValues( this.CurrentPerson.GetHomeLocation() );
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        #endregion

        #region Methods

        // helper functional methods (like BindGrid(), etc.)

        #endregion

        protected void btnTest_Click( object sender, EventArgs e )
        {
            PaymentInfo paymentInfo = new PaymentInfo();
            paymentInfo.UpdateAddressFieldsFromAddressControl( acTest );
            paymentInfo.FirstName = this.CurrentPerson.FirstName;
            paymentInfo.LastName = this.CurrentPerson.LastName;
            paymentInfo.Email = this.CurrentPerson.Email;


            Rock.NMI.NMITypes.Sale sale = new Rock.NMI.NMITypes.Sale();
            sale.ApiKey = this.GetAttributeValue( "APIKey" );
            sale.IPAddress = GetClientIpAddress();
            sale.Currency = "USD";
            sale.Amount = 12.45M;
            sale.OrderDescription = "Hello Sale";
            sale.TaxAmount = 0.00M;
            sale.ShippingAmount = 0.00M;
            sale.RedirectURL = "http://localhost:6229/GatewayStep2Return.aspx";
            sale.AddCustomer = new Rock.NMI.NMITypes.AddCustomer();
            sale.Billing = new Rock.NMI.NMITypes.BillingAddress();
            sale.Billing.UpdateFromPaymentInfo( paymentInfo );

            XmlSerializer xsSubmit = new XmlSerializer( typeof( Rock.NMI.NMITypes.Sale ) );
            var saleXml = "";

            using ( var sww = new StringWriter() )
            {
                using ( XmlWriter writer = XmlWriter.Create( sww, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true } ) )
                {
                    xsSubmit.Serialize( writer, sale );
                    saleXml = sww.ToString(); // Your XML
                }
            }


            var apiURL = "https://secure.networkmerchants.com/api/v2/three-step";
            var restClient = new RestClient( apiURL );

            var restRequest = new RestRequest( Method.POST );
            restRequest.RequestFormat = DataFormat.Xml;
            restRequest.AddParameter( "text/xml", saleXml, ParameterType.RequestBody );

            var response = restClient.Execute<Rock.NMI.NMITypes.ResponseBase>( restRequest );
            tbResponse.Text = response.Content;

            var saleResponse = response.Data;

            hfSendPaymentInfoURL.Value = saleResponse.FormUrl;


        }
    }
}