using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock.Security;
using Rock.Web.Cache;

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// A multiple DefinedValuePicker control that allows a defined value to be added on the fly.
    /// </summary>
    /// <seealso cref="System.Web.UI.WebControls.CompositeControl" />
    /// <seealso cref="Rock.Web.UI.Controls.IRockControl" />
    /// <seealso cref="Rock.Web.UI.Controls.IDefinedValuePickerWithAdd" />
    public class DefinedValuePickerWithAddMultipleSelect : DefinedValuePickerWithAdd
    {
        /// <summary>
        /// This is where you implement the simple aspects of rendering your control.  The rest
        /// will be handled by calling RenderControlHelper's RenderControl() method.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public override void RenderBaseControl( HtmlTextWriter writer )
        {
            writer.AddAttribute( "id", this.ClientID.ToString() );
            writer.AddAttribute( "class", this.CssClass );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            // Defined Value selector with Add button
            writer.AddAttribute( "class", $"{this.ClientID}-js-defined-value-selector controls controls-row form-control-group checkboxlist-group" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            _cblDefinedValues.RenderControl( writer );

            // Only render the Add button if the user is authorized to edit the defined type
            var definedType = DefinedTypeCache.Get( DefinedTypeId.Value );
            if ( definedType.IsAuthorized( Authorization.EDIT, ( ( RockPage ) Page ).CurrentPerson ) && IsAllowAddDefinedValue )
            {
                LinkButtonAddDefinedValue.RenderControl( writer );
            }

            writer.RenderEndTag();

            // Defined Value Editor
            DefinedValueEditorControl.RenderControl( writer );

            // picker div end tag
            writer.RenderEndTag();
        }

        #region IDefinedValuePickerWtihAdd Implementation

        /// <summary>
        /// Gets the selected defined values identifier.
        /// The field type uses this value for GetEditValue(). This is so all the DefinedValue pickers can share a field type.
        /// </summary>
        /// <value>
        /// Returns the SelectedDefinedValueId in an array.
        /// </value>
        public override int[] SelectedDefinedValuesId
        {
            get
            {
                var selectedDefinedValuesId = new List<int>();

                string selectedids = ViewState["SelectedDefinedValuesId"].ToStringSafe();
                if ( selectedids.IsNotNullOrWhiteSpace() )
                {
                    string[] ids = selectedids.Split( ',' );
                    foreach ( string id in ids )
                    {
                        int parsedint;
                        if ( int.TryParse( id, out parsedint ) )
                        {
                            selectedDefinedValuesId.Add( parsedint );
                        }
                    }
                }

                return selectedDefinedValuesId.ToArray();
            }

            set
            {
                if ( value == null )
                {
                    ViewState["SelectedDefinedValuesId"] = string.Empty;
                }
                else
                {
                    var selectedDefinedValuesId = new List<int>();

                    // check each value in the array to make sure it is valid and add it to the list
                    foreach ( int selectedId in value )
                    {
                        if ( DefinedValueCache.Get( selectedId ) != null )
                        {
                            selectedDefinedValuesId.Add( selectedId );
                        }
                    }

                    // join the list into a csv and save to ViewState["SelectedDefinedValueId"]
                    ViewState["SelectedDefinedValuesId"] = string.Join( ",", selectedDefinedValuesId );
                }

                LoadDefinedValues();
            }
        }

        /// <summary>
        /// Loads the defined values.
        /// </summary>
        public override void LoadDefinedValues()
        {
            _cblDefinedValues.Items.Clear();

            if ( DefinedTypeId.HasValue )
            {
                if ( IncludeEmptyOption )
                {
                    // add Empty option first
                    _cblDefinedValues.Items.Add( new ListItem() );
                }

                var definedTypeCache = DefinedTypeCache.Get( DefinedTypeId.Value );
                var definedValuesList = definedTypeCache?.DefinedValues
                    .Where( a => a.IsActive || IncludeInactive || SelectedDefinedValuesId.Contains( a.Id ) )
                    .OrderBy( v => v.Order )
                    .ThenBy( v => v.Value )
                    .ToList();

                if ( definedValuesList != null && definedValuesList.Any() )
                {
                    foreach ( var definedValue in definedValuesList )
                    {
                        _cblDefinedValues.Items.Add(
                            new ListItem
                            {
                                Text = DisplayDescriptions ? definedValue.Description : definedValue.Value,
                                Value = definedValue.Id.ToString(),
                                Selected = SelectedDefinedValuesId.Contains( definedValue.Id )
                            } );
                    }
                }
            }
        }

        #endregion IDefinedValuePickerWtihAdd Implementation

        private RockCheckBoxList _cblDefinedValues;

        /// <summary>
        /// Gets or sets the repeat direction.
        /// </summary>
        /// <value>
        /// The repeat direction.
        /// </value>
        public RepeatDirection RepeatDirection { get; set; } = System.Web.UI.WebControls.RepeatDirection.Horizontal;

        /// <summary>
        /// Gets or sets the number of columns the checkbox will use.
        /// </summary>
        /// <value>
        /// The repeat columns.
        /// </value>
        public int RepeatColumns { get; set; } = 4;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            // After adding a new value this will post back so we should re-load the defined value list so the new one is included.
            EnsureChildControls();
        }

        /// <summary>
        /// Outputs server control content to a provided <see cref="T:System.Web.UI.HtmlTextWriter" /> object and stores tracing information about the control if tracing is enabled.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( this.Visible )
            {
                RockControlHelper.RenderControl( this, writer );
            }
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            _cblDefinedValues = new RockCheckBoxList();
            _cblDefinedValues.ID = this.ID + "_cblDefinedValues";
            _cblDefinedValues.Style.Add( "width", "85%" );
            _cblDefinedValues.RepeatColumns = this.RepeatColumns;
            _cblDefinedValues.RepeatDirection = this.RepeatDirection;
            _cblDefinedValues.AutoPostBack = true;
            _cblDefinedValues.SelectedIndexChanged += cblDefinedValues_SelectedIndexChanged;
            Controls.Add( _cblDefinedValues );

            LinkButtonAddDefinedValue = new LinkButton();
            LinkButtonAddDefinedValue.ID = this.ID + "_lbAddDefinedValue";
            LinkButtonAddDefinedValue.Text = "Add Item";
            LinkButtonAddDefinedValue.CssClass = "btn btn-default btn-link js-button-add-defined-value";
            LinkButtonAddDefinedValue.OnClientClick = $"javascript:$('.{this.ClientID}-js-defined-value-selector').fadeToggle(400, 'swing', function() {{ $('#{DefinedValueEditorControl.ClientID}').fadeToggle(); }});  return false;";
            Controls.Add( LinkButtonAddDefinedValue );

            LoadDefinedValues();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblDefinedValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void cblDefinedValues_SelectedIndexChanged( object sender, EventArgs e )
        {
            SelectedDefinedValuesId = ( ( RockCheckBoxList ) sender )
                .Items.OfType<ListItem>()
                .Where( a => a.Selected )
                .Select( a => a.Value )
                .AsIntegerList()
                .ToArray();
        }
    }
}
