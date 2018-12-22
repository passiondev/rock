<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupScheduler.ascx.cs" Inherits="RockWeb.Blocks.Groups.GroupScheduler" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block panel-groupscheduler">
            <%-- Panel Header --%>
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-calendar-alt"></i>
                    Group Scheduler
                </h1>

                <div class="panel-labels">
                    <asp:LinkButton id="btnAutoSchedule" runat="server" CssClass="js-autoschedule btn btn-default btn-xs" OnClick="btnAutoSchedule_Click">
                        <i class="fa fa-magic"></i>
                        Auto Schedule
                    </asp:LinkButton>
                </div>
            </div>

            <%-- Panel Body --%>
            <div class="panel-body">
                <div class="row row-eq-height">
                    <%-- Filter Options --%>
                    <div class="col-lg-2 col-md-3 filter-options">
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

                    <%-- Scheduling: container for the scheduler assignments containers --%>
                    <asp:Panel ID="pnlScheduler" runat="server" CssClass="col-lg-10 col-md-9 resource-area">
                        <div class="row">
                            <div class="col-md-4">

                                <div class="group-scheduler-resourcelist">

                                    <Rock:HiddenFieldWithClass ID="hfOccurrenceGroupId" CssClass="js-occurrence-group-id" runat="server" />
                                    <Rock:HiddenFieldWithClass ID="hfOccurrenceOccurrenceDate" CssClass="js-occurrence-occurrence-date" runat="server" />
                                    <Rock:HiddenFieldWithClass ID="hfOccurrenceScheduleId" CssClass="js-occurrence-schedule-id" runat="server" />
                                    <Rock:HiddenFieldWithClass ID="hfResourceGroupId" CssClass="js-resource-group-id" runat="server" />
                                    <Rock:HiddenFieldWithClass ID="hfResourceGroupMemberFilterType" CssClass="js-resource-groupmemberfiltertype" runat="server" />
                                    <Rock:HiddenFieldWithClass ID="hfResourceDataViewId" CssClass="js-resource-dataview-id" runat="server" />
                                    <Rock:HiddenFieldWithClass ID="hfResourceAdditionalPersonIds" CssClass="js-resource-additional-person-ids" runat="server" />

                                    <div class="js-unassigned-resource-template" style="display: none">
                                        <%-- template that groupScheduler.js uses to populate unassigned resources --%>

                                        <div class="js-resource resource clearfix" data-state="unassigned" data-has-conflict="false" data-is-scheduled="" data-person-id="">
                                            <span class="resource-name js-resource-name"></span>
                                            <div class="resource-note pull-left">
                                                <span class="js-resource-note"></span>
                                            </div>
                                            <div class="resource-warning pull-left">
                                                <span class="js-resource-warning"></span>
                                            </div>
                                            <div class="resource-lastattendeddate pull-right">
                                                <span class="js-resource-lastattendeddate"></span>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="panel panel-block">

                                        <div class="panel-heading">
                                            <h1 class="panel-title">
                                                <i class="fa fa-user"></i>
                                                Resource List
                                            </h1>

                                            <div class="panel-labels">
                                                <button class="btn btn-xs btn-default js-select-all">Select All</button>
                                                <asp:LinkButton ID="btnAddResource" runat="server" CssClass="btn btn-xs btn-default btn-square" OnClick="btnAddResource_Click">
                                                    <i class="fa fa-plus"></i>
                                                </asp:LinkButton>
                                            </div>
                                        </div>

                                        <div class="panel-body padding-all-none">
                                            <Rock:RockTextBox ID="sfResource" runat="server" CssClass="resource-search padding-all-sm" PrependText="<i class='fa fa-search'></i>" Placeholder="Search" />
                                            <!-- <div class="scroll-container scroll-container-resourcelist">
                                                <div class="scrollbar"> -->
                                                    <asp:Panel ID="pnlListTrack" runat="server" CssClass="track">
                                                        <div class="thumb">
                                                            <div class="end"></div>
                                                        </div>
                                                    </asp:Panel>
                                                <!-- </div> -->
                                                <asp:Panel ID="pnlListViewPort" runat="server" CssClass="viewport">
                                                    <div class="overview">

                                                        <%-- loading indicator --%>
                                                        <i class="fa fa-refresh fa-spin margin-l-md js-loading-notification" style="display: none; opacity: .4;"></i>

                                                        <%-- container for list of resources --%>

                                                        <asp:Panel ID="pnlResourceListContainer" CssClass="js-scheduler-source-container resource-container dropzone" runat="server">
                                                        </asp:Panel>
                                                    </div>
                                                </asp:Panel>
                                            <!-- </div> -->

                                        </div>
                                    </div>
                                </div>
                            </div>



                            <div class="col-md-8">

                                <div class="js-assigned-resource-template" style="display: none">
                                    <%-- template that groupScheduler.js uses to populate assigned resources --%>

                                    <div class="meta js-resource resource" data-state="assigned" data-has-conflict="false" data-has-blackout-conflict="false" data-attendance-id="" data-person-id="">
                                        <div class="meta-figure">
                                        </div>
                                        <div class="meta-body">
                                            <div class="flex">

                                                <div class="resource-status pull-left">
                                                    <span class="js-resource-status pull-left resource-scheduled-status" data-status="pending"></span>
                                                </div>
                                                <div class="resource-name pull-left">
                                                    <span class="js-resource-name"></span>
                                                </div>

                                                <div class="dropdown js-resource-actions">
                                                    <button class="btn btn-link btn-overflow" type="button" data-toggle="dropdown"><i class="fas fa-ellipsis-h"></i></button>
                                                    <ul class="dropdown-menu">
                                                        <li>
                                                            <button type="button" class="dropdown-item btn-link js-markconfirmed">Mark Confirmed</button></li>
                                                        <li>
                                                            <button type="button" class="dropdown-item btn-link js-resendconfirmation">Resend Confirmation</button></li>
                                                    </ul>
                                                </div>

                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <%-- containers for AttendanceOccurrence locations that resources can be dragged into --%>
                                <div class="locations js-scheduled-occurrences">
                                    <asp:Repeater ID="rptAttendanceOccurrences" runat="server" OnItemDataBound="rptAttendanceOccurrences_ItemDataBound">
                                        <ItemTemplate>

                                            <div class="location js-scheduled-occurrence">
                                                <Rock:HiddenFieldWithClass ID="hfAttendanceOccurrenceId" runat="server" CssClass="js-attendanceoccurrence-id" />
                                                <Rock:HiddenFieldWithClass ID="hfLocationScheduleMinimumCapacity" runat="server" CssClass="js-minimum-capacity" />
                                                <Rock:HiddenFieldWithClass ID="hfLocationScheduleDesiredCapacity" runat="server" CssClass="js-desired-capacity" />
                                                <Rock:HiddenFieldWithClass ID="hfLocationScheduleMaximumCapacity" runat="server" CssClass="js-maximum-capacity" />
                                                <div class="panel panel-block">
                                                    <div class="panel-heading">
                                                        <h1 class="panel-title">
                                                            <asp:Literal ID="lLocationTitle" runat="server" />
                                                        </h1>
                                                        <div class="panel-labels">
                                                            <div class="scheduling-status js-scheduling-status">

                                                                <div class="scheduling-status-progress">
                                                                    <div class="progress js-scheduling-progress">
                                                                        <div class="progress-bar scheduling-progress-confirmed js-scheduling-progress-confirmed" style="width: 0%">
                                                                            <span class="sr-only"><span class="js-progress-text-percent"></span>% Complete (confirmed)</span>
                                                                        </div>
                                                                        <div class="progress-bar scheduling-progress-pending js-scheduling-progress-pending" style="width: 0%">
                                                                            <span class="sr-only"><span class="js-progress-text-percent"></span>% Complete (pending)</span>
                                                                        </div>
                                                                        <div class="progress-bar scheduling-progress-declined js-scheduling-progress-declined" style="width: 0%">
                                                                            <span class="sr-only"><span class="js-progress-text-percent"></span>% Complete (declined)</span>
                                                                        </div>
                                                                        <div class="minimum-indicator js-minimum-indicator" data-minimum-value="0" style="margin-left: 0%">

                                                                        </div>
                                                                    </div>
                                                                </div>
                                                                <div class="js-scheduling-status-light scheduling-status-light" data-status="none">
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="panel-body">
                                                        <div class="scheduler-target-container js-scheduler-target-container dropzone"></div>
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
