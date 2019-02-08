<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupSchedulerAnalytics.ascx.cs" Inherits="RockWeb.Blocks.Groups.GroupSchedulerAnalytics" %>

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
                <div class="row row-eq-height">
                    <div class="col-lg-2 col-md-3 filter-options">
                        <Rock:NotificationBox ID="nbFilterNotification" runat="server" NotificationBoxType="Warning" visible="false"></Rock:NotificationBox>

                        <label>Please select a Group, Person, or Data View</label>
                            
                        <%-- Group picker --%>
                        <Rock:GroupPicker ID="gpGroups" runat="server" AllowMultiSelect="false" Label="Select Groups" LimitToSchedulingEnabledGroups="true" OnSelectItem="gpGroups_SelectItem" />

                        <%-- Person Picker --%>
                        <Rock:PersonPicker ID="ppPerson" runat="server" Label="Person" OnSelectPerson="ppPerson_SelectPerson" />
                        
                        <%-- Date Picker --%>
                        <Rock:SlidingDateRangePicker ID="sdrpDateRange" runat="server" Label="Date Range" AllowPastDateSelection="true" AllowFutureDateSelection="true" />

                        <%-- Locations CBL --%>
                        <Rock:RockCheckBoxList ID="cblLocations" runat="server" Label="Locations" RepeatColumns="1" RepeatDirection="Vertical" OnSelectedIndexChanged="cblLocations_SelectedIndexChanged" AutoPostBack="true" ></Rock:RockCheckBoxList>

                        <%-- Schedules CBL --%>
                        <Rock:RockCheckBoxList ID="cblSchedules" runat="server" Label="Schedules" RepeatColumns="1" RepeatDirection="Vertical" ></Rock:RockCheckBoxList>

                        <%-- Dataview Picker --%>
                        <Rock:DataViewPicker ID="dvDataViews" runat="server" Label="Data View"></Rock:DataViewPicker>

                        <asp:LinkButton ID="btnUpdate" runat="server" CssClass="btn btn-default btn-block" OnClick="btnUpdate_Click"><i class="fa fa-sync"></i>&nbsp;Refresh</asp:LinkButton>

                    </div>

                    <div class="col-lg-10 col-md-9 resource-area">
                        <div class="row">
                            <%-- Bar chart to show the data in the tabular --%>
                            <div class="chart-container">
                            <Rock:NotificationBox ID="nbBarChartMessage" runat="server" NotificationBoxType="Info" Text="No Group Scheduler Data To Show" />
                            <canvas id="barChartCanvas" runat="server" style="height: 450px;" />
                        </div>


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
            </div>

            <script>
                Sys.Application.add_load(function () {

                    var chartSeriesColors = <%=this.SeriesColorsJSON%>;

                    var getSeriesColors = function(numberOfColors) {

                        var result = chartSeriesColors;
                        while (result.length < numberOfColors)
                        {
                            result = result.concat(chartSeriesColors);
                        }

                        return result;
                    };







                });
            </script>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>