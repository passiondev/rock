﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CommunicationEntryWizard.ascx.cs" Inherits="RockWeb.Blocks.Communication.CommunicationEntryWizard" %>

<asp:UpdatePanel ID="upnlContent" runat="server" ChildrenAsTriggers="true" UpdateMode="Conditional">
    <ContentTemplate>

        <style>
            /* always hide thead image remove */
            .propertypanel-image .imageupload-remove {
                display: none !important;
            }
        </style>


        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
        
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-comment"></i> New Communication</h1>

                <div class="panel-labels">
                    <div class="label label-default"><a href="#">Use Legacy Editor</a></div>
                </div>
            </div>
            <div class="panel-body">

                <%-- Recipient Selection --%>
                <asp:Panel ID="pnlRecipientSelection" runat="server" Visible="true">
                    <h4>Recipient Selection</h4>

                    <Rock:Toggle ID="tglRecipientSelection" runat="server" CssClass="btn-group-justified margin-b-lg" OnText="Select From List" OffText="Select Specific Individuals" Checked="true" OnCssClass="btn-primary" OffCssClass="btn-primary" ValidationGroup="vgRecipientSelection" OnCheckedChanged="tglRecipientSelection_CheckedChanged" />
                    
                    <asp:Panel ID="pnlRecipientSelectionList" runat="server">

                        <Rock:NotificationBox ID="nbCommunicationGroupWarning" runat="server" NotificationBoxType="Warning" Visible="false" />
                        <Rock:RockDropDownList ID="ddlCommunicationGroupList" runat="server" Label="List" CssClass="input-width-xxl" ValidationGroup="vgRecipientSelection" Required="true"  OnSelectedIndexChanged="ddlCommunicationGroupList_SelectedIndexChanged" AutoPostBack="true" />

                        
                        <asp:Panel ID="pnlCommunicationGroupSegments" runat="server">
                            <label>Segments</label>
                            <p>Optionally, further refine your recipients by filtering by segment.</p>
                            <asp:CheckBoxList ID="cblCommunicationGroupSegments" runat="server" RepeatDirection="Horizontal" CssClass="margin-b-lg" ValidationGroup="vgRecipientSelection" OnSelectedIndexChanged="cblCommunicationGroupSegments_SelectedIndexChanged" AutoPostBack="true" />

                            <Rock:RockRadioButtonList ID="rblCommunicationGroupSegmentFilterType" runat="server" Label="Recipients Must Meet" RepeatDirection="Horizontal" ValidationGroup="vgRecipientSelection" AutoPostBack="true" OnSelectedIndexChanged="rblCommunicationGroupSegmentFilterType_SelectedIndexChanged" />
                            
                            <div class="control-label">
                                <asp:Literal ID="lRecipientFromListCount" runat="server" Text="" />
                            </div>
                        </asp:Panel>
                    </asp:Panel> 
                    
                    <asp:Panel ID="pnlRecipientSelectionIndividual" runat="server">
                        <div class="row">
                            <div class="col-md-6">
                                <div class="control-label">
                                    <asp:Literal ID="lIndividualRecipientCount" runat="server" Text="" />
                                </div>
                                <asp:LinkButton ID="btnViewIndividualRecipients" runat="server" CssClass="btn btn-default" Text="View List" CausesValidation="false" OnClick="btnViewIndividualRecipients_Click" />
                            </div>
                            <div class="col-md-6">
                                <div class="pull-right">
                                    <Rock:PersonPicker ID="ppAddPerson" runat="server" CssClass="picker-menu-right" PersonName="Add Person" OnSelectPerson="ppAddPerson_SelectPerson" />
                                </div>
                            </div>
                        </div>
                        
                    </asp:Panel>

                    <div class="actions margin-t-md">
                        <asp:LinkButton ID="btnRecipientSelectionNext" runat="server" AccessKey="n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right" ValidationGroup="vgRecipientSelection" CausesValidation="true" OnClick="btnRecipientSelectionNext_Click" />
                    </div>

                    <%-- Recipient Selection: Individual Recipients Modal --%>
                    <Rock:ModalDialog Id="mdIndividualRecipients" runat="server" Title="Individual Recipients" ValidationGroup="mdIndividualRecipientsModal">
                        <Content>
                            <Rock:Grid ID="gIndividualRecipients" runat="server" OnRowDataBound="gIndividualRecipients_RowDataBound" HideDeleteButtonForIsSystem="false" ShowConfirmDeleteDialog="false">
                                <Columns>
                                    <Rock:SelectField></Rock:SelectField>
                                    <asp:BoundField DataField="NickName" HeaderText="First Name" SortExpression="NickName"  />
                                    <asp:BoundField DataField="LastName" HeaderText="Last Name" SortExpression="LastName" />
                                    <Rock:RockLiteralField ID="lRecipientAlert" HeaderText="Notes" />
                                    <Rock:RockLiteralField ID="lRecipientAlertEmail" HeaderText="Email" />
                                    <Rock:RockLiteralField ID="lRecipientAlertSMS" HeaderText="SMS" />
                                    <Rock:DeleteField OnClick="gIndividualRecipients_DeleteClick" />
                                </Columns>
                            </Rock:Grid>
                        </Content>
                    </Rock:ModalDialog>

                </asp:Panel>

                <%-- Medium Selection --%>
                <asp:Panel ID="pnlMediumSelection" runat="server" Visible="false" >
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbCommunicationName" runat="server" Label="Communication Name" Required="true" ValidationGroup="vgMediumSelection"/>
                        </div>
                        <div class="col-md-6">
                            <Rock:Toggle ID="tglBulkCommunication" runat="server" OnText="Yes" OffText="No" ActiveButtonCssClass="btn-primary" Help="Select this option if you are sending this email to a group of people. This will include the option for recipients to unsubscribe and will not send the email to any recipients that have already asked to be unsubscribed." Checked="false" Label="Is The Communication Bulk" />
                        </div>
                    </div>

                    <Rock:RockControlWrapper ID="rcwMediumType" runat="server" Label="Select the communication medium that you would like to send your message through.">
                        <div class="controls">
                            <div class="js-mediumtype">
                                <Rock:HiddenFieldWithClass ID="hfMediumType" CssClass="js-hidden-selected" runat="server" />
                                <div class="btn-group">
                                    <a id="btnMediumUserPreference" runat="server" class="btn btn-primary active js-medium-userpreference" data-val="0" >User Preference</a>
                                    <a id="btnMediumEmail" runat="server" class="btn btn-default" data-val="1" >Email</a>
                                    <a id="btnMediumSMS" runat="server" class="btn btn-default" data-val="2" >SMS</a>
                                </div>
                            </div>
                        </div>

                        <Rock:NotificationBox ID="nbUserPreferenceInfo" runat="server" CssClass="margin-t-md js-medium-userpreference-notification" NotificationBoxType="Info" Title="Heads Up!" Text="Selecting 'User Preference' will require adding content for all active mediums." />
                    </Rock:RockControlWrapper>

                    <div class="row margin-b-md">
                        <div class="col-md-6">
                            <Rock:NotificationBox ID="nbSendDateTimeWarning" runat="server" NotificationBoxType="Danger" Visible="false" />
                            <Rock:Toggle ID="tglSendDateTime" runat="server" OnText="Send Immediately" OffText="Send at a Specific Date and Time" ActiveButtonCssClass="btn-primary" Checked="false" OnCheckedChanged="tglSendDateTime_CheckedChanged" />
                            <Rock:DateTimePicker ID="dtpSendDateTime" runat="server" CssClass="margin-t-md" Visible="true" Required="true" ValidationGroup="vgMediumSelection" />
                        </div>
                        <div class="col-md-6">

                        </div>
                    </div>

                    <div class="actions">
                        <asp:LinkButton ID="btnMediumSelectionPrevious" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="btnMediumSelectionPrevious_Click"  />
                        <asp:LinkButton ID="btnMediumSelectionNext" runat="server" AccessKey="n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right" ValidationGroup="vgMediumSelection" CausesValidation="true" OnClick="btnMediumSelectionNext_Click" />
                    </div>

                </asp:Panel>

                <%-- Template Selection --%>
                <asp:Panel ID="pnlTemplateSelection" runat="server" Visible="false">
                    <h1>Email Template</h1>
                    <div class="row template-selection">
                        <asp:Repeater ID="rptSelectTemplate" runat="server" OnItemDataBound="rptSelectTemplate_ItemDataBound">
                            <ItemTemplate>
                                <div class="col-md-4">
                                    
                                    <asp:Panel Id="pnlTemplatePreview" runat="server" class="template-preview js-template-preview margin-b-md">
                                        <div class="row">
                                            <div class="col-md-5">
                                                <asp:Literal ID="lTemplateImagePreview" runat="server"></asp:Literal>
                                            </div>
                                            <div class="col-md-7">
                                                <label><asp:Literal ID="lTemplateName" runat="server"></asp:Literal></label>
                                                <p><asp:Literal ID="lTemplateDescription" runat="server"></asp:Literal></p>
                                            </div>
                                        </div>
                                        <div class="select-template margin-t-sm">
                                            <asp:LinkButton ID="btnSelectTemplate" runat="server" CssClass="btn btn-action btn-xs" OnClick="btnSelectTemplate_Click" Text="Select" />
                                        </div>
                                    </asp:Panel>
                                    
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <div class="actions margin-t-md">
                        <asp:LinkButton ID="btnTemplateSelectionPrevious" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="btnTemplateSelectionPrevious_Click"  />
                        <asp:LinkButton ID="btnTemplateSelectionNext" runat="server" AccessKey="n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right" ValidationGroup="vgTemplateSelection" CausesValidation="true" OnClick="btnTemplateSelectionNext_Click" />
                    </div>
                </asp:Panel>

                <asp:HiddenField ID="hfSelectedCommunicationTemplateId" runat="server" />
                <asp:HiddenField ID="hfEmailEditorHtml" runat="server" />
                <asp:HiddenField ID="hfEmailEditorHtml_dvrm" runat="server" Value="True" />

                <%-- Email Editor --%>
                <asp:Panel ID="pnlEmailEditor" runat="server" Visible="false">
                    <div class="emaileditor-wrapper">
                        <section id="emaileditor">
			                <div id="emaileditor-designer">
				                <iframe id="ifEmailDesigner" name="emaileditor-iframe" class="emaileditor-iframe js-emaileditor-iframe" runat="server" src="javascript: window.frameElement.getAttribute('srcdoc');" frameborder="0" border="0" cellspacing="0"></iframe>
			                </div>
			                <div id="emaileditor-properties">
				
				                <div class="emaileditor-propertypanels js-propertypanels">
					                <!-- Text/Html Properties -->
                                    <div class="propertypanel propertypanel-text" data-component="text" style="display: none;">
						                <h4 class="propertypanel-title">Text</h4>

                                        <div class="row">
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-text-backgroundcolor">Background Color</label>
									                <div id="component-text-backgroundcolor" class="input-group colorpicker-component">
										                <input type="text" value="" class="form-control" />
										                <span class="input-group-addon"><i></i></span>
									                </div>
								                </div>
                                                <Rock:RockDropDownList Id="ddlLineHeight" CssClass="js-component-text-lineheight" ClientIDMode="Static" runat="server" Label="Line Height">
                                                    <asp:ListItem />
                                                    <asp:ListItem Text="Normal" Value="100%" />
                                                    <asp:ListItem Text="Slight" Value="125%" />
                                                    <asp:ListItem Text="1 &frac12; spacing" Value="150%" />
                                                    <asp:ListItem Text="Double space" Value="200%" />
                                                    <asp:ListItem />
                                                </Rock:RockDropDownList>
							                </div>
							                <div class="col-md-6">
                                                <div class="row">
                                                    <div class="col-md-6">
                                                        <div class="form-group">
									                        <label for="component-text-margin-top">Margin Top</label>
                                                            <div class="input-group input-width-md">
								                                <input class="form-control" id="component-text-margin-top" type="number"><span class="input-group-addon">px</span>
							                                </div>
								                        </div>
                                                        <div class="form-group">
									                        <label for="component-text-margin-bottom">Margin Bottom</label>
									                        <div class="input-group input-width-md">
								                                <input class="form-control" id="component-text-margin-bottom" type="number"><span class="input-group-addon">px</span>
							                                </div>
								                        </div>
                                                    </div>
                                                    <div class="col-md-6">
                                                        <div class="form-group">
									                        <label for="component-text-margin-left">Margin Left</label>
									                        <div class="input-group input-width-md">
								                                <input class="form-control" id="component-text-margin-left" type="number"><span class="input-group-addon">px</span>
							                                </div>
								                        </div>
                                                        <div class="form-group">
									                        <label for="component-text-margin-right">Margin Right</label>
									                        <div class="input-group input-width-md">
								                                <input class="form-control" id="component-text-margin-right" type="number"><span class="input-group-addon">px</span>
							                                </div>
								                        </div>
                                                    </div>
                                                </div>
							                </div>
						                </div>

                                        <Rock:HtmlEditor ID="htmlEditor" CssClass="js-component-text-htmlEditor" runat="server" Height="350" CallbackOnChangeScript="updateTextComponent(this, contents);" />
					                </div>

                                    <!-- Image Properties -->
                                    <div class="propertypanel propertypanel-image" data-component="image" style="display: none;">
						                <h4 class="propertypanel-title">Image</h4>
						                <Rock:ImageUploader ID="componentImageUploader" ClientIDMode="Static" runat="server" Label="Image" UploadAsTemporary="false" DoneFunctionClientScript="handleImageUpdate(e, data)" DeleteFunctionClientScript="handleImageUpdate()" />

                                        <div class="row">
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-image-imgcsswidth">Width</label>
									                <select id="component-image-imgcsswidth" class="form-control">
										                <option value="0">Image Width</option>
										                <option value="1">Full Width</option>
									                </select>
								                </div>

                                                <div class="form-group">
									                <label for="component-image-imagealign">Align</label>
									                <select id="component-image-imagealign" class="form-control">
										                <option value="left">Left</option>
										                <option value="center">Center</option>
										                <option value="right">Right</option>
									                </select>
								                </div>
                                            
                                                <div class="form-group">
									                <label for="component-image-resizemode">Resize Mode</label>
									                <select id="component-image-resizemode" class="form-control">
										                <option value="crop">Crop</option>
										                <option value="pad">Pad</option>
										                <option value="stretch">Stretch</option>
									                </select>
								                </div>
							                </div>
							                <div class="col-md-6">
                                                <div class="row">
                                                    <div class="col-md-6">
                                                        <div class="form-group">
                                                            <label for="component-image-imagewidth">Image Width</label>
                                                            <div class="input-group input-width-md">
                                                                <input class="form-control" id="component-image-imagewidth" type="number"><span class="input-group-addon">px</span>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="col-md-6">
                                                        <div class="form-group">
                                                            <label for="component-image-imageheight">Image Height</label>
                                                            <div class="input-group input-width-md">
                                                                <input class="form-control" id="component-image-imageheight" type="number"><span class="input-group-addon">px</span>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>

                                                <div class="row">
                                                    <div class="col-md-6">
                                                        <div class="form-group">
									                        <label for="component-image-margin-top">Margin Top</label>
                                                            <div class="input-group input-width-md">
								                                <input class="form-control" id="component-image-margin-top" type="number"><span class="input-group-addon">px</span>
							                                </div>
								                        </div>
                                                        <div class="form-group">
									                        <label for="component-image-margin-bottom">Margin Bottom</label>
									                        <div class="input-group input-width-md">
								                                <input class="form-control" id="component-image-margin-bottom" type="number"><span class="input-group-addon">px</span>
							                                </div>
								                        </div>
                                                    </div>
                                                    <div class="col-md-6">
                                                        <div class="form-group">
									                        <label for="component-image-margin-left">Margin Left</label>
									                        <div class="input-group input-width-md">
								                                <input class="form-control" id="component-image-margin-left" type="number"><span class="input-group-addon">px</span>
							                                </div>
								                        </div>
                                                        <div class="form-group">
									                        <label for="component-image-margin-right">Margin Right</label>
									                        <div class="input-group input-width-md">
								                                <input class="form-control" id="component-image-margin-right" type="number"><span class="input-group-addon">px</span>
							                                </div>
								                        </div>
                                                    </div>
                                                </div>
							                </div>
						                </div>
					                </div>

                                    <!-- Section Properties -->
                                    <div class="propertypanel propertypanel-section" data-component="section" style="display: none;">
						                <h4 class="propertypanel-title">Section</h4>
					                </div>

                                    <!-- Divider Properties -->
                                    <div class="propertypanel propertypanel-divider" data-component="divider" style="display: none;">
						                <h4 class="propertypanel-title">Divider</h4>
                                        <div class="row">
							                <div class="col-md-6">
                                                <div class="form-group">
									                <label for="component-divider-height">Height</label>
                                                    <div class="input-group input-width-md">
								                        <input class="form-control" id="component-divider-height" type="number"><span class="input-group-addon">px</span>
							                        </div>
								                </div>
                                                <div class="form-group">
									                <label for="component-divider-color">Color</label>
									                <div id="component-divider-color" class="input-group colorpicker-component">
										                <input type="text" value="" class="form-control" />
										                <span class="input-group-addon"><i></i></span>
									                </div>
								                </div>
                                            </div>
                                            <div class="col-md-6">
                                                <div class="form-group">
									                <label for="component-divider-margin-top">Margin Top</label>
                                                    <div class="input-group input-width-md">
								                        <input class="form-control" id="component-divider-margin-top" type="number"><span class="input-group-addon">px</span>
							                        </div>
								                </div>
                                                <div class="form-group">
									                <label for="component-divider-margin-bottom">Margin Bottom</label>
									                <div class="input-group input-width-md">
								                        <input class="form-control" id="component-divider-margin-bottom" type="number"><span class="input-group-addon">px</span>
							                        </div>
								                </div>
                                            </div>
                                        </div>
					                </div>

                                    <!-- Code Properties -->
                                    <div class="propertypanel propertypanel-code" data-component="code" style="display: none;">
						                <h4 class="propertypanel-title">Code</h4>
                                        <Rock:CodeEditor ID="codeEditor" CssClass="js-component-code-codeEditor" runat="server" Height="350" EditorTheme="Rock" EditorMode="Html" OnChangeScript="updateCodeComponent(this, contents);" />
                                        <div class="alert alert-danger" id="component-code-codeEditor-error"  style="display:none"></div>
						

                                        <div class="row">
                                            <div class="col-md-6">
                                                <div class="form-group">
                                                    <label for="component-code-margin-top">Margin Top</label>
                                                    <div class="input-group input-width-md">
                                                        <input class="form-control" id="component-code-margin-top" type="number"><span class="input-group-addon">px</span>
                                                    </div>
                                                </div>
                                                <div class="form-group">
                                                    <label for="component-code-margin-bottom">Margin Bottom</label>
                                                    <div class="input-group input-width-md">
                                                        <input class="form-control" id="component-code-margin-bottom" type="number"><span class="input-group-addon">px</span>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-6">
                                                <div class="form-group">
                                                    <label for="component-code-margin-left">Margin Left</label>
                                                    <div class="input-group input-width-md">
                                                        <input class="form-control" id="component-code-margin-left" type="number"><span class="input-group-addon">px</span>
                                                    </div>
                                                </div>
                                                <div class="form-group">
                                                    <label for="component-code-margin-right">Margin Right</label>
                                                    <div class="input-group input-width-md">
                                                        <input class="form-control" id="component-code-margin-right" type="number"><span class="input-group-addon">px</span>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
					                </div>

					                <!-- Button Properties -->
                                    <div class="propertypanel propertypanel-button" data-component="button" style="display: none;">
						                <h4 class="propertypanel-title">Button</h4>
						                <hr />
						                <div class="form-group">
							                <label for="component-button-buttontext">Button Text</label>
							                <input class="form-control" id="component-button-buttontext" placeholder="Press Me">
						                </div>

						                <div class="form-group">
							                <label for="component-button-buttonurl">Url</label>
							                <div class="input-group">
								                <span class="input-group-addon"><i class="fa fa-link"></i></span>
								                <input class="form-control" id="component-button-buttonurl" placeholder="http://yourlink.com">
							                </div>
						                </div>

						                <div class="row">
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-button-buttonbackgroundcolor">Background Color</label>
									                <div id="component-button-buttonbackgroundcolor" class="input-group colorpicker-component">
										                <input type="text" value="" class="form-control" />
										                <span class="input-group-addon"><i></i></span>
									                </div>
								                </div>
							                </div>
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-button-buttonfontcolor">Font Color</label>
									                <div id="component-button-buttonfontcolor" class="input-group colorpicker-component">
										                <input type="text" value="" class="form-control" />
										                <span class="input-group-addon"><i></i></span>
									                </div>
								                </div>
							                </div>
						                </div>

						                <div class="row">
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-button-buttonwidth">Width</label>
									                <select id="component-button-buttonwidth" class="form-control">
										                <option value="0">Fit To Text</option>
										                <option value="1">Full Width</option>
									                </select>
								                </div>
							                </div>
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-button-buttonalign">Align</label>
									                <select id="component-button-buttonalign" class="form-control">
										                <option value="left">Left</option>
										                <option value="center">Center</option>
										                <option value="right">Right</option>
									                </select>
								                </div>
							                </div>
						                </div>

						                <div class="form-group">
							                <label for="component-button-buttofont">Font</label>
							                <select id="component-button-buttonfont" class="form-control">
								                <option value=""></option>
								                <option value="Arial, Helvetica, sans-serif">Arial</option>
								                <option value='"Arial Black", Gadget, sans-serif'>Arial Black</option>
								                <option value='"Courier New", Courier, monospace'>Courier New</option>
								                <option value="Georgia, serif">Georgia</option>
								                <option value="Helvetica, Arial, sans-serif">Helvetica</option>
								                <option value="Impact, Charcoal, sans-serif">Impact</option>
								                <option value='"Lucida Sans Unicode", "Lucida Grande", sans-serif'>Lucida</option>
								                <option value='"Lucida Console", Monaco, monospace'>Lucida Console</option>
								                <option value="Tahoma, Geneva, sans-serif">Tahoma</option>
								                <option value='Times New Roman", Times, serif'>Times New Roman</option>
								                <option value='Trebuchet MS", Helvetica, sans-serif'>Trebuchet MS</option>
								                <option value="Verdana, Geneva, sans-serif">Verdana</option>
							                </select>
						                </div>

						                <div class="row">
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-button-buttonfontweight">Font Weight</label>
									                <select id="component-button-buttonfontweight" class="form-control">
										                <option value="normal">Normal</option>
										                <option value="bold">Bold</option>
										                <option value="bolder">Bolder</option>
										                <option value="lighter">Lighter</option>
									                </select>
								                </div>
							                </div>
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-button-buttonfontsize">Font Size</label>
									                <input class="form-control" id="component-button-buttonfontsize">
								                </div>
							                </div>
						                </div>

						                <div class="row">
							                <div class="col-md-6">
								                <div class="form-group">
									                <label for="component-button-buttonpadding">Button Padding</label>
									                <input class="form-control" id="component-button-buttonpadding">
								                </div>
							                </div>
							                <div class="col-md-6">
							                </div>
						                </div>
					                </div>

                                    <div class="js-propertypanel-actions actions" style="display:none">
                                        <a href="#" class="btn btn-primary" onclick="clearPropertyPane(event)">Complete</a>
                                        <a href="#" class="btn btn-link" onclick="deleteCurrentComponent()">Delete</a>
                                    </div>
				                </div>

			                </div>
		                </section>
			
		                <div id="editor-controls" style="display: none;">
                            <div id="editor-toolbar-container" class="js-emaileditor-addon">
			                    <div id="editor-toolbar-content">
				                    <div class="component component-text" data-content="<h1>Big News</h1><p> This is a text block. You can use it to add text to your template.</p>" data-state="template">
					                    <i class="fa fa-align-justify"></i><br /> Text
				                    </div>
				                    <div class="component component-image" data-content="<img src='<%= VirtualPathUtility.ToAbsolute("~/Assets/Images/image-placeholder.jpg") %>' style='width: 100%;' data-width='full' />" data-state="template">
					                    <i class="fa fa-picture-o"></i> <br /> Image
				                    </div>
				                
				                    <div class="component component-divider" data-content="<hr style='margin-top: 0px; margin-bottom: 0px; border: 0; height: 4px; background: #c4c4c4;' />" data-state="template">
					                    <i class="fa fa-minus"></i> <br /> Divider
				                    </div>
				                    <div class="component component-code" data-content="Add your code here..." data-state="template">
					                    <i class="fa fa-code"></i> <br /> Code
				                    </div>
				                    <div class="component component-button" data-content="<table class='button-outerwrap' border='0' cellpadding='0' cellspacing='0' width='100%' style='min-width:100%;'><tbody><tr><td style='padding-top:0; padding-right:0; padding-bottom:0; padding-left:0;' valign='top' align='center' class='button-innerwrap'><table border='0' cellpadding='0' cellspacing='0' class='button-shell' style='display: inline-table; border-collapse: separate !important; border-radius: 3px; background-color: rgb(43, 170, 223);'><tbody><tr><td align='center' valign='middle' class='button-content' style='font-family: Arial; font-size: 16px; padding: 15px;'><a class='button-link' title='Push Me' href='http://' target='_blank' style='font-weight: bold; letter-spacing: normal; line-height: 100%; text-align: center; text-decoration: none; color: rgb(255, 255, 255);'>Push Me</a></td></tr></tbody></table></td></tr></tbody></table>" data-state="template">
					                    <i class="fa fa-square-o"></i> <br /> Button
				                    </div>
			                    </div>
                                <div id="editor-toolbar-structure">
                                    <div class="component component-section" data-content="<div class='dropzone'></div>" data-state="template">
					                    <i class="fa fa-columns"></i> <br /> Section_100
				                    </div>
                                    <div class="component component-section" data-content="<table width='100%'><tr><td width='50%'><div class='dropzone'></div></td><td width='50%'><div class='dropzone'></div></td></tr></table>" data-state="template">
					                    <i class="fa fa-columns"></i> <br /> Section_2x
				                    </div>
                                    <div class="component component-section" data-content="<table width='100%'><tr><td width='33%'><div class='dropzone'></div></td><td width='34%'><div class='dropzone'></div></td><td width='33%'><div class='dropzone'></div></td></tr></table>" data-state="template">
					                    <i class="fa fa-columns"></i> <br /> Section_3x
				                    </div>
                                    <div class="component component-section" data-content="<table width='100%'><tr><td width='25%'><div class='dropzone'></div></td><td width='25%'><div class='dropzone'></div></td><td width='25%'><div class='dropzone'></div></td><td width='25%'><div class='dropzone'></div></td></tr></table>" data-state="template">
					                    <i class="fa fa-columns"></i> <br /> Section_4x
				                    </div>
                                    <div class="component component-section" data-content="<table width='100%'><tr><td width='33%'><div class='dropzone'></div></td><td width='67%'><div class='dropzone'></div></td></tr></table>" data-state="template">
					                    <i class="fa fa-columns"></i> <br /> Section_1:2
				                    </div>
                                    <div class="component component-section" data-content="<table width='100%'><tr><td width='67%'><div class='dropzone'></div></td><td width='33%'><div class='dropzone'></div></td></tr></table>" data-state="template">
					                    <i class="fa fa-columns"></i> <br /> Section_2:1
				                    </div>
                                </div>
                            </div>
		                </div>	
                    </div>
                    
                    <%-- TODO: review css here... --%>
                    <div class="actions margin-t-lg">
                        <asp:LinkButton ID="btnEmailEditorPrevious" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="btnEmailEditorPrevious_Click" />
                        <asp:LinkButton ID="btnEmailEditorNext" runat="server" AccessKey="n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right" ValidationGroup="vgEmailEditor" CausesValidation="true" OnClick="btnEmailEditorNext_Click" />
                    </div>
                    
                </asp:Panel>

                <%-- Email Summary --%>
                <asp:Panel ID="pnlEmailSummary" runat="server" Visible="false">
                    <h4>Email Summary</h4>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbFromName" runat="server" Label="From Name" Required="true" ValidationGroup="vgEmailSummary" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbFromAddress" runat="server" Label="From Address" Required="true" ValidationGroup="vgEmailSummary"/>
                            <div class="pull-right">
                                <a href="#" class="btn btn-xs btn-link js-show-additional-fields" >Show Additional Fields</a>
                            </div>
                        </div>
                    </div>

                    <asp:Panel ID="pnlEmailSummaryAdditionalFields" runat="server" CssClass="js-additional-fields" style="display:none">
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbReplyToAddress" runat="server" Label="Reply To Address" />
                            </div>
                            <div class="col-md-6">
                                
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbCCList" runat="server" Label="CC List" />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbBCCList" runat="server" Label="BCC List" />
                            </div>
                        </div>
                    </asp:Panel>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbEmailSubject" runat="server" Label="Email Subject" Required="true" ValidationGroup="vgEmailSummary" />
                            <asp:UpdatePanel ID="upFileAttachments" runat="server">
                                <ContentTemplate>
                                    <asp:HiddenField ID="hfAttachedBinaryFileIds" runat="server" />
                                    <Rock:FileUploader Id="fupAttachments" runat="server" Label="Attachments" OnFileUploaded="fupAttachments_FileUploaded" />
                                    <asp:Literal ID="lAttachmentListHtml" runat="server" />
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </div>
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbEmailPreview" runat="server" Label="Email Preview" TextMode="MultiLine" Rows="4" />
                        </div>
                    </div>

                    <div class="actions">
                        <asp:LinkButton ID="btnEmailSummaryPrevious" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="btnEmailSummaryPrevious_Click" />
                        <asp:LinkButton ID="btnEmailSummaryNext" runat="server" AccessKey="n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right" ValidationGroup="vgEmailSummary" CausesValidation="true" OnClick="btnEmailSummaryNext_Click" />
                    </div>
                </asp:Panel>

                <%-- Mobile Text Editor --%>
                <asp:Panel ID="pnlMobileTextEditor" runat="server" Visible="false">
                    <h4>Mobile Text Editor</h4>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockDropDownList ID="ddlSMSFrom" runat="server" Label="From" Help="The number to originate message from (configured under Admin Tools > General Settings > Defined Types > SMS From Values)." Required="true" ValidationGroup="vgMobileTextEditor"/>
                            <Rock:RockControlWrapper ID="rcwSMSMessage" runat="server" Label="Message" Help="<span class='tip tip-lava'></span>">
                                <Rock:MergeFieldPicker ID="mfpSMSMessage" runat="server" CssClass="margin-b-sm pull-right" OnSelectItem="mfpMessage_SelectItem" ValidationGroup="vgMobileTextEditor"/>
                                <asp:HiddenField ID="hfSMSCharLimit" runat="server" />
                                <asp:Label ID="lblSMSMessageCount" runat="server" CssClass="badge margin-all-sm pull-right" />
                                <Rock:RockTextBox ID="tbSMSTextMessage" runat="server" TextMode="MultiLine" Rows="3" Required="true" ValidationGroup="vgMobileTextEditor"/>
                                <div class="actions margin-t-sm pull-right">
                                    <asp:Button ID="btnSMSSendTest" runat="server" CssClass="btn btn-sm btn-default" Text="Send Test" OnClick="btnSMSSendTest_Click" />
                                </div>
                            </Rock:RockControlWrapper>
                        </div>
                        <div class="col-md-6">
                            <%-- TODO: This is where the SMS Bubbles thing would probably go --%>
                            <asp:Label ID="lblSMSPreview" runat="server" CssClass="js-sms-preview" />
                        </div>
                    </div>
                    <div class="actions margin-t-md">
                        <asp:LinkButton ID="btnMobileTextEditorPrevious" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="btnMobileTextEditorPrevious_Click" />
                        <asp:LinkButton ID="btnMobileTextEditorNext" runat="server" AccessKey="n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right" ValidationGroup="vgMobileTextEditor" CausesValidation="true" OnClick="btnMobileTextEditorNext_Click" />
                    </div>
                </asp:Panel>

                <%-- Confirmation --%>
                <asp:Panel ID="pnlConfirmation" runat="server" Visible="false">
                    <h4>Confirmation</h4>
                    <div class="alert alert-info js-confirmation-senddatetime-alert">
                        <asp:Label ID="lConfirmationSendDateTimeHtml" runat="server" />
                        <a href='#' class="btn btn-link btn-xs js-show-confirmation-datetime"><strong>Edit</strong></a>
                    </div>
                    <asp:Label ID="lblConfirmationSendHtml" runat="server" />
                    <div class="row">
                        <div class="col-md-6">
                        </div>
                    </div>

                    <asp:HiddenField ID="hfShowConfirmationDateTime" runat="server" />
                    <div class="row margin-b-md js-confirmation-datetime" style="display:none">
                        <div class="col-md-6">
                            <Rock:NotificationBox ID="nbSendDateTimeWarningConfirmation" runat="server" NotificationBoxType="Danger" Visible="false" />
                            <Rock:Toggle ID="tglSendDateTimeConfirmation" runat="server" OnText="Send Immediately" OffText="Send at a Specific Date and Time" ActiveButtonCssClass="btn-primary" Checked="false" OnCheckedChanged="tglSendDateTimeConfirmation_CheckedChanged" />
                            <Rock:DateTimePicker ID="dtpSendDateTimeConfirmation" runat="server" CssClass="margin-t-md" Visible="true" Required="true" ValidationGroup="vgConfirmation" />
                        </div>
                        <div class="col-md-6">

                        </div>
                    </div>
                    
                    <div class="actions margin-b-lg">
                        <asp:LinkButton ID="btnSend" runat="server" Text="Send" CssClass="btn btn-primary" CausesValidation="true" ValidationGroup="vgConfirmation" OnClick="btnSend_Click" />
                        <asp:LinkButton ID="btnSaveAsDraft" runat="server" Text="Save as Draft" CssClass="btn btn-default" CausesValidation="true" ValidationGroup="vgConfirmation" OnClick="btnSaveAsDraft_Click" />
                        <asp:LinkButton ID="btnConfirmationCancel" runat="server" Text="Cancel" CssClass="btn btn-default" CausesValidation="false" ValidationGroup="vgConfirmation" OnClick="btnConfirmationCancel_Click" />
                    </div>
                    
                    <div class="actions">
                        <asp:LinkButton ID="btnConfirmationPrevious" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default" CausesValidation="false" OnClick="btnConfirmationPrevious_Click" />
                    </div>
                </asp:Panel>

            </div>
        
        </asp:Panel>


        <script>
            
            Sys.Application.add_load(function ()
            {
                if ($('#<%=pnlEmailEditor.ClientID%>').length) {
                    loadEmailEditor()
                }

                $('.js-mediumtype .btn').on('click', function (e)
                {
                    setActiveButtonGroupButton($(this));
                });

                setActiveButtonGroupButton($('.js-mediumtype').find("[data-val='" + $('.js-mediumtype .js-hidden-selected').val() + "']"));

                $('.js-show-additional-fields').off('click').on('click', function ()
                {
                    $('.js-additional-fields').slideToggle();
                });

                // Show the Send Date controls on the confirmation page if they click 'edit'
                $('.js-show-confirmation-datetime').off('click').on('click', function ()
                {
                    $('.js-confirmation-senddatetime-alert').slideUp();
                    $('.js-confirmation-datetime').slideDown();
                    $('#ctl00_main_ctl23_ctl01_ctl06_hfShowConfirmationDateTime').val("true");
                });
                
                // Ensure the visibility of of Send Date controls on the confirmation page if they clicked 'edit' and are navigating back and forth to it
                if ($('#<%=hfShowConfirmationDateTime.ClientID %>').val() == "true") {
                    $('.js-confirmation-senddatetime-alert').hide()
                    $('.js-confirmation-datetime').show();
                }
                
                var smsCharLimit = $('#<%=hfSMSCharLimit.ClientID%>').val();
                if ( smsCharLimit && smsCharLimit > 0)
                {
                    $('#<%=tbSMSTextMessage.ClientID%>').limit(
                        {
                            maxChars: smsCharLimit,
                            counter: '#<%=lblSMSMessageCount.ClientID%>',
                            normalClass: 'badge',
                            warningClass: 'badge-warning',
                            overLimitClass: 'badge-danger'
                        });
                }

                $('#<%=btnEmailEditorNext.ClientID%>').off('click').on('click', function ()
                {
                    var $editorIframe = $('#<%=ifEmailDesigner.ClientID%>');
                    var $editorHtml = $editorIframe.contents().find('HTML');

                    // remove all the email editor stuff 
                    $editorHtml.find('.js-emaileditor-addon').remove();

                    var emailHtmlContent = $editorHtml[0].outerHTML;

                    $('#<%=hfEmailEditorHtml.ClientID%>').val(emailHtmlContent);
                });
            }
            );

            function removeAttachment(source, hf, fileId)
            {
                // Get the attachment list
                var $hf = $('#' + hf);
                var fileIds = $hf.val().split(',');

                // Remove the selected attachment 
                var removeAt = $.inArray(fileId, fileIds);
                fileIds.splice(removeAt, 1);
                $hf.val(fileIds.join());

                // Remove parent <li>
                $(source).closest($('li')).remove();
            }

            function loadEmailEditor()
            {
                // load in editor styles and scripts
                var cssLink = document.createElement("link")
                cssLink.className = "js-emaileditor-addon";
                cssLink.href = '<%=RockPage.ResolveRockUrl("~/Themes/Rock/Styles/email-editor.css", true ) %>';
                cssLink.rel = "stylesheet";
                cssLink.type = "text/css";
                

                var fontAwesomeLink = document.createElement("link")
                fontAwesomeLink.className = "js-emaileditor-addon";
                fontAwesomeLink.href = '<%=RockPage.ResolveRockUrl("~/Themes/Rock/Styles/font-awesome.css", true ) %>';
                fontAwesomeLink.rel = "stylesheet";
                fontAwesomeLink.type = "text/css";

                var jqueryLoaderScript = document.createElement("script");
                jqueryLoaderScript.className = "js-emaileditor-addon";
                jqueryLoaderScript.type = "text/javascript";
                jqueryLoaderScript.src = '<%=RockPage.ResolveRockUrl("~/Scripts/jquery-1.12.4.min.js", true ) %>';

                var dragulaLoaderScript = document.createElement("script");
                dragulaLoaderScript.className = "js-emaileditor-addon";
                dragulaLoaderScript.type = "text/javascript";
                dragulaLoaderScript.src = '<%=RockPage.ResolveRockUrl("~/Scripts/dragula.min.js", true ) %>';

                var $editorIframe = $('.js-emaileditor-iframe');

                var editorScript = document.createElement("script");
                editorScript.className = "js-emaileditor-addon";
                editorScript.type = "text/javascript";
                editorScript.src = '<%=RockPage.ResolveRockUrl("~/Scripts/Rock/Controls/EmailEditor/email-editor.js", true ) %>';
                editorScript.onload = function ()
                {
                    $editorIframe[0].contentWindow.Rock.controls.emailEditor.initialize({
                        id: 'editor-window',
                        componentSelected: loadPropertiesPage
                    });
                };

                $editorIframe.load(function ()
                {
                    frames['emaileditor-iframe'].document.head.appendChild(jqueryLoaderScript);
                    frames['emaileditor-iframe'].document.head.appendChild(cssLink);
                    frames['emaileditor-iframe'].document.head.appendChild(fontAwesomeLink);
                    frames['emaileditor-iframe'].document.head.appendChild(dragulaLoaderScript);
                    frames['emaileditor-iframe'].document.head.appendChild(editorScript);

                    var $this = $(this);
                    var contents = $this.contents();

                    var editorMarkup = $('#editor-controls').contents();

                    $(contents).find('body').prepend(editorMarkup);
                });

                if ($editorIframe.length) {
                    $editorIframe[0].src = 'javascript: window.frameElement.getAttribute("srcdoc")';

                    // initialize component helpers
                    Rock.controls.emailEditor.buttonComponentHelper.initializeEventHandlers();
                    Rock.controls.emailEditor.codeComponentHelper.initializeEventHandlers();
                    Rock.controls.emailEditor.dividerComponentHelper.initializeEventHandlers();
                    Rock.controls.emailEditor.imageComponentHelper.initializeEventHandlers();
                    Rock.controls.emailEditor.textComponentHelper.initializeEventHandlers();
                }
            }
			
			function loadPropertiesPage(componentType, $component)
			{
			    $currentComponent = $component;
			    var $currentPropertiesPanel = $('.js-propertypanels').find("[data-component='" + componentType + "']");

				// hide all property panels
				$('.propertypanel').hide();

				// temp - set text of summernote
				switch(componentType){
					case 'text':
					    Rock.controls.emailEditor.textComponentHelper.setProperties($currentComponent);
						break;
					case 'button':
					    Rock.controls.emailEditor.buttonComponentHelper.setProperties($currentComponent);
						break;
				    case 'image':
				        Rock.controls.emailEditor.imageComponentHelper.setProperties($currentComponent);
				        break;
				    case 'section':
                        // no properties, just a delete button
				        break;
				    case 'divider':
				        Rock.controls.emailEditor.dividerComponentHelper.setProperties($currentComponent);
				        break;
				    case 'code':
				        Rock.controls.emailEditor.codeComponentHelper.setProperties($currentComponent);
				        break;
					default:
						 clearPropertyPane(null);
				}

			    // show proper panel
				$currentPropertiesPanel.show();

			    // show panel actions
				$('.js-propertypanel-actions').show();
			}

			// function that components will call after they have processed their own save and close logic
			function clearPropertyPane(e){

			    // hide all property panes, hide panel actions and set current as not selected
			    $('.propertypanel').hide();
			    $('.js-propertypanel-actions').hide();
			    $currentComponent.removeClass('selected');
				
				if (e != null){
					e.preventDefault();
				}
			}

            // function that will remove the currently selected component from the email html
			function deleteCurrentComponent()
			{
			    $currentComponent.remove();
			    clearPropertyPane(null);
			}

			function updateTextComponent(el, contents)
			{
			    Rock.controls.emailEditor.textComponentHelper.updateTextComponent(el, contents);
			}

			function updateCodeComponent(el, contents)
			{
			    Rock.controls.emailEditor.codeComponentHelper.updateCodeComponent(el, contents);
			}

			function handleImageUpdate(e, data)
			{
			    Rock.controls.emailEditor.imageComponentHelper.handleImageUpdate(e, data);
			}

			function setActiveButtonGroupButton($activeBtn)
			{
			    $activeBtn.addClass('active').addClass('btn-primary').removeClass('btn-default');
			    $activeBtn.siblings('.btn').removeClass('active').removeClass('btn-primary').addClass('btn-default')
			    $activeBtn.closest('.btn-group').siblings('.js-hidden-selected').val($activeBtn.data('val'));

			    if ($('.js-medium-userpreference').hasClass('active')) {
			        $('.js-medium-userpreference-notification').show();
			    } else {
			        $('.js-medium-userpreference-notification').hide();
			    }
			}

        </script>
        
        <!-- Text Component -->
        <script src='<%=RockPage.ResolveRockUrl("~/Scripts/Rock/Controls/EmailEditor/textComponentHelper.js", true)%>' ></script>

        <!-- Button Component -->
        <script src='<%=RockPage.ResolveRockUrl("~/Scripts/Rock/Controls/EmailEditor/buttonComponentHelper.js", true)%>' ></script>

        <!-- Image Component -->
        <script src='<%=RockPage.ResolveRockUrl("~/Scripts/Rock/Controls/EmailEditor/imageComponentHelper.js", true)%>' ></script>

        <!-- Divider Component -->
        <script src='<%=RockPage.ResolveRockUrl("~/Scripts/Rock/Controls/EmailEditor/dividerComponentHelper.js", true)%>' ></script>

        <!-- Code Component -->
        <script src='<%=RockPage.ResolveRockUrl("~/Scripts/Rock/Controls/EmailEditor/codeComponentHelper.js", true)%>' ></script>

    </ContentTemplate>
</asp:UpdatePanel>