<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupScheduleToolbox.ascx.cs" Inherits="RockWeb.Blocks.Groups.GroupScheduleToolbox" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfSelectedPersonId" runat="server" />
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-calendar"></i>
                    Schedule Toolbox
                </h1>

                <div class="panel-labels">
                </div>
            </div>
            <div class="panel-body">
                <Rock:PersonPicker ID="ppSelectedPerson" runat="server" OnSelectPerson="ppSelectedPerson_SelectPerson" EnableSelfSelection="true" />

                <div class="margin-b-md">
                    <Rock:ButtonGroup ID="bgTabs" runat="server" SelectedItemClass="btn btn-primary active" UnselectedItemClass="btn btn-default" AutoPostBack="true" OnSelectedIndexChanged="bgTabs_SelectedIndexChanged" />
                </div>

                <%-- My Schedule --%>
                <asp:Panel ID="pnlMySchedule" runat="server">
                    <Rock:RockControlWrapper ID="rcwPendingConfirmations" runat="server" Label="Pending Confirmations">
                        <Rock:Grid ID="gPendingConfirmations" runat="server">
                        </Rock:Grid>
                    </Rock:RockControlWrapper>

                    <Rock:RockControlWrapper ID="rcwUpcomingSchedules" runat="server" Label="Upcoming Schedules">
                        <Rock:Grid ID="gUpcomingSchedules" runat="server">
                        </Rock:Grid>
                    </Rock:RockControlWrapper>

                </asp:Panel>

                <%-- Preferences --%>
                <asp:Panel ID="pnlPreferences" runat="server">
                </asp:Panel>

                <%-- Sign-up --%>
                <asp:Panel ID="pnlSignup" runat="server">
                </asp:Panel>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
