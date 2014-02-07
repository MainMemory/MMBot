using System.Windows.Forms;

namespace MMBot
{
    public partial class Dialog1
    {
        public Dialog1()
        {
            InitializeComponent();
        }

        private void OK_Button_Click(System.Object sender, System.EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }
    }
}