// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace Rock.Migrations
{

    /// <summary>
    ///
    /// </summary>
    public partial class AddSchedulingResponseEmailTemplate : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.UpdateSystemEmail( "Groups", "Scheduling Response Email", "", "", "", "", "", "{%- assign rsvp = ScheduledItem.RSVP | AsBoolean -%} {% if rsvp %}Accepted{% else %}Declined{% endif %}",
@"{{ ""Global"" | Attribute:""EmailHeader"" }}
<h1>Scheduling Response</h1>
<p>Hi {{ Scheduler.NickName }}!</p>
<br/>
<p>{{ Person.FullName }}{%- assign rsvp = ScheduledItem.RSVP | AsBoolean -%} {% if rsvp %} has confirmed and will be at the:{%else %} is unable to attend the: {% endif %}</p>
<br/>
{{ Group.Name }}
{{ ScheduledItem.Location.Name }} {{ScheduledItem.Schedule.Name}}
<br/>
{{ ""Global"" | Attribute:""OrganizationName"" }}<br/>
<h2>{{ScheduledItem.Occurence.OccurenceDate | Date: ""dddd, MMMM, d, yyyy""}}</h2>
<p>&nbsp;</p>
{{ ""Global"" | Attribute:""EmailFooter"" }}", "D095F78D-A5CF-4EF6-A038-C7B07E250611" );
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteSystemEmail( "D095F78D-A5CF-4EF6-A038-C7B07E250611" );
        }
    }
}
