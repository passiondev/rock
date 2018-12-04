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
            <div class="panel-body">
                <div class="row">
                    <%-- Options --%>
                    <div class="col-md-3 filter-options">
                        <Rock:GroupPicker ID="gpGroup" runat="server" Label="Group" OnValueChanged="gpGroup_ValueChanged" />
                        <Rock:DatePicker ID="dpWeek" runat="server" Label="Week" AllowPastDateSelection="true" AllowFutureDateSelection="true"  />

                        <Rock:RockRadioButtonList ID="rblSchedule" runat="server" Label="Schedule" OnSelectedIndexChanged="cblSchedule_SelectedIndexChanged" />
                        <Rock:RockCheckBoxList ID="cblLocations" runat="server" Label="Locations" />

                        <Rock:RockControlWrapper ID="rcwResourceListSource" runat="server" Label="Resource List Source" >
                            <Rock:ButtonGroup ID="bgResourceListSource" runat="server" >
                                <asp:ListItem Text="Group" Selected="True" />
                                <asp:ListItem Text="Alternate Group" />
                                <asp:ListItem Text="Data View" />
                            </Rock:ButtonGroup>
                            <Rock:RockRadioButtonList ID="rblGroupMemberFilter" runat="server" />
                        </Rock:RockControlWrapper>
                    </div>

                    <%-- Scheduling --%>
                    <div class="col-md-9">
                        <div class="row">
                        </div>
                    </div>
                </div>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
