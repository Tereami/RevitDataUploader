using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RevitDataUploader
{
    public partial class FormSelectLinks : Form
    {
        public Dictionary<string, Autodesk.Revit.DB.RevitLinkInstance> selectedDocs;
        private Dictionary<string, Autodesk.Revit.DB.RevitLinkInstance> allLinksBase;

        public FormSelectLinks(Dictionary<string, Autodesk.Revit.DB.RevitLinkInstance> linksBase)
        {
            InitializeComponent();

            allLinksBase = linksBase;

            List<string> docNames = linksBase.Keys.ToList();
            docNames.Sort();
            foreach(string title in docNames)
            {
                checkedListBox1.Items.Add(title);
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            selectedDocs = new Dictionary<string, Autodesk.Revit.DB.RevitLinkInstance>();
            foreach(var row in checkedListBox1.SelectedItems)
            {
                string curTitle = (string)row;
                Autodesk.Revit.DB.RevitLinkInstance curLink = allLinksBase[curTitle];
                selectedDocs.Add(curTitle, curLink);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
