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
    public partial class AddStructuredContentToolDefinedType : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType( "Tools", "Structured Content Editor Tools", "", "E43AD92C-4DD4-4D78-9852-FCFAEFDF52CA", @"" );
            RockMigrationHelper.UpdateDefinedValue( "E43AD92C-4DD4-4D78-9852-FCFAEFDF52CA", "Default", "{     header: {     class: Header,     inlineToolbar: ['link'],     config: {         placeholder: 'Header'     },     shortcut: 'CMD+SHIFT+H'     },     image: {     class: ImageTool,     inlineToolbar: ['link'],     },     list: {     class: List,     inlineToolbar: true,     shortcut: 'CMD+SHIFT+L'     },     checklist: {     class: Checklist,     inlineToolbar: true,     },     quote: {     class: Quote,     inlineToolbar: true,     config: {         quotePlaceholder: 'Enter a quote',         captionPlaceholder: 'Quote\\'s author',     },     shortcut: 'CMD+SHIFT+O'     },     warning: Warning,     marker: {     class:  Marker,     shortcut: 'CMD+SHIFT+M'     },     code: {     class:  CodeTool,     shortcut: 'CMD+SHIFT+C'     },     delimiter: Delimiter,     inlineCode: {     class: InlineCode,     shortcut: 'CMD+SHIFT+C'     },     linkTool: LinkTool,     embed: Embed,     table: {     class: Table,     inlineToolbar: true,     shortcut: 'CMD+ALT+T'     } }", "31C63FB9-1365-4EEF-851D-8AB9A188A06C", false );
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteDefinedValue( "31C63FB9-1365-4EEF-851D-8AB9A188A06C" ); // Default
            RockMigrationHelper.DeleteDefinedType( "E43AD92C-4DD4-4D78-9852-FCFAEFDF52CA" ); // Structured Content Editor Tools
        }
    }
}
