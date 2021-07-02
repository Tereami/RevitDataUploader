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
    public enum UploadFormat { JSON, XML }

    public partial class FormSettings : Form
    {
        public UploadFormat UploadFormat;

        public FormSettings()
        {
            InitializeComponent();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            if (radioButtonJSON.Checked)
                this.UploadFormat = UploadFormat.JSON;
            if (radioButtonXML.Checked)
                this.UploadFormat = UploadFormat.XML;
            
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
