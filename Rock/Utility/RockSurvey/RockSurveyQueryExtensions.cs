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
using System;
using System.Linq;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Utility
{
    /// <summary>
    /// Query extensions used to calculate metrics required for a Rock Survey.
    /// </summary>
    public static class RockSurveyQueryExtensions
    {
        #region Attributes

        /// <summary>
        /// Filter a query to return only those records marked as active.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Rock.Model.Attribute> WhereIsAttachedToEntityType<TEntity>( this IQueryable<Rock.Model.Attribute> qry )
        {
            int? entityTypeId = EntityTypeCache.Get( typeof( TEntity ), createIfNotFound: false )?.Id;

            return qry.Where( x => x.EntityTypeId == entityTypeId );
        }

        #endregion

        #region Connection Requests

        /// <summary>
        /// Filter a query to return only Connection Requests that are currently flagged as active.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Rock.Model.ConnectionRequest> WhereIsActive( this IQueryable<Rock.Model.ConnectionRequest> qry )
        {
            return qry.Where( x => x.ConnectionState == ConnectionState.Active );
        }

        #endregion

        #region Person

        /// <summary>
        /// Filter a query to return only Person entities having a record type of "Person", as opposed to other types of contacts also stored in the Person table.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Person> WhereIsPersonRecordType( this IQueryable<Person> qry )
        {
            int? recordTypePersonId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() );

            return qry.Where( x => x.RecordTypeValueId == recordTypePersonId );
        }

        /// <summary>
        /// Filter a query to return only those Person records having a record type of "Business".
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Person> WhereIsBusinessRecordType( this IQueryable<Person> qry )
        {
            int? recordTypePersonId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() );

            return qry.Where( x => x.RecordTypeValueId == recordTypePersonId );
        }

        /// <summary>
        /// Filter a query to return only those records marked as active.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Person> WhereIsActive( this IQueryable<Person> qry )
        {
            int? recordStatusActiveId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() );

            return qry.Where( x => x.RecordStatusValueId == recordStatusActiveId );
        }

        /// <summary>
        /// Filter a query to return only those people having an associated photo.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Person> WhereHasPhoto( this IQueryable<Person> qry )
        {
            return qry.Where( x => x.PhotoId != null );
        }

        #endregion

        #region Groups

        /// <summary>
        /// Filter a query to return only those records marked as active.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Group> WhereIsActive( this IQueryable<Group> qry )
        {
            return qry.Where( x => x.IsActive );
        }

        /// <summary>
        /// Filter a query to return only those records marked as active.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Group> WhereIsArchived( this IQueryable<Group> qry )
        {
            return qry.Where( x => x.IsArchived );
        }

        /// <summary>
        /// Filter a query to return only Groups that represents a Family.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Group> WhereIsFamily( this IQueryable<Group> qry )
        {
            return qry.WhereIsWellKnownType( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
        }

        /// <summary>
        /// Filter a query to return only Groups that represents a Family.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Group> WhereIsSecurityRole( this IQueryable<Group> qry )
        {
            return qry.WhereIsWellKnownType( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE );
        }

        /// <summary>
        /// Filter a query to return only Groups that represents Communication Lists.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Group> WhereIsCommunicationList( this IQueryable<Group> qry )
        {
            return qry.WhereIsWellKnownType( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST );
        }

        /// <summary>
        /// Filter a query to return only Groups that represents a well-known type, identified by a Guid.
        /// </summary>
        /// <param name="qry"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static IQueryable<Group> WhereIsWellKnownType( this IQueryable<Group> qry, Guid guid )
        {
            int? groupTypeId = GroupTypeCache.GetId( guid );

            return qry.Where( x => x.GroupTypeId == groupTypeId );
        }

        /// <summary>
        /// Filter a query to return only Groups that represents a well-known type, identified by a Guid.
        /// </summary>
        /// <param name="qry"></param>
        /// <param name="guidText"></param>
        /// <returns></returns>
        public static IQueryable<Group> WhereIsWellKnownType( this IQueryable<Group> qry, string guidText )
        {
            var guid = guidText.AsGuidOrNull();

            if ( guid == null )
            {
                throw new ArgumentException( string.Format( "Value \"{0}\" is not a valid Guid.", guidText ) );
            }

            return qry.WhereIsWellKnownType( guid.Value );
        }

        #endregion

        #region Following

        /// <summary>
        /// Filter a query to return only those Following links that have a Person as a target.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Following> WhereTargetTypeIsPerson( this IQueryable<Following> qry )
        {
            var personEntityTypeId = EntityTypeCache.GetId( typeof( Rock.Model.Person ) );

            return qry.Where( x => x.EntityTypeId == personEntityTypeId );

        }

        #endregion

        #region Workflow

        /// <summary>
        /// Filter a query to return only those records marked as active.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Rock.Model.Workflow> WhereIsActive( this IQueryable<Rock.Model.Workflow> qry )
        {
            return qry.Where( x => x.ActivatedDateTime.HasValue && !x.CompletedDateTime.HasValue );
        }

        #endregion

        #region Finance

        /// <summary>
        /// Filter a query to return only those records marked as active.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<FinancialGateway> WhereIsActive( this IQueryable<FinancialGateway> qry )
        {
            return qry.Where( x => x.IsActive );
        }

        #endregion

        #region Tags

        /// <summary>
        /// Filter a query to return only those tags having organisation-level scope.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Rock.Model.Tag> WhereHasOrganisationScope( this IQueryable<Rock.Model.Tag> qry )
        {
            return qry.Where( x => x.OwnerPersonAliasId == null );
        }

        /// <summary>
        /// Filter a query to return only those tags having person-level scope.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns></returns>
        public static IQueryable<Rock.Model.Tag> WhereHasPersonScope( this IQueryable<Rock.Model.Tag> qry )
        {
            return qry.Where( x => x.OwnerPersonAliasId != null );
        }

        #endregion
    }
}