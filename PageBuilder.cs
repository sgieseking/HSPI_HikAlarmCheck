// Copyright (C) 2016 SRG Technology, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using HomeSeerAPI;
using static Scheduler.PageBuilderAndMenu;
using Scheduler;
using System.Collections.Specialized;

namespace HSPI_Template
{
    /// <summary>
    /// This class adds some common support functions for creating the web pages used by HomeSeer plugins. 
    /// <para/>For each control there are three functions:  
    /// <list type="bullet">
    /// <item><description><c>Build:</c> Used to initially create the control in the web page.</description></item>
    /// <item><description><c>Update:</c> Used to modify the control in an existing web page.</description></item>
    /// <item><description><c>Form:</c> Not normally call externally but could be useful in special circumstances.</description></item>
    /// </list>
    /// </summary>
    /// <seealso cref="Scheduler.PageBuilderAndMenu.clsPageBuilder" />
    class PageBuilder : clsPageBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HikAlarmPageBuilder"/> class.
        /// </summary>
        /// <param name="Pagename">The name used by HomeSeer when referencing this particular page.</param>
        public PageBuilder(string Pagename) : base(Pagename)
        {
        }

        /// <summary>
        /// Build a button for a web page.
        /// </summary>
        /// <param name="Text">The text on the button.</param>
        /// <param name="Name">The name used to create the references for the button.</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        /// <returns>The text to insert in the web page to create the button.</returns>
        protected string BuildButton(string Text, string Name, bool Enabled = true)
        {
            return "<div id='" + Name + "_div'>" + FormButton(Name, Text, Enabled: Enabled) + "</div>";
        }

        /// <summary>
        /// Update a button on a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Text">The text on the button.</param>
        /// <param name="Name">The name used to create the references for the button.</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        protected void UpdateButton(string Text, string Name, bool Enabled = true)
        {
            divToUpdate.Add(Name + "_div", FormButton(Name, Text, Enabled: Enabled));
        }

        /// <summary>
        /// Return the string required to create a web page button.
        /// </summary>
        protected string FormButton(string Name, string label = "Submit", bool SubmitForm = true,
                                    string ImagePathNormal = "", string ImagePathPressed = "", string ToolTip = "",
                                    bool Enabled = true, string Style = "")
        {
            clsJQuery.jqButton b = new clsJQuery.jqButton(Name, label, PageName, SubmitForm);
            b.id = "o" + Name;
            b.imagePathNormal = ImagePathNormal;
            b.imagePathPressed = (ImagePathPressed == "") ? b.imagePathNormal : ImagePathPressed;
            b.toolTip = ToolTip;
            b.enabled = Enabled;
            b.style = Style;

            string Button = b.Build();
            Button.Replace("</button>\r\n", "</button>");
            Button.Trim();
            return Button;
        }

        /// <summary>
        /// Build a label for a web page.
        /// </summary>
        /// <param name="Text">The text for the label.</param>
        /// <param name="Name">The name used to create the references for the label.</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        /// <returns>The text to insert in the web page to create the label.</returns>
        protected string BuildLabel(string Name, string Msg = "")
        {
            return "<div id='" + Name + "_div'>" + FormLabel(Name, Msg) + "</div>";
        }

        /// <summary>
        /// Update a label on a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Text">The text for the label.</param>
        /// <param name="Name">The name used to create the references for the label.</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        protected void UpdateLabel(string Name, string Msg = "")
        {
            divToUpdate.Add(Name + "_div", FormLabel(Name, Msg));
        }

        /// <summary>
        /// Return the string required to create a web page label.
        /// </summary>
        protected string FormLabel(string Name, string Message = "", bool Visible = true)
        {
            string Content;
            if (Visible)
                Content = Message + "<input id='" + Name + "' Name='" + Name + "' Type='hidden'>";
            else
                Content = "<input id='" + Name + "' Name='" + Name + "' Type='hidden' value='" + Message + "'>";
            return Content;
        }

        /// <summary>
        /// Build a text entry box for a web page.
        /// </summary>
        /// <param name="Text">The default text for the text box.</param>
        /// <param name="Name">The name used to create the references for the text box.</param>
        /// <param name="AllowEdit">if set to <c>true</c> allow the text to be edited.</param>
        /// <returns>The text to insert in the web page to create the text box.</returns>
        protected string BuildTextBox(string Name, string Text = "", bool AllowEdit = true)
        {
            return "<div id='" + Name + "_div'>" + HTMLTextBox(Name, Text, 20, AllowEdit) + "</div>";
        }

        /// <summary>
        /// Update a text box on a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Text">The text for the text box.</param>
        /// <param name="Name">The name used to create the references for the text box.</param>
        /// <param name="AllowEdit">if set to <c>true</c> allow the text to be edited.</param>
        protected void UpdateTextBox(string Name, string Text = "", bool AllowEdit = true)
        {
            divToUpdate.Add(Name + "_div", HTMLTextBox(Name, Text, 20, AllowEdit));
        }

        /// <summary>
        /// Return the string required to create a web page text box.
        /// </summary>
        protected string HTMLTextBox(string Name, string DefaultText, int Size, bool AllowEdit = true)
        {
            string Style = "";
            string sReadOnly = "";

            if (!AllowEdit)
            {
                Style = "color:#F5F5F5; background-color:#C0C0C0;";
                sReadOnly = "readonly='readonly'";
            }

            return "<input type='text' id='o" + Name + "' style='" + Style + "' size='" + Size + "' name='" + Name + "' " + sReadOnly + " value='" + DefaultText + "'>";
        }

        /// <summary>
        /// Build a check box for a web page.
        /// </summary>
        /// <param name="Name">The name used to create the references for the text box.</param>
        /// <param name="Checked">if set to <c>true</c> [checked].</param>
        /// <returns>The text to insert in the web page to create the check box.</returns>
        protected string BuildCheckBox(string Name, bool Checked = false)
        {
            return "<div id='" + Name + "_div'>" + FormCheckBox(Name, Checked) + "</div>";
        }

        /// <summary>
        /// Update a check box on a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Name">The name used to create the references for the text box.</param>
        /// <param name="Checked">if set to <c>true</c> [checked].</param>
        protected void UpdateCheckBox(string Name, bool Checked = false)
        {
            divToUpdate.Add(Name + "_div", FormCheckBox(Name, Checked));
        }

        /// <summary>
        /// Return the string required to create a web page check box.
        /// </summary>
        protected string FormCheckBox(string Name, bool Checked = false, bool AutoPostBack = true, bool SubmitForm = true)
        {
            clsJQuery.jqCheckBox cb = new clsJQuery.jqCheckBox(Name, "", PageName, AutoPostBack, SubmitForm);
            cb.id = "o" + Name;
            cb.@checked = Checked;
            return cb.Build();
        }

        /// <summary>
        /// Build a list box for a web page.
        /// </summary>
        /// <param name="Name">The name used to create the references for the list box.</param>
        /// <param name="Options">Data value pairs used to populate the list box.</param>
        /// <param name="Selected">Index of the item to be selected.</param>
        /// <param name="SelectedValue">Name of the value to be selected.</param>
        /// <param name="Width">Width of the list box</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].  Doesn't seem to work.</param>
        /// <returns>The text to insert in the web page to create the list box.</returns>
        protected string BuildListBox(string Name, ref NameValueCollection Options, int Selected = -1, string SelectedValue = "", int Width = 150, bool Enabled = true)
        {
            return "<div id='" + Name + "_div'>" + FormListBox(Name, ref Options, Selected, SelectedValue, Width, Enabled) + "</div>";
        }

        /// <summary>
        /// Update a list box for a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Name">The name used to create the references for the list box.</param>
        /// <param name="Options">Data value pairs used to populate the list box.</param>
        /// <param name="Selected">Index of the item to be selected.</param>
        /// <param name="SelectedValue">Name of the value to be selected.</param>
        /// <param name="Width">Width of the list box</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].  Doesn't seem to work.</param>
        protected void UpdateListBox(string Name, ref NameValueCollection Options, int Selected = -1, string SelectedValue = "", int Width = 150, bool Enabled = true)
        {
            divToUpdate.Add(Name + "_div", FormListBox(Name, ref Options, Selected, SelectedValue, Width, Enabled));
        }

        /// <summary>
        /// Return the string required to create a web page list box.
        /// </summary>
        protected string FormListBox(string Name, ref NameValueCollection Options, int Selected = -1, string SelectedValue = "", int Width = 150, bool Enabled = true)
        {
            clsJQuery.jqListBox lb = new clsJQuery.jqListBox(Name, PageName);

            lb.items.Clear();
            lb.id = "o" + Name;
            lb.style = "width: " + Width + "px;";
            lb.enabled = Enabled;

            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    if ((Selected == -1) && (SelectedValue == Options.GetKey(i)))
                        Selected = i;
                    lb.items.Add(Options.GetKey(i));
                }
                if (Selected >= 0)
                    lb.SelectedValue = Options.GetKey(Selected);
            }

            return lb.Build();
        }

        /// <summary>
        /// Build a drop list for a web page.
        /// </summary>
        /// <param name="Name">The name used to create the references for the list box.</param>
        /// <param name="Options">Data value pairs used to populate the list box.</param>
        /// <param name="Selected">Index of the item to be selected.</param>
        /// <param name="SelectedValue">Name of the value to be selected.</param>
        /// <returns>The text to insert in the web page to create the drop list.</returns>
        protected string BuildDropList(string Name, ref NameValueCollection Options, int Selected = -1, string SelectedValue = "")
        {
            return "<div id='" + Name + "_div'>" + FormDropDown(Name, ref Options, Selected, SelectedValue: SelectedValue) + "</div>";
        }

        /// <summary>
        /// Update a drop list for a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Name">The name used to create the references for the list box.</param>
        /// <param name="Options">Data value pairs used to populate the list box.</param>
        /// <param name="Selected">Index of the item to be selected.</param>
        /// <param name="SelectedValue">Name of the value to be selected.</param>
        protected void UpdateDropList(string Name, ref NameValueCollection Options, int Selected = -1, string SelectedValue = "")
        {
            divToUpdate.Add(Name + "_div", FormDropDown(Name, ref Options, Selected, SelectedValue: SelectedValue));
        }

        /// <summary>
        /// Return the string required to create a web page drop list.
        /// </summary>
        protected string FormDropDown(string Name, ref NameValueCollection Options, int selected, int width = 150, bool SubmitForm = true, bool AddBlankRow = false,
                                    bool AutoPostback = true, string Tooltip = "", bool Enabled = true, string ddMsg = "", string SelectedValue = "")
        {
            clsJQuery.jqDropList dd = new clsJQuery.jqDropList(Name, PageName, SubmitForm);

            dd.selectedItemIndex = -1;
            dd.id = "o" + Name;
            dd.autoPostBack = AutoPostback;
            dd.toolTip = Tooltip;
            dd.style = "width: " + width + "px;";
            dd.enabled = Enabled;

            //Add a blank area to the top of the list
            if (AddBlankRow)
                dd.AddItem(ddMsg, "", false);

            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    bool sel = (i == selected) || (Options.Get(i) == SelectedValue);

                    dd.AddItem(Options.GetKey(i), Options.Get(i), sel);
                }
            }

            return dd.Build();
        }
    }
}