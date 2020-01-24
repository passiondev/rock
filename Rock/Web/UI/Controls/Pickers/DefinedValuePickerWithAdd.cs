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
    /// A DefinedValuePicker control that allows a defined value to be added on the fly.
    /// </summary>
    /// <seealso cref="System.Web.UI.WebControls.CompositeControl" />
    /// <seealso cref="Rock.Web.UI.Controls.IRockControl" />
    /// <seealso cref="Rock.Web.UI.Controls.IDefinedValuePickerWithAdd" />
    public class DefinedValuePickerWithAdd : CompositeControl, IRockControl, IDefinedValuePickerWithAdd
    {
        #region IRockControl Implementation

        /// <summary>
        /// Gets or sets the label text.
        /// </summary>
        /// <value>
        /// The label text.
        /// </value>
        [Bindable( true )]
        [Category( "Appearance" )]
        [DefaultValue( "" )]
        [Description( "The text for the label." )]
        public string Label
        {
            get
            {
                return ViewState["Label"] as string ?? string.Empty;
            }
            set
            {
                ViewState["Label"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the help text.
        /// </summary>
        /// <value>
        /// The help text.
        /// </value>
        [Bindable( true )]
        [Category( "Appearance" )]
        [DefaultValue( "" )]
        [Description( "The help block." )]
        public string Help
        {
            get
            {
                return HelpBlock != null ? HelpBlock.Text : string.Empty;
            }

            set
            {
                if ( HelpBlock != null )
                {
                    HelpBlock.Text = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the warning text.
        /// </summary>
        /// <value>
        /// The warning text.
        /// </value>
        [Bindable( true )]
        [Category( "Appearance" )]
        [DefaultValue( "" )]
        [Description( "The warning block." )]
        public string Warning
        {
            get
            {
                return WarningBlock != null ? WarningBlock.Text : string.Empty;
            }

            set
            {
                if ( WarningBlock != null )
                {
                    WarningBlock.Text = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RockTextBox"/> is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if required; otherwise, <c>false</c>.
        /// </value>
        [Bindable( true )]
        [Category( "Behavior" )]
        [DefaultValue( "false" )]
        [Description( "Is the value required?" )]
        public bool Required
        {
            get
            {
                return ViewState["Required"] as bool? ?? false;
            }

            set
            {
                ViewState["Required"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the required error message.  If blank, the LabelName name will be used
        /// </summary>
        /// <value>The required error message.</value>
        public string RequiredErrorMessage
        {
            get
            {
                return RequiredFieldValidator != null ? RequiredFieldValidator.ErrorMessage : string.Empty;
            }

            set
            {
                if ( RequiredFieldValidator != null )
                {
                    RequiredFieldValidator.ErrorMessage = value;
                }
            }
        }

        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public virtual bool IsValid
        {
            get
            {
                return !Required || RequiredFieldValidator == null || RequiredFieldValidator.IsValid;
            }
        }

        /// <summary>
        /// Gets or sets the form group class.
        /// </summary>
        /// <value>
        /// The form group class.
        /// </value>
        public string FormGroupCssClass
        {
            get
            {
                return ViewState["FormGroupCssClass"] as string ?? string.Empty;
            }

            set
            {
                ViewState["FormGroupCssClass"] = value;
            }
        }

        /// <summary>
        /// Gets the help block.
        /// </summary>
        /// <value>
        /// The help block.
        /// </value>
        public HelpBlock HelpBlock { get; set; }

        /// <summary>
        /// Gets the warning block.
        /// </summary>
        /// <value>
        /// The warning block.
        /// </value>
        public WarningBlock WarningBlock { get; set; }

        /// <summary>
        /// Gets the required field validator.
        /// </summary>
        /// <value>
        /// The required field validator.
        /// </value>
        public RequiredFieldValidator RequiredFieldValidator { get; set; }

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>
        /// The validation group.
        /// </value>
        public string ValidationGroup
        {
            get
            {
                return ViewState["ValidationGroup"] as string;
            }

            set
            {
                ViewState["ValidationGroup"] = value;
            }
        }
        
        /// <summary>
        /// This is where you implement the simple aspects of rendering your control.  The rest
        /// will be handled by calling RenderControlHelper's RenderControl() method.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public void RenderBaseControl( HtmlTextWriter writer )
        {
            writer.AddAttribute( "id", this.ClientID.ToString() );
            writer.AddAttribute( "class", this.CssClass );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            // Defined Value selector with Add button
            writer.AddAttribute( "class", "js-defined-value-selector controls controls-row form-control-group" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            _ddlDefinedValues.RenderControl( writer );

            // Only render the Add button if the user is authorized to edit the defined type
            var definedType = DefinedTypeCache.Get( DefinedTypeId.Value );
            if ( definedType.IsAuthorized( Authorization.EDIT, ( ( RockPage ) Page ).CurrentPerson ) )
            {
                _lbAddDefinedValue.RenderControl( writer );
            }

            writer.RenderEndTag();

            // Defined Value Editor
            _definedValueEditor.RenderControl( writer );

            // picker div end tag
            writer.RenderEndTag();
        }

        #endregion IRockControl Implementation

        #region IDefinedValuePickerWtihAdd Implementation

        /// <summary>
        /// Gets the selected defined values identifier.
        /// The field type uses this value for GetEditValue(). This is so all the DefinedValue pickers can share a field type.
        /// </summary>
        /// <value>
        /// Returns the SelectedDefinedValueId in an array.
        /// </value>
        public int[] SelectedDefinedValuesId
        {
            get
            {
                return new int[] { SelectedDefinedValueId.Value };
            }

            set
            {
                SelectedDefinedValueId = value == null || value.Length == 0 ? 0 : value[0];
            }
        }
        
        /// <summary>
        /// Loads the defined values.
        /// </summary>
        /// <param name="selectedDefinedValueIds">The selected defined value ids. Only the first element in the array is used for this single select control.</param>
        public void LoadDefinedValues( int[] selectedDefinedValueIds )
        {
            SelectedDefinedValueId = selectedDefinedValueIds[0];
            LoadDefinedValues();
        }

        /// <summary>
        /// Loads the defined values.
        /// </summary>
        public void LoadDefinedValues()
        {
            _ddlDefinedValues.Items.Clear();

            if ( DefinedTypeId.HasValue )
            {
                if ( IncludeEmptyOption )
                {
                    // add Empty option first
                    _ddlDefinedValues.Items.Add( new ListItem() );
                }

                var definedTypeCache = DefinedTypeCache.Get( DefinedTypeId.Value );
                var definedValuesList = definedTypeCache?.DefinedValues
                    .Where( a => a.IsActive || IncludeInactive || a.Id == SelectedDefinedValueId )
                    .OrderBy( v => v.Order )
                    .ThenBy( v => v.Value )
                    .ToList();

                if ( definedValuesList != null && definedValuesList.Any() )
                {
                    foreach ( var definedValue in definedValuesList )
                    {
                        _ddlDefinedValues.Items.Add(
                            new ListItem
                            {
                                Text = DisplayDescriptions ? definedValue.Description : definedValue.Value,
                                Value = definedValue.Id.ToString(),
                                Selected = definedValue.Id == SelectedDefinedValueId
                            } );
                    }
                }
            }
        }

        #endregion IDefinedValuePickerWtihAdd Implementation

        private DefinedValueEditor _definedValueEditor;
        private RockDropDownList _ddlDefinedValues;
        private LinkButton _lbAddDefinedValue;

        /// <summary>
        /// Gets or sets the defined type identifier.
        /// </summary>
        /// <value>
        /// The defined type identifier.
        /// </value>
        public int? DefinedTypeId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [display descriptions].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [display descriptions]; otherwise, <c>false</c>.
        /// </value>
        public bool DisplayDescriptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include inactive].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [include inactive]; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeInactive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow adding new values].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [allow adding new values]; otherwise, <c>false</c>.
        /// </value>
        public bool AllowAddingNewValues { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include empty option].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [include empty option]; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeEmptyOption { get; set; }

        /// <summary>
        /// Gets or sets the selected defined value identifier.
        /// </summary>
        /// <value>
        /// The selected defined value identifier.
        /// </value>
        public int? SelectedDefinedValueId
        {
            get
            {
                int parsedInt = 0;
                int.TryParse( ViewState["SelectedDefinedValueId"].ToStringSafe(), out parsedInt );

                return parsedInt;
            }

            set
            {
                ViewState["SelectedDefinedValueId"] = value == null ? string.Empty : value.ToString();

                LoadDefinedValues();
            }
        }

        /// <summary>
        /// Gets the selected value.
        /// </summary>
        /// <value>
        /// The selected value.
        /// </value>
        public string SelectedValue
        {
            get
            {
                return DefinedValueCache.GetValue( SelectedDefinedValueId );
            }
        }

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
            Controls.Clear();
            RockControlHelper.CreateChildControls( this, Controls );

            _ddlDefinedValues = new RockDropDownList();
            _ddlDefinedValues.ID = this.ID + "_ddlDefinedValues";
            _ddlDefinedValues.Style.Add( "width", "85%" );
            _ddlDefinedValues.SelectedIndexChanged += ddlDefinedValues_SelectedIndexChanged;
            _ddlDefinedValues.AutoPostBack = true;
            Controls.Add( _ddlDefinedValues );

            _definedValueEditor = new DefinedValueEditor();
            _definedValueEditor.ID = this.ID + "_definedValueEditor";
            _definedValueEditor.Hidden = true;
            _definedValueEditor.DefinedTypeId = DefinedTypeId.Value;
            Controls.Add( _definedValueEditor );

            _lbAddDefinedValue = new LinkButton();
            _lbAddDefinedValue.ID = this.ID + "_lbAddDefinedValue";
            _lbAddDefinedValue.CssClass = "btn btn-default btn-square js-button-add-defined-value";
            _lbAddDefinedValue.OnClientClick = $"javascript:$('#{_definedValueEditor.ClientID}').fadeToggle(); $('.js-defined-value-selector').fadeToggle(); return false;";
            _lbAddDefinedValue.Controls.Add( new HtmlGenericControl { InnerHtml = "<i class='fa fa-plus'></i>" } );
            Controls.Add( _lbAddDefinedValue );

            LoadDefinedValues();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlDefinedValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlDefinedValues_SelectedIndexChanged( object sender, EventArgs e )
        {
            SelectedDefinedValueId = ( ( RockDropDownList ) sender ).SelectedValue.AsIntegerOrNull();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IDefinedValuePickerWithAdd
    {
        /// <summary>
        /// Gets the selected defined values identifier.
        /// </summary>
        /// <value>
        /// The selected defined values identifier.
        /// </value>
        int[] SelectedDefinedValuesId { get; set; }

        /// <summary>
        /// Loads the defined values.
        /// </summary>
        void LoadDefinedValues();

        /// <summary>
        /// Loads the defined values.
        /// </summary>
        /// <param name="selectedDefinedValueIds">The selected defined value ids.</param>
        void LoadDefinedValues( int[] selectedDefinedValueIds );

    }
}
