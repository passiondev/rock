<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupVolunteerScheduleStatusBoard.ascx.cs" Inherits="GroupVolunteerScheduleStatusBoard" %>

<asp:UpdatePanel ID="upScheduleStatusBoard" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block panel-groupscheduler">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-calendar"></i>Schedule Status Board
                </h1>
                <div class="panel-labels">
                    <asp:LinkButton ID="btnGroups" runat="server" CssClass="btn btn-default btn-xs" OnClick="btnGroups_Click">
                        <i class="fa fa-users"></i>
                        Groups
                    </asp:LinkButton>
                    <asp:LinkButton ID="btnDates" runat="server" CssClass="btn btn-default btn-xs" OnClick="btnDates_Click">
                        <i class="fa fa-calendar"></i>
                        Dates
                    </asp:LinkButton>
                </div>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Info" />
                <div>
                    <asp:Literal ID="ltContentPlaceholer" runat="server" />
               </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
