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
                            <Rock:RockCheckBoxList ID="cblLocations" runat="server" Label="Locations" AutoPostBack="true" OnSelectedIndexChanged="cblLocations_SelectedIndexChanged" />
                        </asp:Panel>

                        <Rock:RockControlWrapper ID="rcwResourceListSource" runat="server" Label="Resource List Source">
                            <Rock:ButtonGroup ID="bgResourceListSource" runat="server" SelectedItemClass="btn btn-xs btn-primary" UnselectedItemClass="btn btn-xs btn-default" AutoPostBack="true" OnSelectedIndexChanged="bgResourceListSource_SelectedIndexChanged" />

                            <asp:Panel ID="pnlResourceFilterGroup" runat="server">
                                <Rock:RockRadioButtonList ID="rblGroupMemberFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="rblGroupMemberFilter_SelectedIndexChanged">
                                    <asp:ListItem Text="Show Matching Preference" Value="0" />
                                    <asp:ListItem Text="Show All Group Members" Value="1" />
                                </Rock:RockRadioButtonList>
                            </asp:Panel>

                            <asp:Panel ID="pnlResourceFilterAlternateGroup" runat="server">
                                <Rock:GroupPicker ID="gpResourceListAlternateGroup" runat="server" Label="Alternate Group" OnValueChanged="gpAlternateGroup_ValueChanged" />
                            </asp:Panel>

                            <asp:Panel ID="pnlResourceFilterDataView" runat="server">
                                <Rock:DataViewItemPicker ID="dvpResourceListDataView" runat="server" Label="Data View" EntityTypeId="15" OnValueChanged="dvpResourceListDataView_ValueChanged" />
                            </asp:Panel>
                        </Rock:RockControlWrapper>
                    </div>

                    <%-- Scheduling --%>
                    <div class="col-md-9">
                        <div class="row">
                            <div class="col-md-4">
                                <div class="panel panel-block">
                                    <div class="panel-heading">
                                        <h1 class="panel-title">
                                            <i class="fa fa-user"></i>
                                            Resource List
                                        </h1>

                                        <div class="panel-labels">
                                            <asp:LinkButton ID="btnSelectAllResource" runat="server" CssClass="btn btn-xs btn-default" Text="Select All" OnClick="btnSelectAllResource_Click"/>
                                            <asp:LinkButton ID="btnAddResource" runat="server" CssClass="btn btn-xs btn-default" OnClick="btnAddResource_Click" >
                                                <i class="fa fa-plus"></i>
                                            </asp:LinkButton>
                                        </div>
                                    </div>
                                    <div class="panel-body">
                                        <Rock:RockTextBox ID="sfResource" runat="server" PrependText="<i class='fa fa-search'></i>" Placeholder="Search" />
                                        
                                        <asp:Repeater ID="rptResources" runat="server">
                                            <ItemTemplate>
                                                #TODO#
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                </div>

                            </div>
                            <div class="col-md-8">
                                <h1>Locations</h1>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
