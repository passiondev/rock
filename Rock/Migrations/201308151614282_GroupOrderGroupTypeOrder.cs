//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class GroupOrderGroupTypeOrder : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            AddColumn("dbo.Group", "Order", c => c.Int(nullable: false));
            
            // GroupType already had a column called DisplayOrder but that wasn't consistent with the other tables that have an Order column.  So rename it
            AddColumn("dbo.GroupType", "Order", c => c.Int(nullable: false));
            DropColumn("dbo.GroupType", "DisplayOrder");
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            AddColumn("dbo.GroupType", "DisplayOrder", c => c.Int(nullable: false));
            DropColumn("dbo.GroupType", "Order");
            DropColumn("dbo.Group", "Order");
        }
    }
}
