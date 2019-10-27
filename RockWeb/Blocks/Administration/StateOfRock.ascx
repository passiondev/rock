<%@ Control Language="C#" AutoEventWireup="true" CodeFile="StateOfRock.ascx.cs" Inherits="RockWeb.Blocks.Administration.StateOfRock" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-flag"></i>State Of Rock</h1>
            </div>
            <div class="panel-body">

                <Rock:NotificationBox ID="nbMessage" runat="server"
                    Visible="<%# ViewModel.SurveyDataWarningIsVisible %>"
                    NotificationBoxType="Warning"
                    Text="Only aggregate data is sent to the Spark server for collecting information about feature usage and data patterns. No dollar amounts are collected. For a complete list of what's sent please see our State of Rock details page.">
                </Rock:NotificationBox>
                <Rock:NotificationBox ID="nbValidation" runat="server"
                    Visible="<%# ViewModel.ValidationMessagePanelIsVisible %>"
                    NotificationBoxType="<%# ViewModel.ValidationNotification.NotificationType %>"
                    Heading="<%# ViewModel.ValidationNotification.Title %>"
                    Text="<%# ViewModel.ValidationNotification.Message %>">
                </Rock:NotificationBox>

                <asp:Panel ID="pnlInputForm" runat="server" class="form-group" DefaultButton="bbtnSendData" Visible="<%# ViewModel.SendPanelIsVisible %>">
                    <fieldset>
                        <div class="row">
                            <div class="col-md-3">
                                <Rock:RockTextBox ID="tbReportedBy" runat="server" Label="Reported By" ReadOnly="true" Text="<%# ViewModel.ReportedBy %>"></Rock:RockTextBox>
                                <Rock:NumberBox ID="tbAttendance" runat="server" Label="Average Weekend Attendance" Text="<%# ViewModel.AverageWeeklyAttendance %>" Help="We use your average weekend attendance number to help compare your statistics to other organizations. It will not be shared with other organizations."></Rock:NumberBox>
                            </div>
                    </fieldset>
                    <Rock:BootstrapButton ID="bbtnSendData" runat="server" CssClass="btn btn-primary" OnClick="bbtnSendData_Click" Text="Send" DataLoadingText="Sending..."></Rock:BootstrapButton>
                </asp:Panel>

                <asp:Panel ID="pnlComplete" runat="server" class="form-group" Visible="<%# ViewModel.CompletedPanelIsVisible %>">
                    <Rock:NotificationBox ID="nbCompletionMessage" runat="server" Visible="true"
                        NotificationBoxType="<%# ViewModel.CompletedNotification.NotificationType %>"
                        Heading="<%# ViewModel.CompletedNotification.Title %>"
                        Text="<%# ViewModel.CompletedNotification.Message %>">
                    </Rock:NotificationBox>
                    <Rock:BootstrapButton ID="btnResend" runat="server" CssClass="btn" OnClick="bbtnResend_Click" Text="Resend" DataLoadingText="Loading..."></Rock:BootstrapButton>
                </asp:Panel>
            </div>
        </div>
        
    </ContentTemplate>
</asp:UpdatePanel>
