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
using System.Collections.Generic;

namespace Rock.Field.Types
{
    /// <summary>
    /// Specifies the level of availability for a data entry field.
    /// </summary>
    public enum RequirementLevelSpecifier
    {
        /// <summary>
        /// No requirement level has been specified for this data element.
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// The data element is available but not required.
        /// </summary>
        Optional = 1,
        /// <summary>
        /// The data element is available and required.
        /// </summary>
        Required = 2,
        /// <summary>
        /// The data element is not available.
        /// </summary>
        Unavailable = 3
    }

    /// <summary>
    /// A data field that stores the level of necessity and availability associated with an item on a data entry form.
    /// </summary>
    /// <summary>
    /// Field Type used to display a dropdown list of RequirementLevels
    /// </summary>
    [Serializable]
    public class RequirementLevelFieldType : EnumFieldType<RequirementLevelSpecifier>
    {
        public RequirementLevelFieldType()
        {
            var values = new Dictionary<RequirementLevelSpecifier, string>();

            values.Add( RequirementLevelSpecifier.Optional, "Optional" );
            values.Add( RequirementLevelSpecifier.Required, "Required" );
            values.Add( RequirementLevelSpecifier.Unavailable, "Hidden" );

            base.SetAvailableValues( values );
        }
    }
}
