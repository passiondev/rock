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

                <Rock:PersonPicker ID="ppSelectedPerson" runat="server" OnSelectPerson="ppSelectedPerson_SelectPerson" Label="DEBUG SelectPerson" EnableSelfSelection="true" />

                <div class="margin-b-md">
                    <Rock:ButtonGroup ID="bgTabs" runat="server" SelectedItemClass="btn btn-primary active" UnselectedItemClass="btn btn-default" AutoPostBack="true" OnSelectedIndexChanged="bgTabs_SelectedIndexChanged" />
                </div>

                <%-- My Schedule --%>
                <asp:Panel ID="pnlMySchedule" runat="server">
                    <%-- Pending Confirmations Grid --%>
                    <div>
                        <span class="control-label">
                            <asp:Literal runat="server" ID="lPendingConfirmations" Text="Pending Confirmations" />
                        </span>
                        <hr class="margin-t-sm margin-b-sm" />
                        <asp:Repeater ID="rptPendingConfirmations" runat="server" OnItemDataBound="rptPendingConfirmations_ItemDataBound">
                            <ItemTemplate>
                                <div class="row">
                                    <div class="col-md-6">
                                        <asp:Literal ID="lPendingOccurrenceDetails" runat="server" />
                                    </div>
                                    <div class="col-md-3">
                                        <asp:Literal ID="lPendingOccurrenceTime" runat="server" />

                                    </div>
                                    <div class="col-md-3">
                                        <div class="actions">
                                            <asp:LinkButton ID="btnConfirmAttending" runat="server" CssClass="btn btn-xs btn-success" Text="Attending" OnClick="btnConfirmAttending_Click" />
                                            <asp:LinkButton ID="btnDeclineAttending" runat="server" CssClass="btn btn-xs btn-danger" Text="Decline" OnClick="btnDeclineAttending_Click" />
                                        </div>
                                    </div>
                                </div>
                            </ItemTemplate>

                        </asp:Repeater>
                    </div>

                    <%-- Upcoming Schedules Grid --%>
                    <div class="margin-t-md">
                        <span class="control-label">
                            <asp:Literal runat="server" ID="lUpcomingSchedules" Text="Upcoming Schedules" />
                        </span>
                        <hr class="margin-t-sm margin-b-sm" />
                        <asp:Repeater ID="rptUpcomingSchedules" runat="server" OnItemDataBound="rptUpcomingSchedules_ItemDataBound">
                            <ItemTemplate>
                                <div class="row">
                                    <div class="col-md-6">
                                        <asp:Literal ID="lConfirmedOccurrenceDetails" runat="server" />
                                    </div>
                                    <div class="col-md-3">
                                        <asp:Literal ID="lConfirmedOccurrenceTime" runat="server" />
                                    </div>
                                    <div class="col-md-3">
                                        <asp:LinkButton ID="btnCancelConfirmAttending" runat="server" CssClass="btn btn-xs btn-link" Text="Cancel Confirmation" OnClick="btnCancelConfirmAttending_Click" />
                                    </div>
                                </div>

                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </asp:Panel>

                <%-- Preferences --%>
                <asp:Panel ID="pnlPreferences" runat="server">
                    <div class="row">
                        <div class="col-md-6">
                            <%-- Reminder Preferences --%>
                            <span class="control-label">
                                <asp:Literal runat="server" ID="lReminderPreferences" Text="Reminder Preferences" />
                            </span>
                            <hr class="margin-t-sm margin-b-sm" />
                            <Rock:RockDropDownList ID="ddlSendRemindersDaysOffset" runat="server" Label="Send Reminders" />

                            <%-- Per Group Preferences --%>
                            <asp:Repeater ID="rptGroupPreferences" runat="server" OnItemDataBound="rptGroupPreferences_ItemDataBound">
                                <ItemTemplate>
                                    <span class="control-label">
                                        <asp:Literal runat="server" ID="lGroupPreferencesGroupName" Text="##GROUPNAME##" />
                                    </span>
                                    <hr class="margin-t-sm margin-b-sm" />

                                    <Rock:RockDropDownList ID="ddlGroupMemberScheduleTemplate" runat="server" Label="Current Schedule" OnSelectedIndexChanged="ddlGroupMemberScheduleTemplate_SelectedIndexChanged" />

                                    <span class="control-label">
                                        <asp:Literal runat="server" ID="lGroupPreferenceAssignmentLabel" Text="Assignment" />
                                    </span>
                                    <span class="control-label">
                                        <asp:Literal runat="server" ID="lGroupPreferenceAssignmentHelp" Text="Please select a time and optional location that you would like to be scheduled for." />
                                    </span>

                                    <asp:Repeater ID="rptGroupPreferenceAssignments" runat="server" OnItemDataBound="rptGroupPreferenceAssignments_ItemDataBound">
                                        <ItemTemplate>
                                            <div class="row">
                                                <div class="col-md-4">
                                                    <asp:CheckBox ID="cbGroupPreferenceAssignmentScheduleTime" runat="server" Text="##TODO## time" AutoPostBack="true" OnCheckedChanged="cbGroupPreferenceAssignmentScheduleTime_CheckedChanged" />
                                                </div>
                                                <div class="col-md-8">
                                                    <asp:DropDownList ID="ddlGroupPreferenceAssignmentLocation" runat="server" />
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>

                                </ItemTemplate>
                            </asp:Repeater>
                        </div>

                        <div class="col-md-6">
                            <%-- Blackout Dates --%>
                            <span class="control-label">
                                <asp:Literal runat="server" ID="lBlackoutDates" Text="Blackout Dates" />
                            </span>
                            <hr class="margin-t-sm margin-b-sm" />
                            <span class="control-label">
                                <asp:Literal runat="server" ID="lBlackoutDatesHelp" Text="Please provide any dates you will not be able to make." />
                            </span>

                            <Rock:Grid ID="gBlackoutDates" runat="server" DisplayType="Light" ShowHeader="false" >
                                <Columns>

                                </Columns>
                            </Rock:Grid>
                        </div>
                    </div>
                </asp:Panel>

                <%-- Sign-up --%>
                <asp:Panel ID="pnlSignup" runat="server">
                    <asp:PlaceHolder ID="phSignUpSchedules" runat="server"></asp:PlaceHolder>
                    <Rock:BootstrapButton ID="btnSaveSignups" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSaveSignups_Click"></Rock:BootstrapButton>
                </asp:Panel>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
