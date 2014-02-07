using System;
using System.Windows.Forms;

namespace MMBot
{
    public partial class NetworkInfoDialog : Form
    {
        public NetworkInfoDialog()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
