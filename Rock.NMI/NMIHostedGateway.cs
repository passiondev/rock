using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Web.UI;
using Rock.Attribute;
using Rock.Financial;
using Rock.Model;

namespace Rock.NMI
{
    [Description( "NMI Hosted Gateway" )]
    [Export( typeof( GatewayComponent ) )]
    [ExportMetadata( "ComponentName", "NMI Hosted Gateway" )]

    [TextField(
        "Security Key",
        Key = AttributeKey.SecurityKey,
        Description = "The API key",
        IsRequired = true,
        Order = 0
        )]

    [TextField(
        "Admin Username",
        Key = AttributeKey.AdminUsername,
        Description = "The username of an NMI user",
        IsRequired = true,
        Order = 1 )]


    [TextField( "Admin Password",
        Key = AttributeKey.AdminPassword,
        Description = "The password of the NMI user",
        IsRequired = true,
        IsPassword = true,
        Order = 2 )]

    [TextField(
        "Three Step API URL",
        Key = AttributeKey.APIUrl,
        Description = "The URL of the NMI Three Step API",
        IsRequired = true,
        DefaultValue = "https://secure.networkmerchants.com/api/v2/three-step",
        Order = 3 )]

    [TextField(
        "Query API URL",
        Key = AttributeKey.QueryUrl,
        Description = "The URL of the NMI Query API",
        IsRequired = true,
        DefaultValue = "https://secure.networkmerchants.com/api/query.php",
        Order = 4 )]

    [BooleanField(
        "Prompt for Name On Card",
        Key = AttributeKey.PromptForName,
        Description = "Should users be prompted to enter name on the card",
        DefaultBooleanValue = false,
        Order = 5 )]

    [BooleanField(
        "Prompt for Billing Address",
        Key = AttributeKey.PromptForAddress,
        Description = "Should users be prompted to enter billing address",
        DefaultBooleanValue = false,
        Order = 6 )]

    public class NMIHostedGateway : GatewayComponent, IHostedGatewayComponent
    {
        #region Attribute Keys

        /// <summary>;
        /// Keys to use for Component Attributes
        /// </summary>
        protected static class AttributeKey
        {
            public const string SecurityKey = "SecurityKey";
            public const string AdminUsername = "AdminUsername";
            public const string AdminPassword = "AdminPassword";
            public const string APIUrl = "APIUrl";
            public const string QueryUrl = "QueryUrl";
            public const string PromptForName = "PromptForName";
            public const string PromptForAddress = "PromptForAddress";
        }

        #endregion Attribute Keys


        #region IHostedGatewayComponent

        public Control GetHostedPaymentInfoControl( FinancialGateway financialGateway, string controlId, HostedPaymentInfoControlOptions options )
        {
            throw new NotImplementedException();
        }

        public string GetHostPaymentInfoSubmitScript( FinancialGateway financialGateway, System.Web.UI.Control hostedPaymentInfoControl )
        {
            throw new NotImplementedException();
        }

        public void UpdatePaymentInfoFromPaymentControl( FinancialGateway financialGateway, System.Web.UI.Control hostedPaymentInfoControl, ReferencePaymentInfo referencePaymentInfo, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public string ConfigureURL => throw new NotImplementedException();

        public string LearnMoreURL => throw new NotImplementedException();

        public string CreateCustomerAccount( FinancialGateway financialGateway, ReferencePaymentInfo paymentInfo, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public DateTime GetEarliestScheduledStartDate( FinancialGateway financialGateway )
        {
            throw new NotImplementedException();
        }


        #endregion IHostedGatewayComponent

        #region IGatewayComponent

        public override FinancialScheduledTransaction AddScheduledPayment( FinancialGateway financialGateway, PaymentSchedule schedule, PaymentInfo paymentInfo, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override bool CancelScheduledPayment( FinancialScheduledTransaction transaction, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override FinancialTransaction Charge( FinancialGateway financialGateway, PaymentInfo paymentInfo, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override FinancialTransaction Credit( FinancialTransaction origTransaction, decimal amount, string comment, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override List<Payment> GetPayments( FinancialGateway financialGateway, DateTime startDate, DateTime endDate, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override string GetReferenceNumber( FinancialTransaction transaction, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override string GetReferenceNumber( FinancialScheduledTransaction scheduledTransaction, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override bool GetScheduledPaymentStatus( FinancialScheduledTransaction transaction, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override bool ReactivateScheduledPayment( FinancialScheduledTransaction transaction, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        public override bool UpdateScheduledPayment( FinancialScheduledTransaction transaction, PaymentInfo paymentInfo, out string errorMessage )
        {
            throw new NotImplementedException();
        }

        #endregion IGatewayComponent
    }
}
