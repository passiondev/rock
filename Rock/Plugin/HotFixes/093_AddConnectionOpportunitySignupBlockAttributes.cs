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
namespace Rock.Plugin.HotFixes
{
    /// <summary>
    /// Plugin Migration.
    /// </summary>
    [MigrationNumber( 93, "1.10.1" )]
    public class AddConnectionOpportunitySignupBlockAttributes : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            AddAttributes();
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            // Not yet used by hotfix migrations.
        }

        /// <summary>
        /// Add Attributes for BlockType "Connection Opportunity Signup".
        /// </summary>
        private void AddAttributes()
        {
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( Rock.SystemGuid.BlockType.CONNECTION_OPPORTUNITY_SIGNUP, Rock.SystemGuid.FieldType.CATEGORIES, "Include Attribute Categories", "IncludeAttributeCategories", "", "Attributes in these Categories will be displayed.", 8, @"", Rock.SystemGuid.Attribute.CONNECTION_OPPORTUNITY_SIGNUP_INCLUDE_ATTRIBUTE_CATEGORIES );
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( Rock.SystemGuid.BlockType.CONNECTION_OPPORTUNITY_SIGNUP, Rock.SystemGuid.FieldType.CATEGORIES, "Exclude Attribute Categories", "ExcludeAttributeCategories", "", "Attributes in these Categories will not be displayed.", 9, @"", Rock.SystemGuid.Attribute.CONNECTION_OPPORTUNITY_SIGNUP_EXCLUDE_ATTRIBUTE_CATEGORIES );
        }
    }
}
