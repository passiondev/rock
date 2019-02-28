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
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class AddSchedulingUpdateSystemEmail : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.UpdateSystemEmail( "Groups", "Scheduling Update Email", "", "", "", "", "", "Scheduling Update", @"{{ 'Global' | Attribute:'EmailHeader' }}
<h1>Scheduling Update</h1>
<p>Hi {{  Attendance.PersonAlias.Person.NickName  }}!</p>
<br/>
<p>You have been added to the schedule for the following dates and times. Please let us know if you'll be attending as soon as possible.</p>
<br/>
<p>Thanks!</p>
{{ Attendance.ScheduledByPersonAlias.Person.FullName  }}
</br>
{{ 'Global' | 'Attribute:OrganizationName' }}
<br/>
<br/>
<table>
{% for attendance in Attendances %}
    <tr><td>&nbsp;</td></tr>
    <tr><td><h5>{{attendance.Occurrence.OccurrenceDate | Date:'dddd, MMMM, d, yyyy'}}</h5></td></tr>
    <tr><td>{{ attendance.Occurrence.Group.Name }}</td></tr>
    <tr><td>{{ attendance.Location.Name }}&nbsp;{{ attendance.Schedule.Name }}</td></tr>
    {% if forloop.first  %}
    <tr><td><a href=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}ScheduleConfirmation?attendanceId{{attendance.Id}}&isConfirmed=true"">Accept</a>&nbsp;<a href=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}ScheduleConfirmation?attendanceId{{attendance.Id}}&isConfirmed=false"">Decline</a></td>
    </tr>
    <tr><td>&nbsp;</td></tr>
{% endif %}
{% endfor %}
</table>
<br/>
{{ 'Global' | Attribute:'OrganizationName' }}
<br/>
{{ ScheduledDate | Date: 'dddd, MMMM, d, yyyy' }}", "F8E4CE07-68F5-4169-A865-ECE915CF421C" );
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteSystemEmail( "F8E4CE07-68F5-4169-A865-ECE915CF421C" );
        }
    }
}
