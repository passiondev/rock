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
    using Rock.Migrations.Migrations;

    /// <summary>
    ///
    /// </summary>
    public partial class AddCampusTeamToAllCampuses : Rock.Migrations.RockMigration
    {
        private static class Guids
        {
            public const string GROUP_TYPE_CAMPUS_TEAM = SystemGuid.GroupType.GROUPTYPE_CAMPUS_TEAM;
            public const string GROUP_TYPE_ROLE_CAMPUS_PASTOR = "F8C6289B-0E68-4121-A595-A51369404EBA";
            public const string GROUP_TYPE_ROLE_CAMPUS_ADMINISTRATOR = "07F857ED-C0D7-47B4-AB6C-9AFDFAE2ADD9";

            public const string ENTITY_TYPE_PERSON_GET_CAMPUS_TEAM_MEMBER = "6A4F7FEC-3D49-4A31-882C-2D10DB84231E";

            public const string WORKFLOW_ATTRIBUTE_PERSON = "C10C4C89-2B91-4D9A-8D5F-A3E65758A878";
            public const string WORKFLOW_ATTRIBUTE_CAMPUS = "B07F920E-8450-4D1F-985D-6241E4F5E5CB";
            public const string WORKFLOW_ATTRIBUTE_CAMPUS_ROLE = "5F8F5E6B-5888-4834-B47B-36664FB3A96C";
            public const string WORKFLOW_ATTRIBUTE_CAMPUS_TEAM_MEMBER = "7CFEDCB2-EA8F-421F-BA5E-B0D8BD10EA92";
        }

        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            // Add 'Campus Team' GroupType
            RockMigrationHelper.AddGroupType( "Campus Team", "Used to track groups that serve a given Campus.", "Group", "Member", false, false, false, null, 0, null, 0, null, Guids.GROUP_TYPE_CAMPUS_TEAM, true );

            // Add default Roles to 'Campus Team' GroupType
            RockMigrationHelper.AddGroupTypeRole( Guids.GROUP_TYPE_CAMPUS_TEAM, "Campus Pastor", "Pastor of a Campus", 0, 1, null, Guids.GROUP_TYPE_ROLE_CAMPUS_PASTOR, true, true, false );
            RockMigrationHelper.AddGroupTypeRole( Guids.GROUP_TYPE_CAMPUS_TEAM, "Campus Administrator", "Administrator of a Campus", 1, null, null, Guids.GROUP_TYPE_ROLE_CAMPUS_ADMINISTRATOR, true, false, true );

            // Add 'Group Member Detail' Page to 'Campus Detail' Page
            RockMigrationHelper.AddPage( true, "BDD7B906-4D42-43C0-8DBB-B89A566734D8", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Group Member Detail", "", "EB135AE0-5BAC-458B-AD5B-47460C2BFD31", "fa fa-users" ); // Site:Rock RMS
            RockMigrationHelper.AddPageRoute( "EB135AE0-5BAC-458B-AD5B-47460C2BFD31", "Campus/{CampusId}/GroupMember/{GroupMemberId}", "9660B9FB-C90F-4AFE-9D58-C0EC271C1377" ); // for Page:Group Member Detail

            // Add Block to Page: Campus Detail Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "BDD7B906-4D42-43C0-8DBB-B89A566734D8".AsGuid(), null, "C2D29296-6A87-47A9-A753-EE4E9159C4C4".AsGuid(), "88B7EFA9-7419-4D05-9F88-38B936E61EDD".AsGuid(), "Group Member List", "Main", @"", @"", 1, "318B80EE-7349-4BF4-82F2-64FC38A5AB0B" );

            // update block order for pages with new blocks if the page,zone has multiple blocks
            Sql( @"UPDATE [Block] SET [Order] = 0 WHERE [Guid] = '176FFC6F-6B55-4319-A781-A2F7F1F85F24'" );  // Page: Campus Detail,  Zone: Main,  Block: Campus Detail
            Sql( @"UPDATE [Block] SET [Order] = 1 WHERE [Guid] = '318B80EE-7349-4BF4-82F2-64FC38A5AB0B'" );  // Page: Campus Detail,  Zone: Main,  Block: Group Member List

            // Attrib Value for Block:Group Member List, Attribute:core.CustomGridEnableStickyHeaders Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "7F31B5A8-96F6-4D30-9BB6-3EB2DBE26234", @"False" );
            // Attrib Value for Block:Group Member List, Attribute:Show Note Column Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "5F54C068-1418-44FA-B215-FBF70072F6A5", @"False" );
            // Attrib Value for Block:Group Member List, Attribute:Show Date Added Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "F281090E-A05D-4F81-AD80-A3599FB8E2CD", @"False" );
            // Attrib Value for Block:Group Member List, Attribute:Show Campus Filter Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "65B9EA6C-D904-4105-8B51-CCA784DDAAFA", @"True" );
            // Attrib Value for Block:Group Member List, Attribute:Show First/Last Attendance Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "65834FB0-0AB0-4F73-BE1B-9D2F9FFD2664", @"False" );
            // Attrib Value for Block:Group Member List, Attribute:Detail Page Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "E4CCB79C-479F-4BEE-8156-969B2CE05973", @"eb135ae0-5bac-458b-ad5b-47460c2bfd31,9660b9fb-c90f-4afe-9d58-c0ec271c1377" );

            // Add Block to Page: Group Member Detail Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "EB135AE0-5BAC-458B-AD5B-47460C2BFD31".AsGuid(), null, "C2D29296-6A87-47A9-A753-EE4E9159C4C4".AsGuid(), "AAE2E5C3-9279-4AB0-9682-F4D19519D678".AsGuid(), "Group Member Detail", "Main", @"", @"", 0, "96361229-3CF1-4713-84B5-E913AECDC804" );

            // Seed all existing Campuses with a new 'TeamGroup' Group association
            Sql( RockMigrationSQL._202001142201240_AddCampusTeamToAllCampuses_Up );

            // Add 'PersonGetCampusTeamMember' Action to the EntityType table
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.PersonGetCampusTeamMember", Guids.ENTITY_TYPE_PERSON_GET_CAMPUS_TEAM_MEMBER, false, true );

            // Add 'PersonGetCampusTeamMember' Action's Attributes
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( Guids.ENTITY_TYPE_PERSON_GET_CAMPUS_TEAM_MEMBER, SystemGuid.FieldType.WORKFLOW_ATTRIBUTE, "Person", "Person", "Workflow attribute that contains the person to get the Campus team member for.", 0, @"", Guids.WORKFLOW_ATTRIBUTE_PERSON );
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( Guids.ENTITY_TYPE_PERSON_GET_CAMPUS_TEAM_MEMBER, SystemGuid.FieldType.WORKFLOW_ATTRIBUTE, "Campus", "Campus", "Workflow attribute that contains the Campus to get the Campus team member for. If both Person and Campus are provided, Campus takes precedence over the Person's Campus. If Campus is not provided, the Person's primary Campus will be assigned to this attribute.", 1, @"", Guids.WORKFLOW_ATTRIBUTE_CAMPUS );
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( Guids.ENTITY_TYPE_PERSON_GET_CAMPUS_TEAM_MEMBER, SystemGuid.FieldType.WORKFLOW_ATTRIBUTE, "Campus Role", "CampusRole", "Workflow attribute that contains the Role of the Campus team member to get. If multiple team members are in this role for a given Campus, the first match will be selected.", 2, @"", Guids.WORKFLOW_ATTRIBUTE_CAMPUS_ROLE );
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( Guids.ENTITY_TYPE_PERSON_GET_CAMPUS_TEAM_MEMBER, SystemGuid.FieldType.WORKFLOW_ATTRIBUTE, "Campus Team Member", "CampusTeamMember", "Workflow attribute to assign the Campus team member to.", 3, @"", Guids.WORKFLOW_ATTRIBUTE_CAMPUS_TEAM_MEMBER );
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // Remove 'PersonGetCampusTeamMember' Action's Attributes
            RockMigrationHelper.DeleteAttribute( Guids.WORKFLOW_ATTRIBUTE_PERSON );
            RockMigrationHelper.DeleteAttribute( Guids.WORKFLOW_ATTRIBUTE_CAMPUS );
            RockMigrationHelper.DeleteAttribute( Guids.WORKFLOW_ATTRIBUTE_CAMPUS_ROLE );
            RockMigrationHelper.DeleteAttribute( Guids.WORKFLOW_ATTRIBUTE_CAMPUS_TEAM_MEMBER );

            // Remove 'PersonGetCampusTeamMember' Action from the EntityType table
            RockMigrationHelper.DeleteEntityType( Guids.ENTITY_TYPE_PERSON_GET_CAMPUS_TEAM_MEMBER );

            // Delete any Campus > Group assiations that were seeded as a part of the Up() method
            Sql( RockMigrationSQL._202001142201240_AddCampusTeamToAllCampuses_Down );

            // Remove Block: Group Member Detail, from Page: Group Member Detail, Site: Rock RMS
            RockMigrationHelper.DeleteBlock( "96361229-3CF1-4713-84B5-E913AECDC804" );

            // Attrib Value for Block:Group Member List, Attribute:core.CustomGridEnableStickyHeaders Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.DeleteBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "7F31B5A8-96F6-4D30-9BB6-3EB2DBE26234" );
            // Attrib Value for Block:Group Member List, Attribute:Show Note Column Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.DeleteBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "5F54C068-1418-44FA-B215-FBF70072F6A5" );
            // Attrib Value for Block:Group Member List, Attribute:Show Date Added Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.DeleteBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "F281090E-A05D-4F81-AD80-A3599FB8E2CD" );
            // Attrib Value for Block:Group Member List, Attribute:Show Campus Filter Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.DeleteBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "65B9EA6C-D904-4105-8B51-CCA784DDAAFA" );
            // Attrib Value for Block:Group Member List, Attribute:Show First/Last Attendance Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.DeleteBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "65834FB0-0AB0-4F73-BE1B-9D2F9FFD2664" );
            // Attrib Value for Block:Group Member List, Attribute:Detail Page Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.DeleteBlockAttributeValue( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B", "E4CCB79C-479F-4BEE-8156-969B2CE05973" );

            // Remove Block: Group Member List, from Page: Campus Detail, Site: Rock RMS
            RockMigrationHelper.DeleteBlock( "318B80EE-7349-4BF4-82F2-64FC38A5AB0B" );            

            // Remove 'Group Member Detail' Page from 'Campus Detail' Page
            RockMigrationHelper.DeletePageRoute( "9660B9FB-C90F-4AFE-9D58-C0EC271C1377" );
            RockMigrationHelper.DeletePage( "EB135AE0-5BAC-458B-AD5B-47460C2BFD31" ); //  Page: Group Member Detail, Layout: Full Width, Site: Rock RMS

            // Remove default Roles from 'Campus Team' GroupType
            RockMigrationHelper.DeleteGroupTypeRole( Guids.GROUP_TYPE_ROLE_CAMPUS_PASTOR );
            RockMigrationHelper.DeleteGroupTypeRole( Guids.GROUP_TYPE_ROLE_CAMPUS_ADMINISTRATOR );

            // Remove 'Campus Team' GroupType
            RockMigrationHelper.DeleteGroupType( Guids.GROUP_TYPE_CAMPUS_TEAM );
        }
    }
}
