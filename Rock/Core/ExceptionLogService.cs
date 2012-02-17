//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the T4\Model.tt template.
//
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
using System;
using System.Collections.Generic;
using System.Linq;

using Rock.Data;

namespace Rock.Core
{
	/// <summary>
	/// Exception Log POCO Service Layer class
	/// </summary>
    public partial class ExceptionLogService : Service<Rock.Core.ExceptionLog>
    {
		/// <summary>
		/// Gets Exception Logs by Parent Id
		/// </summary>
		/// <param name="parentId">Parent Id.</param>
		/// <returns>An enumerable list of ExceptionLog objects.</returns>
	    public IEnumerable<Rock.Core.ExceptionLog> GetByParentId( int? parentId )
        {
            return Repository.Find( t => ( t.ParentId == parentId || ( parentId == null && t.ParentId == null ) ) );
        }
		
		/// <summary>
		/// Gets Exception Logs by Site Id
		/// </summary>
		/// <param name="siteId">Site Id.</param>
		/// <returns>An enumerable list of ExceptionLog objects.</returns>
	    public IEnumerable<Rock.Core.ExceptionLog> GetBySiteId( int? siteId )
        {
            return Repository.Find( t => ( t.SiteId == siteId || ( siteId == null && t.SiteId == null ) ) );
        }
		
		/// <summary>
		/// Gets Exception Logs by Person Id
		/// </summary>
		/// <param name="personId">Person Id.</param>
		/// <returns>An enumerable list of ExceptionLog objects.</returns>
	    public IEnumerable<Rock.Core.ExceptionLog> GetByPersonId( int? personId )
        {
            return Repository.Find( t => ( t.PersonId == personId || ( personId == null && t.PersonId == null ) ) );
        }
		
    }
}
