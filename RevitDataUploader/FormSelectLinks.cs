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
        public List<string> selectedDocs { get; set; }

        public FormSelectLinks(List<string> docNames)
        {
            InitializeComponent();

            docNames.Sort();
            foreach(string title in docNames)
            {
                listBox1.Items.Add(title);
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            selectedDocs = new List<string>();
            foreach(var row in listBox1.SelectedItems)
            {
                string curTitle = (string)row;
                selectedDocs.Add(curTitle);
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
