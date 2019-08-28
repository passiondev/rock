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
using System.Linq;
using System.Web.UI.WebControls;
using Rock.Field.Types;

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// Allows user selection of one or more parts of a postal address.
    /// </summary>
    public class AddressPartsPicker : RockCheckBoxList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddressPartsPicker"/> class.
        /// </summary>
        public AddressPartsPicker()
            : base()
        {
            Label = "Address Parts";

            this.Items.Clear();

            var parts = Enum.GetValues( typeof( PostalAddressPartSpecifier ) ).OfType<PostalAddressPartSpecifier>().ToList();

            foreach ( var part in parts )
            {
                this.Items.Add( new ListItem( part.ConvertToString(), part.ConvertToInt().ToString() ) );
            }
        }

        /// <summary>
        /// Gets the selected days of the week
        /// </summary>
        /// <value>
        /// The selected days of the week
        /// </value>
        public List<PostalAddressPartSpecifier> SelectedAddressParts
        {
            get
            {
                return this.Items.OfType<ListItem>().Where( l => l.Selected ).Select( a => ( PostalAddressPartSpecifier ) int.Parse( a.Value ) ).ToList();
            }

            set
            {
                foreach ( ListItem item in this.Items )
                {
                    item.Selected = value.Exists( a => a.Equals( ( PostalAddressPartSpecifier ) int.Parse( item.Value ) ) );
                }
            }
        }

    }
}