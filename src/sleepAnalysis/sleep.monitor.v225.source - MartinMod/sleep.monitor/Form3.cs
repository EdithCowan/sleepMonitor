using System.Windows.Forms;

namespace sleep.monitor
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }
        public void updateStatus(int fileNumber, int totalFiles)
        {
            label1.Text = $"Processing file {fileNumber.ToString("#,##0")} of {totalFiles.ToString("#,##0")}.";
        }
    }
}
