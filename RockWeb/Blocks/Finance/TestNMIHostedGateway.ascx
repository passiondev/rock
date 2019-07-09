<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TestNMIHostedGateway.ascx.cs" Inherits="RockWeb.Blocks.Finance.TestNMIHostedGateway" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:RockTextBox ID="txtCreditCard" runat="server" Label="Card Number" MaxLength="19" CssClass="credit-card js-cc-number" Text="4111111111111111111" />
        <Rock:MonthYearPicker ID="mypExpiration" runat="server" Label="Expiration Date" CssClass="js-cc-exp" />
        <Rock:RockTextBox ID="txtCVV" Label="Card Security Code" CssClass="input-width-xs js-cc-cvv" runat="server" MaxLength="4" />

        <Rock:AddressControl ID="acTest" runat="server" />

        <asp:LinkButton ID="btnTest" runat="server" CssClass="btn btn-sm btn-primary" Text="Go" OnClick="btnTest_Click" />

        <Rock:CodeEditor ID="tbResponse" runat="server" EditorMode="Xml" Label="Response" />

        <a onclick="postPaymentInfo()">Click Me</a>

        <Rock:HiddenFieldWithClass ID="hfSendPaymentInfoURL" runat="server" CssClass="js-send-payment-url" />

        <script>
            function postPaymentInfo() {

                debugger
                var ccNumber = $('.js-cc-number').val();
                var ccExp = $('.js-cc-exp').val();
                var ccCvv = $('.js-cc-cvv').val();
                var postUrl = $('.js-send-payment-url').val();


                var $form = $("<form>")
                    .attr("method", "POST")
                    .attr("action", postUrl);

                $form.append($("<input type='hidden' >").attr("name", "billing-cc-number").val(ccNumber));
                $form.append($("<input type='hidden' >").attr("name", "billing-cc-exp").val(ccExp));
                $form.append($("<input type='hidden' >").attr("name", "billing-cvv").val(ccCvv));

                /*$form.append($("<input type='hidden' >").attr("name", "billing-cc-number").val(ccNumber);
                $form.append($("<input type='hidden' >").attr("name", "billing-cc-number").val(ccNumber);
                $form.append($("<input type='hidden' >").attr("name", "billing-cc-number").val(ccNumber);
                $form.append($("<input type='hidden' >").attr("name", "billing-cc-number").val(ccNumber);
                */


                /*
billing-cc-number* Credit card number.
billing-cc-exp* Credit card expiration. Format: MMYY
billing-cvv Card security code.
billing-account-name** The name on the customer's ACH Account.
billing-account-number** The customer's bank account number.
billing-routing-number** The customer's bank routing number.
billing-account-type The customer's ACH account type. Values: 'checking' or 'savings'
billing-entity-type The customer's ACH account entity. Values: 'personal' or 'business'
*/

                var postData = $form.serialize();

                $.post(postUrl, postData).done(function (d) {
                    debugger
                }).fail(function (a, b, c) {
                    debugger
                    //alert("error");
                }).complete(function (a, b, c) {
                    debugger
                });

                //$('body').append($form);
                //$form.submit();

            }


        </script>

    </ContentTemplate>
</asp:UpdatePanel>
