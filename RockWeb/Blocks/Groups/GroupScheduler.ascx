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
                    <asp:LinkButton ID="btnRecompileLess" runat="server" CssClass="btn btn-default btn-xs" OnClick="btnRecompileLess_Click" Text="RECOMPILE LESS" style="background-color: violet;" />
                </div>
            </div>

            <%-- Panel Body --%>
            <div class="panel-body panel-groupscheduler">
                <div class="row">
                    <%-- Options --%>
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
                            <Rock:ButtonGroup ID="bgResourceListSource" runat="server" SelectedItemClass="btn btn-xs btn-primary" UnselectedItemClass="btn btn-xs btn-default" AutoPostBack="true" OnSelectedIndexChanged="bgResourceListSource_SelectedIndexChanged" />

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
                        <%-- container for the dragula containers --%>
                        <asp:Panel ID="pnlSchedulerDragula" runat="server">
                            <div class="row">
                                <div class="col-md-4">

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
                                            <Rock:RockTextBox ID="sfResource" runat="server" PrependText="<i class='fa fa-search'></i>" Placeholder="Search" />
                                            <%-- Dragula container for list of resources --%>
                                            <asp:Panel ID="pnlResourceListContainer" CssClass="js-scheduler-source-container resource-container" runat="server">
                                                <asp:Repeater ID="rptResources" runat="server" OnItemDataBound="rptResources_ItemDataBound">
                                                    <ItemTemplate>
                                                        <div class="js-resource resource" style="background-color: violet;" data-state="unassigned">
                                                            <div class="js-resource-name resource-name">
                                                                <asp:Literal ID="lPersonName" runat="server" />
                                                            </div>
                                                        </div>
                                                    </ItemTemplate>
                                                </asp:Repeater>
                                            </asp:Panel>
                                        </div>
                                    </div>
                                </div>


                                <div class="col-md-8">
                                    <asp:Repeater ID="rptGroupLocations" runat="server" OnItemDataBound="rptGroupLocations_ItemDataBound">
                                        <ItemTemplate>
                                            <div class="location js-location">
                                                <div class="panel panel-block">
                                                    <div class="panel-heading">
                                                        <h1 class="panel-title">
                                                            <asp:Literal ID="lLocationTitle" runat="server" />
                                                        </h1>
                                                    </div>
                                                    <div class="panel-body">
                                                        <div class="js-scheduler-target-container dropzone padding-all-md" style="background-color: lime"></div>
                                                    </div>
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </div>
                        </asp:Panel>
                    </div>
                </div>
            </div>

        </asp:Panel>
        <script>
            Sys.Application.add_load(function () {

                var schedulerDragulaId = '<%=pnlSchedulerDragula.ClientID%>';

                Rock.controls.groupScheduler.initialize({
                    id: schedulerDragulaId,
                });
            });
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
