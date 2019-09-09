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

        public virtual bool IsValid
        {
            get
            {
                return !Required || RequiredFieldValidator == null || RequiredFieldValidator.IsValid;
            }
        }

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

        public HelpBlock HelpBlock { get; set; }

        public WarningBlock WarningBlock { get; set; }

        public RequiredFieldValidator RequiredFieldValidator { get; set; }

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

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            // After adding a new one this will post back so we should re-load the defined value list so the new one is included.
            EnsureChildControls();
            //LoadDefinedValues();
        }

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

        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( this.Visible )
            {
                RockControlHelper.RenderControl( this, writer );
            }
        }

        private DefinedValueEditor _definedValueEditor;
        private RockDropDownList _ddlDefinedValues;
        private LinkButton _lbAddDefinedValue;

        public int? DefinedTypeId { get; set; }
        public bool DisplayDescriptions { get; set; }
        public bool IncludeInactive { get; set; }
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
                ViewState["SelectedDefinedValueId"] = value;
            }
        }

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
                int parsedInt = 0;
                int.TryParse( ViewState["SelectedDefinedValueId"].ToStringSafe(), out parsedInt );
                return new int[] { parsedInt };
            }
        }

        public string SelectedValue
        {
            get
            {
                return _ddlDefinedValues.SelectedValue;
            }
        }

        public bool AllowAddingNewValues { get; set; }

        public bool IncludeEmptyOption { get; set; }

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
            _lbAddDefinedValue.OnClientClick = $"javascript:$('#{_definedValueEditor.ClientID}').fadeToggle(); return false;";
            _lbAddDefinedValue.Controls.Add( new HtmlGenericControl { InnerHtml = "<i class='fa fa-plus'></i>" } );
            Controls.Add( _lbAddDefinedValue );

            LoadDefinedValues();
        }

        public void LoadDefinedValues( int selectedDefinedValueId )
        {
            LoadDefinedValues();
        }

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

        protected void ddlDefinedValues_SelectedIndexChanged( object sender, EventArgs e )
        {
            SelectedDefinedValueId = ( ( RockDropDownList ) sender ).SelectedValue.AsIntegerOrNull();
        }

        protected void lbAddDefinedValue_Click( object sender, EventArgs e )
        {
            AddClick?.Invoke( sender, e );
        }

        protected void lbAddDefinedValue_ClientClick( object sender, EventArgs e )
        {
            AddClientClick?.Invoke( sender, e );
        }

        public event EventHandler AddClick;
        public event EventHandler AddClientClick;
    }

    public interface IDefinedValuePickerWithAdd
    {
        int[] SelectedDefinedValuesId { get; }
        void LoadDefinedValues();
        void LoadDefinedValues( int selectedDefinedValueId );

    }
}
