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
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

using Rock.Model;
using Rock.Reporting;
using Rock.Web.UI.Controls;

namespace Rock.Field.Types
{
    /// <summary>
    /// Enumerates the components of a postal address.
    /// </summary>
    public enum PostalAddressPartSpecifier
    {
        AddressLine1 = 0,
        AddressLine2 = 1,
        CityOrTown = 2,
        LocalityOrCounty = 3,
        StateOrRegion = 4,
        PostalCode = 5
    }

    /// <summary>
    /// A data field that stores a selection of postal address parts.
    /// </summary>
    public class AddressPartsFieldType : FieldType
    {
        #region Formatting

        /// <summary>
        /// Returns the field's current value(s)
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">Information about the value</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">Flag indicating if the value should be condensed (i.e. for use in a grid column)</param>
        /// <returns></returns>
        public override string FormatValue( Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            string formattedValue = string.Empty;

            if ( !string.IsNullOrWhiteSpace( value ) )
            {
                var addressParts = value.Split( ',' ).Select( a => ( PostalAddressPartSpecifier ) ( a.AsInteger() ) ).ToList();

                var addressPartNames = addressParts.Select( a => a.ConvertToString() ).ToList();

                return addressPartNames.AsDelimited( ", " );
            }

            return base.FormatValue( parentControl, formattedValue, null, condensed );
        }

        #endregion

        #region EditControl

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id"></param>
        /// <returns>
        /// The control
        /// </returns>
        public override Control EditControl( Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            return new AddressPartsPicker { ID = id };
        }

        /// <summary>
        /// Reads new values entered by the user for the field
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            var addressPartsPicker = control as AddressPartsPicker;

            if ( addressPartsPicker != null )
            {
                return addressPartsPicker.SelectedAddressParts.Select( a => a.ConvertToInt().ToString() ).ToList().AsDelimited( "," );
            }

            return null;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The value.</param>
        public override void SetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            var addressPartsPicker = control as AddressPartsPicker;

            if ( addressPartsPicker != null )
            {
                var selectedAddressParts = ( value ?? string.Empty ).SplitDelimitedValues().AsIntegerList().Select( a => ( PostalAddressPartSpecifier ) a ).ToList();

                addressPartsPicker.SelectedAddressParts = selectedAddressParts;
            }
        }

        #endregion 

        #region Filter Control

        /// <summary>
        /// Gets the type of the filter comparison.
        /// </summary>
        /// <value>
        /// The type of the filter comparison.
        /// </value>
        public override ComparisonType FilterComparisonType
        {
            get
            {
                return ComparisonHelper.ContainsFilterComparisonTypes;
            }
        }

        /// <summary>
        /// Formats the filter value value.
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override string FormatFilterValueValue( Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            string formattedValue = string.Empty;

            if ( !string.IsNullOrWhiteSpace( value ) )
            {
                var addressParts = value.Split( ',' ).Select( a => ( PostalAddressPartSpecifier ) ( a.AsInteger() ) ).ToList();

                var partNames = addressParts.Select( a => a.ConvertToString() ).ToList();

                return AddQuotes( partNames.AsDelimited( "' AND '" ) );
            }

            return string.Empty;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Get a serialized representation of a value for this field type.
        /// </summary>
        /// <param name="addressParts"></param>
        /// <returns></returns>
        public string GetSerializedValue( IEnumerable<PostalAddressPartSpecifier> addressParts )
        {
            return addressParts.Select( a => a.ConvertToInt().ToString() ).ToList().AsDelimited( "," );
        }

        /// <summary>
        /// Get a value for this field type from a serialized representation, or return the specified default value.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="defaultAddressParts"></param>
        /// <returns></returns>
        public List<PostalAddressPartSpecifier> GetDeserializedValue( string text, List<PostalAddressPartSpecifier> defaultAddressParts = null )
        {
            var selectedAddressParts = ( text ?? string.Empty ).SplitDelimitedValues().AsIntegerList().Select( a => ( PostalAddressPartSpecifier ) a ).ToList();

            return selectedAddressParts;
        }

        #endregion
    }
}
