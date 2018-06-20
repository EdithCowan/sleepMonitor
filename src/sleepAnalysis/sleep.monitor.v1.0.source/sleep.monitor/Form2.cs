using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace sleep.monitor
{
    public partial class Form2 : Form
    {
        public Form2()
        // Class constructor
        {
            InitializeComponent();

            // Populate text boxes with settings
            textBox1.Text = Properties.Settings.Default.fileExtn;
            textBox2.Text = Properties.Settings.Default.fileContains1;
            textBox3.Text = Properties.Settings.Default.fileContains2;
            textBox4.Text = Properties.Settings.Default.folderDefault;

            // Configure display time combobox
            for (int i = 2; i <= 1024;)
            {
                comboBox1.Items.Add(i);
                i = i * 2;
            }
            comboBox1.SelectedIndex = comboBox1.FindStringExact(Properties.Settings.Default.speedDefault);

            // Configure Zero pixel differences from start/end combo boxes and peak detection look ahead
            for (int i = 0; i <= 9; i++)
            {
                comboBox3.Items.Add(i);
                comboBox4.Items.Add(i);
                comboBox5.Items.Add(i);
            }
            comboBox3.SelectedIndex = comboBox3.FindStringExact(Properties.Settings.Default.zeroPixelStart);
            comboBox4.SelectedIndex = comboBox4.FindStringExact(Properties.Settings.Default.zeroPixelEnd);
            comboBox5.SelectedIndex = comboBox5.FindStringExact(Properties.Settings.Default.peakLookAhead);

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
            ParseFileExtn();                                                            // Allowed file extensions
            ParseWhiteList(1);                                                          // Image name whitelist for series 1
            ParseWhiteList(2);                                                          // Image name whitelist for series 2
            ParseFolderLocation();                                                      // Default folder location

            // Save comboboxes to settings - these do not requre parsing like above.
            Properties.Settings.Default.speedDefault = comboBox1.Text;                  // Default playback speed (ms)
            Properties.Settings.Default.zeroPixelStart = comboBox3.Text;                // Zero pixel start
            Properties.Settings.Default.zeroPixelEnd = comboBox4.Text;                  // Zero pixel end
            Properties.Settings.Default.peakLookAhead = comboBox5.Text;                 // Peak look ahead

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
        private void ParseFileExtn()
        // This method is used to parse the input in textBox1 "Allowed file extensions".
        // If textBox1 contains only white space, an error is raised which prevents the preferences box from closing.
        // Else the value is saved to the setting fileExtn.
        {
            if (textBox1.Text == "")
            {
                Management.errorList = Management.errorList + "\n - File extension box cannot be blank";
            }
            else
            {
                Properties.Settings.Default.fileExtn = ParseString(textBox1.Text);
            }
        }
        private void ParseWhiteList(int seriesNo)
        // This method is used to parse the input in textBox2 and textBox3 "Image name whitelist".
        // Whitespace is allowed, and if this is entered it is saved directly to the setting fileContains.
        // If the input is not whitespace it is then parsed and saved as above.
        {
            string processString = String.Empty;
            if (seriesNo == 1)
            {
                processString = textBox2.Text.Trim();
                if (processString == "")
                {
                    Properties.Settings.Default.fileContains1 = "";
                }
                else
                {
                    Properties.Settings.Default.fileContains1 = ParseString(processString);
                }
            }
            else
            {
                processString = textBox3.Text.Trim();
                if (processString == "")
                {
                    Properties.Settings.Default.fileContains2 = "";
                }
                else
                {
                    Properties.Settings.Default.fileContains2 = ParseString(processString);
                }
            }
        }
        private string ParseString(string processString)
        // This method is used to parse the input in the text boxes by removing duplicates.
        {
            string[] processList = processString.Trim().ToLower().Split(' ').Distinct().ToArray();      // Trim whitespace, make lowercase, and remove duplicates by converting to array
            processString = String.Join(" ", processList);                                              // Convert back to string with single whitespace as separator
            return processString;                                                                       // Return value
        }
        private void ParseFolderLocation()
        // This method is used to verify that folder specified in textBox4 "Default folder location" exists.
        // If it exists, the value is saved to the setting folderDefault.
        // Else an error is raised which prevents the preferences box from closing.
        {
            // Parse default folder location and save to string
            if (Directory.Exists(textBox4.Text))
            {
                Properties.Settings.Default.folderDefault = textBox4.Text;
            }
            else
            {
                Management.errorList = Management.errorList + "\n - Default folder location does not exist";
            }
        }

        ///***********************************************************************
        // Buttons actions go here
        private void button2_Click(object sender, EventArgs e)
        // When Cancel is selected, SelectCancel() is called to exit the preferences window without saving changes.
        {
            SelectCancel();
        }
        private void button1_Click(object sender, EventArgs e)
        // When OK is selected, SelectOK() is called to save changs and exit the preferences window.
        {
            SelectOK();
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
