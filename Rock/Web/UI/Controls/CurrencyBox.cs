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
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock.Web.Cache;

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// An implementation of a <see cref="T:System.Web.UI.WebControls.TextBox"/> control for displaying and editing currency values.
    /// </summary>
    [ToolboxData( "<{0}:CurrencyBox runat=server></{0}:CurrencyBox>" )]
    public class CurrencyBox : RockTextBox
    {
        private RegularExpressionValidator _RegexValidator;
        private string _CurrencySymbol = "$";
        private string _CurrencyDecimalSeparator = ".";
        private string _CurrencyGroupSeparator = ",";

        /// <summary>
        /// Gets or sets the name of the field (for range validation messages when Label is not provided)
        /// </summary>
        /// <value>
        /// The name of the field.
        /// </value>
        public string FieldName
        {
            get { return ViewState["FieldName"] as string ?? Label; }
            set { ViewState["FieldName"] = value; }
        }

        /// <summary>
        /// Gets or sets the currency value.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        public decimal? Value
        {
            get
            {
                return this.Text.ReverseCurrencyFormatting().ToStringSafe().AsDecimalOrNull();
            }

            set
            {
                // Format the value as a fixed point decimal to 2 places.
                this.Text = value?.ToString( "F2" );
            }
        }

        /// <summary>
        /// Gets or sets the text content of the control.
        /// </summary>
        public override string Text
        {
            get { return base.Text; }

            set
            {
                // Format the value as a fixed point decimal to 2 places.
                base.Text = value.AsDecimalOrNull()?.ToString( "F2" );
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current value of this field is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public override bool IsValid
        {
            get
            {
                EnsureChildControls();

                return base.IsValid && _RegexValidator.IsValid;
            }
        }

        /// <summary>
        /// Gets or sets the group of controls for which the <see cref="T:System.Web.UI.WebControls.TextBox" /> control causes validation when it posts back to the server.
        /// </summary>
        /// <returns>The group of controls for which the <see cref="T:System.Web.UI.WebControls.TextBox" /> control causes validation when it posts back to the server. The default value is an empty string ("").</returns>
        public override string ValidationGroup
        {
            get
            {
                return base.ValidationGroup;
            }
            set
            {
                base.ValidationGroup = value;

                EnsureChildControls();

                if ( _RegexValidator != null )
                {
                    _RegexValidator.ValidationGroup = value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( System.EventArgs e )
        {
            base.OnInit( e );

            // Get the preferred currency symbol from the Rock global settings.
            var globalAttributes = GlobalAttributesCache.Get();

            if ( globalAttributes != null )
            {
                _CurrencySymbol = globalAttributes.GetValue( "CurrencySymbol" );

                if ( string.IsNullOrWhiteSpace( _CurrencySymbol ) )
                {
                    _CurrencySymbol = "$";
                }

                this.PrependText = _CurrencySymbol;
            }

            // Get the currency formatting options for the current thread culture.
            var numberFormat = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;

            _CurrencyDecimalSeparator = numberFormat.CurrencyDecimalSeparator;
            _CurrencyGroupSeparator = numberFormat.CurrencyGroupSeparator;
        }

        /// <summary>
        /// Renders the base control.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public override void RenderBaseControl( HtmlTextWriter writer )
        {
            // Add a script to limit the allowable keystrokes for currency data entry: number keys, currency format keys, and all shortcut Ctrl/Alt key combinations.
            // This won't prevent invalid text being copied/pasted into the field, so we need to rely on regular expression validation to catch the exceptions.
            var changeScript = @"
$('<controlId>').keydown( function (e)
{
    var key = e.which || e.keyCode;
    var validRegularCodes = [<regularKeyList>];
    var validSpecialCodes = [<specialKeyList>];
    if (e.shiftKey)
    {
        return (validSpecialCodes.indexOf(key) >= 0);
    }
    else if (e.altKey || e.ctrlKey)
    {
        return true;
    }
    else
    {
        return (validRegularCodes.indexOf(key) >= 0) || (validSpecialCodes.indexOf(key) >= 0);
    }
} );
";

            changeScript = changeScript.Replace( "<controlId>", ".js-currency-field input" );

            // Define Regular Keys, which are standard printable characters.
            var regularKeyCodes = new List<int>();

            // Add keys: Numbers 0-9, keyboard and keypad.
            for ( int i = 48; i <= 57; i++ )
            {
                regularKeyCodes.Add( i );
                regularKeyCodes.Add( i + 48 );
            }
            // Add keys: Comma, Period, keypad Period.
            regularKeyCodes.AddRange( new int[] { 110, 188, 190 } );

            changeScript = changeScript.Replace( "<regularKeyList>", regularKeyCodes.AsDelimited( "," ) );

            // Define Special Keys, which can be used alone or in combination with Shift/Alt/Ctrl.
            var specialKeyCodes = new List<int>();

            // Add keys: Backspace, Tab, Enter.
            specialKeyCodes.AddRange( new int[] { 8, 9, 13 } );
            // Add keys: Home, End.
            specialKeyCodes.AddRange( new int[] { 35, 36 } );
            // Add keys: Left Arrow, Right Arrow.
            specialKeyCodes.AddRange( new int[] { 37, 39 } );
            // Add keys: Insert, Delete.
            specialKeyCodes.AddRange( new int[] { 45, 46 } );

            changeScript = changeScript.Replace( "<specialKeyList>", specialKeyCodes.AsDelimited( "," ) );

            this.AddCssClass( "js-currency-field" );

            ScriptManager.RegisterStartupScript( this, this.GetType(), "CurrencyFieldFilterKeyDownScript", changeScript, true );

            base.RenderBaseControl( writer );
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            _RegexValidator = new RegularExpressionValidator();
            _RegexValidator.ID = this.ID + "_CurrencyRE";
            _RegexValidator.ControlToValidate = this.ID;
            _RegexValidator.Display = ValidatorDisplay.Dynamic;
            _RegexValidator.CssClass = "validation-error help-inline";
            _RegexValidator.Enabled = true;

            // Create a regular expression that includes culture-specific currency format characters.
            var regexExpression = @"^\<CS>?([1-9]{1}[0-9]{0,2}(\<GS>[0-9]{3})*(\<DS>[0-9]{0,2})?|[1-9]{1}[0-9]{0<GS>}(\<DS>[0-9]{0,2})?|0(\<DS>[0-9]{0,2})?|(\<DS>[0-9]{1,2})?)$";

            regexExpression = regexExpression.Replace( "<CS>", _CurrencySymbol );
            regexExpression = regexExpression.Replace( "<DS>", _CurrencyDecimalSeparator );
            regexExpression = regexExpression.Replace( "<GS>", _CurrencyGroupSeparator );

            _RegexValidator.ValidationExpression = regexExpression;

            Controls.Add( _RegexValidator );
        }

        /// <summary>
        /// Renders the data validators for this IRockControl instance.
        /// </summary>
        /// <param name="writer">The writer.</param>
        protected override void RenderDataValidator( HtmlTextWriter writer )
        {
            base.RenderDataValidator( writer );

            _RegexValidator.ValidationGroup = this.ValidationGroup;
            _RegexValidator.ErrorMessage = string.Format( "{0} is not a valid currency value.", string.IsNullOrWhiteSpace( FieldName ) ? "Value" : FieldName );

            _RegexValidator.RenderControl( writer );
        }
    }
}