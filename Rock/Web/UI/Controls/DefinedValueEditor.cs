using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Web.UI.Controls
{
    class DefinedValueEditor : CompositeControl
    {
        protected HiddenField _hfDefinedValueId;
        protected ValidationSummary _valSummaryValue;
        protected Literal _lActionTitleDefinedValue;
        protected DataTextBox _tbValueName;
        protected DataTextBox _tbValueDescription;
        protected AttributeValuesContainer _avcDefinedValueAttributes;
        protected LinkButton _btnSave;
        protected LinkButton _btnCancel;

        public int DefinedTypeId
        {
            get
            {
                int parsedInt = 0;
                int.TryParse( ViewState["DefinedTypeId"].ToStringSafe(), out parsedInt );
                return parsedInt;
            }

            set
            {
                ViewState["DefinedTypeId"] = value;
            }
        }

        public Guid DefinedTypeGuid
        {
            get
            {
                string guid = ViewState["DefinedTypeGuid"] as string;
                if ( guid == null )
                {
                    if ( DefinedTypeId != 0 )
                    {
                        DefinedTypeGuid = DefinedTypeCache.Get( DefinedTypeId ).Guid;
                        return DefinedTypeGuid;
                    }

                    return Guid.Empty;
                }

                return new Guid( guid );
            }

            set
            {
                ViewState["DefinedTypeGuid"] = value.ToString();
            }
        }

        public int DefinedValueId
        {
            get
            {
                int parsedInt = 0;
                int.TryParse( ViewState["DefinedValueId"].ToStringSafe(), out parsedInt );

                return parsedInt;
            }

            set
            {
                ViewState["DefinedValueId"] = value.ToString();
            }
        }

        public Guid DefinedValueGuid
        {
            get
            {
                string guid = ViewState["DefinedValueGuid"] as string;
                if ( guid == null )
                {
                    return Guid.Empty;
                }

                return new Guid( guid );
            }

            set
            {
                ViewState["DefinedValueGuid"] = value.ToString();
            }
        }

        public string Name
        {
            get
            {
                return ViewState["Name"].ToString();
            }

            set
            {
                ViewState["Name"] = value;
                _tbValueName.Text = value;
            }
        }

        public string Description
        {
            get
            {
                return ViewState["Description"].ToString();
            }

            set
            {
                ViewState["Description"] = value;
                _tbValueDescription.Text = value;
            }
        }

        public bool Hidden { get; set; }

        #region Overridden Control Methods

        protected override void OnInit( EventArgs e )
        {

            base.OnInit( e );
        }

        protected override void LoadViewState( object savedState )
        {

            base.LoadViewState( savedState );
        }

        protected override object SaveViewState()
        {
            return base.SaveViewState();
        }

        protected override void CreateChildControls()
        {
            Controls.Clear();

            _hfDefinedValueId = new HiddenField();
            _hfDefinedValueId.ID = this.ID + "_hfDefinedValueId";
            Controls.Add( _hfDefinedValueId );

            _valSummaryValue = new ValidationSummary();
            _valSummaryValue.ID = this.ID + "_valSummaryValue ";
            _valSummaryValue.AddCssClass( "alert alert-validation" );
            _valSummaryValue.ValidationGroup = "Value";
            _valSummaryValue.HeaderText = "Please correct the following:";
            Controls.Add( _valSummaryValue );

            _lActionTitleDefinedValue = new Literal();
            _lActionTitleDefinedValue.ID = this.ID + "_lActionTitleDefinedValue";
            Controls.Add( _lActionTitleDefinedValue );

            _tbValueName = new DataTextBox();
            _tbValueName.ID = this.ID + "_tbValueName";
            _tbValueName.SourceTypeName = "Rock.Model.DefinedValue, Rock";
            _tbValueName.PropertyName = "Value";
            _tbValueName.ValidationGroup = "Value";
            _tbValueName.Label = "Value";
            Controls.Add( _tbValueName );

            _tbValueDescription = new DataTextBox();
            _tbValueDescription.ID = this.ID + "_tbValueDescription";
            _tbValueDescription.SourceTypeName = "Rock.Model.DefinedValue, Rock";
            _tbValueDescription.PropertyName = "Description";
            _tbValueDescription.ValidationGroup = "Value";
            _tbValueDescription.TextMode = TextBoxMode.MultiLine;
            _tbValueDescription.Rows = 3;
            _tbValueDescription.ValidateRequestMode = ValidateRequestMode.Disabled;
            Controls.Add( _tbValueDescription );

            _avcDefinedValueAttributes = new AttributeValuesContainer();
            _avcDefinedValueAttributes.ID = this.ID + "_avcDefinedValueAttributes";
            Controls.Add( _avcDefinedValueAttributes );

            _btnSave = new LinkButton();
            _btnSave.ID = this.ID + "_btnSave";
            _btnSave.Text = "Add";
            _btnSave.CssClass = "btn btn-primary";
            _btnSave.Click += btnSave_Click;
            Controls.Add( _btnSave );

            _btnCancel = new LinkButton();
            _btnCancel.ID = this.ID + "_btnCancel";
            _btnCancel.Text = "Cancel";
            _btnCancel.CssClass = "btn btn-default";
            _btnCancel.CausesValidation = false;
            //_btnCancel.Click += btnCancel_Click;
            _btnCancel.OnClientClick = $"javascript:$('#{this.ClientID}').fadeToggle(); return false;";
            Controls.Add( _btnCancel );

            LoadDefinedValueAttributes();
        }

        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );
        }

        protected override void Render( HtmlTextWriter writer )
        {
            writer.AddAttribute( "id", this.ClientID.ToString() );
            if ( Hidden )
            {
                writer.AddStyleAttribute( "display", "none" );
            }

            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            // Validation Row
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            _valSummaryValue.RenderControl( writer );
            writer.RenderEndTag();

            // Title Row
            writer.RenderBeginTag( HtmlTextWriterTag.Legend );
            _lActionTitleDefinedValue.RenderControl( writer );
            writer.RenderEndTag(); // row 2

            // Start FieldSet
            writer.RenderBeginTag( HtmlTextWriterTag.Fieldset );

            // Name Description Row
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "row-fluid" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "span12" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            _tbValueName.RenderControl( writer );
            _tbValueDescription.RenderControl( writer );
            writer.RenderEndTag();
            writer.RenderEndTag();

            // Attributes
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "attributes" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            _avcDefinedValueAttributes.RenderControl( writer );
            writer.RenderEndTag();

            // End of FieldSet
            writer.RenderEndTag();

            // Buttons
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "row" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-md-6" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            _btnSave.RenderControl( writer );
            _btnCancel.RenderControl( writer );
            writer.RenderEndTag();

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "col-md-6" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            writer.RenderEndTag();

            writer.RenderEndTag(); // row

            // End control
            writer.RenderEndTag();
        }

        #endregion Overridden Control Methods

        public void LoadDefinedValueAttributes()
        {
            DefinedValue definedValue;
            var definedType = DefinedTypeCache.Get( DefinedTypeId );

            if ( DefinedValueId == 0 )
            {
                definedValue = new DefinedValue { Id = 0 };
                definedValue.DefinedTypeId = DefinedTypeId;
            }
            else
            {
                definedValue = new DefinedValueService( new RockContext() ).Get( DefinedValueId );
            }

            _hfDefinedValueId.SetValue( definedValue.Id );
            _tbValueName.Text = definedValue.Value;
            _tbValueDescription.Text = definedValue.Description;

            _avcDefinedValueAttributes.ValidationGroup = _valSummaryValue.ValidationGroup;
            _avcDefinedValueAttributes.AddEditControls( definedValue );
        }

        #region Events

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            DefinedValue definedValue;
            var rockContext = new RockContext();
            DefinedValueService definedValueService = new DefinedValueService( rockContext );

            if ( DefinedValueId.Equals( 0 ) )
            {
                definedValue = new DefinedValue { Id = 0 };
                definedValue.DefinedTypeId = DefinedTypeId;
                definedValue.IsSystem = false;

                var orders = definedValueService.Queryable()
                    .Where( d => d.DefinedTypeId == DefinedTypeId )
                    .Select( d => d.Order )
                    .ToList();

                definedValue.Order = orders.Any() ? orders.Max() + 1 : 0;
            }
            else
            {
                definedValue = definedValueService.Get( DefinedValueId );
            }

            definedValue.Value = _tbValueName.Text;
            definedValue.Description = _tbValueDescription.Text;
            _avcDefinedValueAttributes.GetEditValues( definedValue );

            if ( !Page.IsValid )
            {
                return;
            }

            if ( !definedValue.IsValid )
            {
                // Controls will render the error messages
                return;
            }

            rockContext.WrapTransaction( () =>
            {
                if ( definedValue.Id.Equals( 0 ) )
                {
                    definedValueService.Add( definedValue );
                }

                rockContext.SaveChanges();

                definedValue.SaveAttributeValues( rockContext );

            } );

            CreateChildControls();

            if ( this.Parent is IDefinedValuePickerWithAdd )
            {
                var picker = this.Parent as IDefinedValuePickerWithAdd;
                picker.LoadDefinedValues( definedValue.Id );
                
            }

        }

        ///// <summary>
        ///// Handles the Click event of the btnCancel control.
        ///// </summary>
        ///// <param name="sender">The source of the event.</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //protected void btnCancel_Click( object sender, EventArgs e )
        //{
        //    CancelClick?.Invoke( sender, e );
        //}

        ///// <summary>
        ///// Occurs when Save is clicked
        ///// </summary>
        //public event EventHandler SaveClick;

        ///// <summary>
        ///// Occurs when Cancel is clicked.
        ///// </summary>
        //public event EventHandler CancelClick;

        #endregion
    }
}
