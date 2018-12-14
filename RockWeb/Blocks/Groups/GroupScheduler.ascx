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

                                        <Rock:HiddenFieldWithClass ID="hfOccurrenceGroupId" CssClass="js-occurrence-group-id" runat="server" />
                                        <Rock:HiddenFieldWithClass ID="hfOccurrenceOccurrenceDate" CssClass="js-occurrence-occurrence-date" runat="server" />
                                        <Rock:HiddenFieldWithClass ID="hfOccurrenceScheduleId" CssClass="js-occurrence-schedule-id" runat="server" />
                                        <Rock:HiddenFieldWithClass ID="hfResourceGroupId" CssClass="js-resource-group-id" runat="server" />
                                        <Rock:HiddenFieldWithClass ID="hfResourceGroupMemberFilterType" CssClass="js-resource-groupmemberfiltertype" runat="server" />
                                        <Rock:HiddenFieldWithClass ID="hfResourceDataViewId" CssClass="js-resource-dataview-id" runat="server" />
                                        <Rock:HiddenFieldWithClass ID="hfResourceAdditionalPersonIds" CssClass="js-resource-additional-person-ids" runat="server" />

                                        <div class="js-unassigned-resource-template" style="display: none">
                                            <%-- template that groupScheduler.js uses to populate unassigned resources --%>

                                            <div class="meta js-resource resource" data-state="unassigned" data-has-conflict="false" data-is-scheduled="" data-person-id="">
                                                <div class="meta-figure">
                                                </div>
                                                <div class="meta-body">
                                                    <div class="flex">
                                                        <div class="resource-name pull-left">
                                                            <span class="js-resource-name"></span>
                                                        </div>
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
                                            </div>
                                        </div>

                                        <div class="panel panel-block">

                                            <div class="panel-heading">
                                                <h1 class="panel-title">
                                                    <i class="fa fa-user"></i>
                                                    Resource List
                                                </h1>

                                                <div class="panel-labels">
                                                    <button class="btn btn-xs btn-default js-select-all" >Select All</button>
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

                                                            <%-- loading indicator --%>
                                                            <i class="fa fa-refresh fa-spin margin-l-md js-loading-notification" style="display: none; opacity: .4;"></i>

                                                            <%-- container for list of resources --%>

                                                            <asp:Panel ID="pnlResourceListContainer" CssClass="js-scheduler-source-container resource-container dropzone" runat="server">
                                                            </asp:Panel>
                                                        </div>
                                                    </asp:Panel>
                                                </div>

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
