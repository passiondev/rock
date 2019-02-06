<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupSchedulerAnalytics.ascx.cs" Inherits="Blocks_Groups_GroupSchedulerAnalytics" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block panel-analytics">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-line-chart"></i>
                    Group Scheduler Analytics
                </h1>
            </div>

            <div class="panel-body">

                <div class="filters">
                    <%-- Group picker --%>
                    <Rock:GroupPicker ID="gpGroups" runat="server" AllowMultiSelect="true" Label="Select Groups" LimitToSchedulingEnabledGroups="true" />

                    <%-- Person Picker --%>
                    <Rock:PersonPicker ID="ppPerson" runat="server" Label="Person" />

                    <%-- Date Picker --%>
                    <Rock:SlidingDateRangePicker ID="sdrpDateRange" runat="server" Label="Date Range" />

                    <%-- Dataview Picker --%>
                    <Rock:DataViewPicker ID="dvDataViews" runat="server"></Rock:DataViewPicker>


                    <%-- Locations CBL --%>
                    <Rock:RockCheckBoxList ID="cblLocations" runat="server" Label="Locations" RepeatColumns="1" RepeatDirection="Vertical" ></Rock:RockCheckBoxList>


                    <%-- Schedules CBL --%>
                    <Rock:RockCheckBoxList ID="dblSchedules" runat="server" Label="Schedules" RepeatColumns="1" RepeatDirection="Vertical" ></Rock:RockCheckBoxList>


                </div>

                <div class="graphs">
                    <div class="row">
                        <%-- Bar chart to show the data in the tabular --%>



                        <%-- Doughnut chart to show the decline reasons--%>
                    </div>
                    <div class="row">
                        <%-- tabular data --%>
                        <Rock:Grid ID="gData" runat="server" AllowPaging="true" EmptyDataText="No Data Found" ShowActionsInHeader="false">
                            <Columns>
                                <Rock:RockBoundField DataField="FullName" HeaderText="Name"></Rock:RockBoundField>
                                <Rock:RockBoundField DataField="CheckIns" HeaderText="Checked In"></Rock:RockBoundField>
                                <Rock:RockBoundField DataField="MissedConfirmations" HeaderText="Missed Confirmations"></Rock:RockBoundField>
                                <Rock:RockBoundField DataField="Declines" HeaderText="Declines"></Rock:RockBoundField>
                            </Columns>
                        </Rock:Grid>

                    </div>

                </div>

            </div>

            <script>


            </script>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>