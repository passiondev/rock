<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupScheduler.ascx.cs" Inherits="RockWeb.Blocks.Groups.GroupScheduler" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <%-- Panel Header --%>
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-calendar-alt"></i>
                    Group Scheduler
                </h1>

                <div class="panel-labels">
                    <asp:LinkButton ID="btnRecompileLess" runat="server" CssClass="btn btn-default btn-xs" OnClick="btnRecompileLess_Click" Text="RECOMPILE LESS" Style="background-color: violet;" />
                </div>
            </div>

            <%-- Panel Body --%>
            <div class="panel-body panel-groupscheduler">
                <div class="row">
                    <%-- Filter Options --%>
                    <div class="col-md-3 filter-options">
                        <asp:HiddenField ID="hfGroupId" runat="server" />
                        <Rock:GroupPicker ID="gpGroup" runat="server" Label="Group" LimitToSchedulingEnabledGroups="true" OnValueChanged="gpGroup_ValueChanged" />
                        <Rock:DatePicker ID="dpDate" runat="server" Label="Date" AllowPastDateSelection="true" AllowFutureDateSelection="true" OnValueChanged="dpDate_ValueChanged" />

                        <Rock:NotificationBox ID="nbGroupWarning" runat="server" NotificationBoxType="Warning" />
                        <asp:Panel ID="pnlGroupScheduleLocations" runat="server">
                            <Rock:RockRadioButtonList ID="rblSchedule" runat="server" Label="Schedule" AutoPostBack="true" OnSelectedIndexChanged="cblSchedule_SelectedIndexChanged" />
                            <Rock:RockCheckBoxList ID="cblGroupLocations" runat="server" Label="Locations" AutoPostBack="true" OnSelectedIndexChanged="cblGroupLocations_SelectedIndexChanged" />
                        </asp:Panel>

                        <Rock:RockControlWrapper ID="rcwResourceListSource" runat="server" Label="Resource List Source">
                            <Rock:ButtonGroup ID="bgResourceListSource" runat="server" CssClass="margin-b-md" SelectedItemClass="btn btn-xs btn-primary" UnselectedItemClass="btn btn-xs btn-default" AutoPostBack="true" OnSelectedIndexChanged="bgResourceListSource_SelectedIndexChanged" />

                            <asp:Panel ID="pnlResourceFilterGroup" runat="server">
                                <Rock:RockRadioButtonList ID="rblGroupMemberFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="rblGroupMemberFilter_SelectedIndexChanged" />
                            </asp:Panel>

                            <asp:Panel ID="pnlResourceFilterAlternateGroup" runat="server">
                                <Rock:GroupPicker ID="gpResourceListAlternateGroup" runat="server" Label="Alternate Group" OnValueChanged="gpResourceListAlternateGroup_ValueChanged" />
                            </asp:Panel>

                            <asp:Panel ID="pnlResourceFilterDataView" runat="server">
                                <Rock:DataViewItemPicker ID="dvpResourceListDataView" runat="server" Label="Data View" EntityTypeId="15" OnValueChanged="dvpResourceListDataView_ValueChanged" />
                            </asp:Panel>
                        </Rock:RockControlWrapper>
                    </div>

                    <%-- Scheduling --%>
                    <div class="col-md-9">
                        <%-- container for the scheduler assignments containers --%>
                        <asp:Panel ID="pnlScheduler" runat="server">
                            <div class="row">
                                <div class="col-md-4">
                                    <div class="group-scheduler-resourcelist">
                                        <div class="panel panel-block">

                                            <div class="panel-heading">
                                                <h1 class="panel-title">
                                                    <i class="fa fa-user"></i>
                                                    Resource List
                                                </h1>

                                                <div class="panel-labels">
                                                    <asp:LinkButton ID="btnSelectAllResource" runat="server" CssClass="btn btn-xs btn-default" Text="Select All" OnClick="btnSelectAllResource_Click" />
                                                    <asp:LinkButton ID="btnAddResource" runat="server" CssClass="btn btn-xs btn-default" OnClick="btnAddResource_Click">
                                                <i class="fa fa-plus"></i>
                                                    </asp:LinkButton>
                                                </div>
                                            </div>

                                            <div class="panel-body">
                                                <Rock:RockTextBox ID="sfResource" runat="server" CssClass="margin-b-md" PrependText="<i class='fa fa-search'></i>" Placeholder="Search" />
                                                <div class="scroll-container scroll-container-resourcelist">
                                                    <div class="scrollbar">
                                                        <asp:Panel ID="pnlListTrack" runat="server" CssClass="track">
                                                            <div class="thumb">
                                                                <div class="end"></div>
                                                            </div>
                                                        </asp:Panel>
                                                    </div>
                                                    <asp:Panel ID="pnlListViewPort" runat="server" CssClass="js-resource-scroller viewport">
                                                        <div class="overview">
                                                            <%-- Dragula container for list of resources --%>
                                                            <asp:Panel ID="pnlResourceListContainer" CssClass="js-scheduler-source-container resource-container dropzone" runat="server">
                                                                <asp:Repeater ID="rptResources" runat="server" OnItemDataBound="rptResources_ItemDataBound">
                                                                    <ItemTemplate>
                                                                        <%-- resource --%>
                                                                        <div class="js-resource resource" data-state="unassigned">
                                                                            <div class="resource-row">
                                                                                <div class="js-resource-status resource-status pull-left">
                                                                                    <span class="pull-left resource-scheduled-status"></span>
                                                                                </div>
                                                                                <div class="js-resource-name resource-name pull-left">
                                                                                    <Rock:HiddenFieldWithClass ID="hfResourcePersonId" CssClass="js-resource-personId" runat="server" />
                                                                                    <asp:HiddenField ID="hfResourceGroupMemberId" runat="server" />
                                                                                    <asp:Literal ID="lPersonName" runat="server" />
                                                                                </div>

                                                                                <div class="js-resource-lastscheduleddate resource-lastattendeddate pull-right">
                                                                                    <asp:Literal ID="lResourceLastAttendedDate" runat="server" />
                                                                                </div>
                                                                            </div>
                                                                            <div class="resource-row">
                                                                                <div class="js-resource-warning resource-warning pull-left">
                                                                                    <asp:Literal ID="lResourceWarning" runat="server" />
                                                                                </div>
                                                                                <div class="js-resource-note resource-note pull-left">
                                                                                    <asp:Literal ID="lResourceNote" runat="server" />
                                                                                </div>
                                                                            </div>
                                                                        </div>
                                                                    </ItemTemplate>
                                                                </asp:Repeater>
                                                            </asp:Panel>
                                                        </div>
                                                    </asp:Panel>
                                                </div>

                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-8">
                                    
                                    <%-- template that groupScheduler.js uses to populate assigned resources --%>
                                    <div class="js-assigned-resource-template" style="display:none">
                                        <div class="js-resource resource" data-state="assigned">
                                            <div class="resource-row">
                                                <div class="resource-status pull-left">
                                                    <span class="js-resource-status pull-left resource-scheduled-status" data-status="pending"></span>
                                                </div>
                                                <div class="js-resource-name resource-name pull-left">
                                                    <span class="js-resource-name" ></span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <%-- Dragula containers for AttendanceOccurrence locations that resources can be dragged into --%>
                                    <div class="locations js-locations">
                                        <asp:Repeater ID="rptAttendanceOccurrences" runat="server" OnItemDataBound="rptAttendanceOccurrences_ItemDataBound">
                                            <ItemTemplate>

                                                <div class="location js-location">
                                                    <Rock:HiddenFieldWithClass ID="hfAttendanceOccurrenceId" runat="server" CssClass="js-attendanceoccurrence-id" />
                                                    <div class="panel panel-block">
                                                        <div class="panel-heading">
                                                            <h1 class="panel-title">
                                                                <asp:Literal ID="lLocationTitle" runat="server" />
                                                            </h1>
                                                        </div>
                                                        <div class="panel-body">
                                                            <div class="js-scheduler-target-container dropzone"></div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                </div>
                            </div>
                        </asp:Panel>
                    </div>
                </div>
            </div>

        </asp:Panel>
        <script>
            Sys.Application.add_load(function () {
                var schedulerContainerId = '<%=pnlScheduler.ClientID%>';

                Rock.controls.groupScheduler.initialize({
                    id: schedulerContainerId,
                });
            });
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
