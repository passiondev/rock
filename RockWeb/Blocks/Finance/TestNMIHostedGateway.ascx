<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TestNMIHostedGateway.ascx.cs" Inherits="RockWeb.Blocks.Finance.TestNMIHostedGateway" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:RockTextBox ID="txtCreditCard" runat="server" Label="Card Number" MaxLength="19" CssClass="credit-card" />
        <Rock:MonthYearPicker ID="mypExpiration" runat="server" Label="Expiration Date" />
        <Rock:RockTextBox ID="txtCVV" Label="Card Security Code" CssClass="input-width-xs" runat="server" MaxLength="4" />

        <Rock:AddressControl ID="acTest" runat="server" />

        <asp:LinkButton ID="btnTest" runat="server" CssClass="btn btn-sm btn-primary" Text="Go" OnClick="btnTest_Click" />

        <Rock:CodeEditor ID="tbResponse" runat="server" EditorMode="Xml" Label="Response" />

    </ContentTemplate>
</asp:UpdatePanel>
