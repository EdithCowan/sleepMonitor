using System;
using System.IO;
using System.Windows.Forms;

namespace sleep.monitor
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();

            // Populate text box with settings
            textBox1.Text = Properties.Settings.Default.folderDefault;

            // Configure flip display image checkbok
            checkBox1.Checked = Properties.Settings.Default.flipImage;
        }

        ///***********************************************************************
        // Methods called by buttons go here
        private void SelectCancel()
        // Exit the preferences window without saving changes.
        {
            this.Close();
        }
        private void SelectOK()
        // Save changes and exit the preferences window.
        {
            // Parse textboxes and save to settings.
            ParseFolderLocation();                                                      // Default folder location

            // Save checkbox to settings
            Properties.Settings.Default.flipImage = checkBox1.Checked;                  // Flip image
                                                                                        // Print error message and prevent closing window if required, then reset error message.
                                                                                        // Else save settings and close window.
            if (Management.errorList != String.Empty)
            {
                MessageBox.Show($"Settings could not be saved due to the following errors:{Management.errorList}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Management.errorList = String.Empty;
            }
            else
            {
                Properties.Settings.Default.Save();
                this.Close();
            }
        }

        private void ParseFolderLocation()
        // This method is used to verify that folder specified in textBox4 "Default folder location" exists.
        // If it exists, the value is saved to the setting folderDefault.
        // Else an error is raised which prevents the preferences box from closing.
        {
            // Parse default folder location and save to string
            if (Directory.Exists(textBox1.Text))
            {
                Properties.Settings.Default.folderDefault = textBox1.Text;
            }
            else
            {
                Management.errorList = Management.errorList + "\n - Default folder location does not exist";
            }
        }

        ///***********************************************************************
        // Buttons actions go here
        private void button1_Click(object sender, EventArgs e)
        // When OK is selected, SelectOK() is called to save changs and exit the preferences window.
        {
            SelectOK();
        }
        private void button2_Click(object sender, EventArgs e)
        // When Cancel is selected, SelectCancel() is called to exit the preferences window without saving changes.
        {
            SelectCancel();
        }
        private void button3_Click(object sender, EventArgs e)
        {          
            Form2 preferencesAdvanced = new Form2();
            preferencesAdvanced.FormBorderStyle = FormBorderStyle.FixedDialog;
            preferencesAdvanced.MaximizeBox = false;
            preferencesAdvanced.MinimizeBox = false;
            preferencesAdvanced.ControlBox = false;
            preferencesAdvanced.StartPosition = FormStartPosition.CenterScreen;
            preferencesAdvanced.ShowDialog();
            this.Close();
        }

        ///***********************************************************************
        // Methods relating to keyboard shortcuts go here
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        // All keyboard shortcuts are defined in this method
        {
            // Keyboard shorcuts relating to playback go here
            if (keyData == (Keys.Enter))                                             // Toggle play pause when alt+space is pressed
            {
                SelectOK();
                return true;
            }
            else if (keyData == (Keys.Escape))                                       // Toggle play pause when alt+space is pressed
            {
                SelectCancel();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
