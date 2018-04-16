using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;



namespace sleep.monitor
{
    public partial class Form1 : Form
    {
        /// Create timer
        // Source: http://stackoverflow.com/questions/12039695/thread-sleep-in-c-sharp
        Timer myTimer = new Timer();

        public Form1()
        // Class constructor
        {
            InitializeComponent();        
            chart1.Series.Clear();                                                              // Clear the chart
            Management.waitTime = Convert.ToInt32(Properties.Settings.Default.speedDefault);    // Set wait time to default
            InitializeGUI();
        }

        ///***********************************************************************
        // Methods relating to the timer go here

        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        // This method relates to the tick of the timer.
        // Normal operation is when the chart is not being clicked
        //      - On each tick of the timer, the method LoadNewImage() is called.
        //      - If Management.imageCounter != Management.imageCount (normal operation), Management.imageCounter is incremented by 1.
        //      - Else the stop button is called.
        // When the chart is being clicked (real time scrubbing), Management.imageCounter is updated according to the cursor position relative to the chart by calling the method GetChartX().
        // Then the image displayed is updated by calling the method LoadNewImage().
        // If the Y value is close to the threshold value then the threshold line is moved instead of the playback position line (provided segmentation unprocessed)
        {
            if (Management.chartClick == false)
            {
                LoadNewImage();
                //if (Management.imageCounter + 1 != Management.imgCount1)
                if (Management.imageCounter + Management.frameSkip < Management.imgCount1)
                {
                    //Management.imageCounter++;
                    Management.imageCounter+= Management.frameSkip;
                }
                else
                {
                    ButtonStop();
                }
            }
            else
            {
                // Change threshold line if Y val close to current threshold, and segmentation unprocessed
                if (Management.changeThreshold == true && Management.segmentationProcessed == false)
                {
                    Management.threshold = GetChartY();
                    UpdateThreshold();
                    PlotThresholdLine();
                }
                else
                {
                    Management.imageCounter = GetChartX();
                    LoadNewImage();
                }
            }
        }


        ///***********************************************************************
        // Methods relating to updating the GUI go here
        
        private void InitializeGUI()
        // When this method is called, the labels are reset to their initial state and the methods UpdateDisplayTime() and UpdateThreshold() are called.
        {
            label2.Text = "Image filename:";
            label6.Text = "Image filesize:";
            label5.Text = "Image number:";
            label3.Text = "Image dimensions:";
            UpdateDisplayTime();
            UpdateThreshold();
            Management.displaySeries1 = false; // Set display series to second series

            // Disable export to CSV
            printReportToolStripMenuItem.Enabled = false;
            Management.allowExport2CSV = false;

            // Disable process segmentation
            processSegmentationToolStripMenuItem.Enabled = false;
            Management.allowProcessSegmentation = false;
            Management.segmentationProcessed = false;
            processSegmentationToolStripMenuItem.Text = "Process segmentation (F3)";

            // Disable chart title
            chart1.Titles[0].Visible = false;
        }
        private void UpdateDisplayTime()
        // When this method is called, the timer interval is updated, and the GUI us updated to show the new timer interval in ms.
        {
            label7.Text = $"Image display time: {Management.waitTime} milliseconds";
            myTimer.Interval = Management.waitTime;
        }
        private void UpdateThreshold()
        // When this method is called, the text showing the threshold % in the GUI is updated.
        {
            label10.Text = $"Threshold: {Management.threshold.ToString("N3")}%";
        }
        private void LoadNewImage()
        // When this method is called, the image file input is used to update the text display in the GUI
        // A separate method PlotPlaybackLine() is also called to update the playback position in chart1.
        // The method can display images from both Series 1 and Series 2 depending on the value of the bool Management.displaySeries1
        {
            // Obtain image location as string depending on which series selected
            if (Management.displaySeries1 == false && Management.imgCount2 != 0)
            {
                Management.displayImage = Management.imageFiles2[Management.imageCounter];
            }
            else
            {
                Management.displayImage = Management.imageFiles1[Management.imageCounter];
            }

            // Test to ensure that file is accessible
            if (File.Exists(Management.displayImage) == false)
            {
                MessageBox.Show($"File ${Management.displayImage} is no longer available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ButtonStop();
                return;
            }

            // Use filename to create OpenCV object and display the image in the pictureBox
            // Source: http://www.emgu.com/wiki/index.php/Setting_up_EMGU_C_Sharp#A_Basic_Program
            Image<Gray, byte> myImage = new Image<Gray, byte>(Management.displayImage);

            // Flip image on X axis if required in preferences
            if (Properties.Settings.Default.flipImage == true)
            {
                myImage = myImage.Flip(Emgu.CV.CvEnum.FlipType.Horizontal); 
            }

            // stretch contrast
            myImage._EqualizeHist();
           
            pictureBox1.Image = myImage.ToBitmap();

            // Update labels with info relating to image
            label2.Text = $"Filename: {Path.GetFileName(Management.displayImage)}";
            label6.Text = $"Filesize: {Management.humanReadableFileSize(Management.displayImage)}";
            label5.Text = $"Sequence number: {(Management.imageCounter + 1).ToString("#,##0")}/{Management.imgCount1.ToString("#,##0")}";
            label3.Text = $"Dimensions: {myImage.Width} x {myImage.Height}";

            // Update sleep segment info
            if (Management.sleepIndex.Count() > 1) // Was previously Management.segmentationProcessed == true but resulted in crash when segmentation reprocessed with threshold above max val resulting in single segment
            {
                int segmentNo = Management.GetSleepIndex();
                TimeSpan[] timeStartEnd = Management.GetTimes(segmentNo);
                TimeSpan timeDuration = timeStartEnd[1] - timeStartEnd[0];

                label8.Text = $"Sequence number: {segmentNo + 1}/{Management.sleepIndex.Count()}";
                label11.Text = $"Posture classification: {Management.sleepPos[Management.GetSleepIndex()]}";
                label12.Text = $"Start time (hh:mm:ss): {timeStartEnd[0].ToString(@"hh\:mm\:ss")}";
                label13.Text = $"End time (hh:mm:ss): {timeStartEnd[1].ToString(@"hh\:mm\:ss")}";
                label14.Text = $"Duration (hh:mm:ss): {timeDuration.ToString(@"hh\:mm\:ss")}";
            }
            else if (Management.sleepIndex.Count() == 1)
            {
                int segmentNo = Management.GetSleepIndex();
                TimeSpan timeDuration = Management.relTimeStampsTS.Last() - Management.relTimeStampsTS.First();

                label8.Text = $"Sequence number: {segmentNo + 1}/{Management.sleepIndex.Count()}";
                label11.Text = $"Posture classification: {Management.sleepPos[Management.GetSleepIndex()]}";
                label12.Text = $"Start time (hh:mm:ss): {Management.relTimeStampsTS.First().ToString(@"hh\:mm\:ss")}";
                label13.Text = $"End time (hh:mm:ss): {Management.relTimeStampsTS.Last().ToString(@"hh\:mm\:ss")}";
                label14.Text = $"Duration (hh:mm:ss): {timeDuration.ToString(@"hh\:mm\:ss")}";
            }
            else
            {
                label8.Text = "Sequence number: N/A";
                label11.Text = "Posture classification: N/A";
                label12.Text = "Start time (hh:mm:ss): N/A";
                label13.Text = "End time (hh:mm:ss): N/A";
                label14.Text = "Duration (hh:mm:ss): N/A";
            }         

            // Plot playback position in chart
            PlotPlaybackLine();
            
            // Update comments
            textBox1.Text = Management.sleepComments[Management.imageCounter];
        }

        ///***********************************************************************
        // Methods relating to charts go here

        private void InitializeChart()
        // This method is called the first time that the program loads image data and is used to initialize the chart.
        {
            chart1.ChartAreas[0].AxisY.Maximum = 105;
            //Series1 pixel differences represented by blue line on chart
            var series1 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Series1",
                Color = System.Drawing.Color.Blue,
                IsVisibleInLegend = false,
                IsXValueIndexed = false,
                Enabled = true,
                ChartType = SeriesChartType.Line
            };
            chart1.Series.Add(series1);

            // Series2 threshold represented by horizontal red line on chart
            var series2 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Series2", 
                Color = System.Drawing.Color.Red,
                IsVisibleInLegend = false,
                IsXValueIndexed = false,
                Enabled = true,
                ChartType = SeriesChartType.FastLine
            };
            chart1.Series.Add(series2);

            // Series3 playback position represented by vertical green line on chart
            var series3 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Series3", 
                Color = System.Drawing.Color.Green,
                IsVisibleInLegend = false,
                IsXValueIndexed = false,
                Enabled = true,
                ChartType = SeriesChartType.FastLine
            };
            chart1.Series.Add(series3);

            // Series4 test for secondary X axis data
            var series4 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Series4",
                IsVisibleInLegend = false,
                IsXValueIndexed = false,
                Enabled = true,
                XAxisType = AxisType.Secondary
            };
            chart1.Series.Add(series4);

            for (int i = 1; i < Management.pixelDiff.Count(); i++)
            {
                chart1.Series["Series4"].Points.AddXY(Management.absTimeStamps[i], 0);
            }

            // Enable title
            chart1.Titles[0].Visible = true;

            // Format primary X axis
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm";
            chart1.ChartAreas[0].AxisX.Title = "Elapsed time \"hh:mm\"";
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            // Format secondary X axis
            chart1.ChartAreas[0].AxisX2.LabelStyle.Format = "HH:mm";
            chart1.ChartAreas[0].AxisX2.Title = "Time of day \"hh:mm\"";
            chart1.ChartAreas[0].AxisX2.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX2.MajorGrid.Enabled = false;
            // Format Y axis
            chart1.ChartAreas[0].AxisY.Title = "Movement ammount";
            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            Management.chartShown = true; // Set booleen variable to true so that functionality requiring chart to be plotted can proceed
        }
        private void PlotPixelDiff()
        // This method is used to plot the blue pixel differences line in the chart and is called when ever new image data is loaded.
        {
            chart1.Series["Series1"].Points.Clear();
            for (int i = 0; i < Management.pixelDiff.Count(); i++)
            {
                chart1.Series["Series1"].Points.AddXY(Management.relTimeStampsDT[i], Management.pixelDiff[i] * 100);
            }
        }
        private void PlotThresholdLine()
        // This method is used to plot the horizontal red threshold line in the chart and is called when ever Management.threshold changes.
        // It is only active when Management.chartShown == true.
        {
            if (Management.chartShown == true)
            {
                chart1.Series["Series2"].Points.Clear();
                chart1.Series["Series2"].Points.AddXY(0, Management.threshold);
                chart1.Series["Series2"].Points.AddXY(Management.relTimeStampsDT.Last(), Management.threshold);
            }
        }
        private void PlotPlaybackLine()
        // This method is used to plot the vertical green playback line in the chart and is called when ever Management.imageCounter changes.
        {
            int plotPoint = Management.imageCounter + 1;
            if (Management.imageCounter + 1 == Management.relTimeStampsDT.Count())
            {
                plotPoint = Management.imageCounter;
            }

            chart1.Series["Series3"].Points.Clear();
            chart1.Series["Series3"].Points.AddXY(Management.relTimeStampsDT[plotPoint], 0);
            chart1.Series["Series3"].Points.AddXY(Management.relTimeStampsDT[plotPoint], Management.pixelDiffMax);
        }
        private int GetChartX()
        // This method returns the image sequence value relative to the chart
        {
            double pX = 0;
            try
            {
                pX = chart1.ChartAreas[0].AxisX.PixelPositionToValue(chart1.PointToClient(Control.MousePosition).X);
            }
            catch (System.ArgumentException)
            {
                pX = 0;
            }

            if (pX < 0)                                             // When curser is left of chart min val
            {
                return 0;
            }
            else if (pX > chart1.ChartAreas[0].AxisX.Maximum)       // When curser is right of chart max val
            {
                return Management.imgCount1 - 1;
            }
            else                                                    // All other values in between
            {
                return Convert.ToInt32((pX / chart1.ChartAreas[0].AxisX.Maximum) * (Management.imgCount1 - 1));
            }
        }
        private double GetChartY()
        // This method returns the Y value of the cursor position relative to the chart
        {
            double pY = 0;
            try
            {
                pY = Convert.ToDouble(chart1.ChartAreas[0].AxisY.PixelPositionToValue(chart1.PointToClient(Control.MousePosition).Y));
            }
            catch (System.ArgumentException)
            {
                pY = 0;
            }

            if (pY < 0)                             // Min val allowed for pY is zero
            {
                pY = 0;
            }

            if (pY > Management.pixelDiffMax)       // Max val allowed for pY is pixelDiffMax
            {
                pY = Management.pixelDiffMax;
            }
            return pY;
        }
        void chart1_MouseDown(object sender, MouseEventArgs e)
        // This method is triggered when the left mouse button is clicked on the chart.
        // It is used for real time scrubbing to change the playback position and to change the threshold line.
        {
            // Save the current state and initiate play if not already
            Management.waitTimeOld = Management.waitTime;
            if (myTimer.Enabled == true)
            {
                Management.prevPlayState = true;
            }
            else
            {
                ButtonPlay();
                Management.prevPlayState = false;
            }

            // Change the behaviour of the timer and increase the interval
            Management.waitTime = 2;
            Management.chartClick = true;

            // Determine whether timer will be updating Threshold line or Playback line
            double pY = GetChartY();
            if (pY < (Management.threshold + (Management.pixelDiffMax * 0.1)) && pY > (Management.threshold - (Management.pixelDiffMax * 0.1)))
            {
                Management.changeThreshold = true;
            }
            else
            {
                Management.changeThreshold = false;
            }

            // Trigger tick of the timer
        }
        void chart1_MouseUp(object sender, MouseEventArgs e)
        // This method is triggered when the left mouse button is released after clicking on the chart.
        // It is used to return the program to its initial state after real time scrubbing or changing the thresold line.
        {
            Management.chartClick = false;
            Management.waitTime = Management.waitTimeOld;
            if (Management.prevPlayState == false)
            {
                ButtonPause();
            }
        }
        private void GetPixelVals()
        // This method is used to save pixel values into the list Management.pixelVals from the image locations saved in Management.imageFiles1
        {
            Management.pixelVals.Clear();

            // Status form
            Form3 status = new Form3();
            status.FormBorderStyle = FormBorderStyle.FixedDialog;
            status.MaximizeBox = false;
            status.MinimizeBox = false;
            status.ControlBox = false;
            status.StartPosition = FormStartPosition.CenterScreen;

            // Calculate pixel vals
            status.Show(); // Show status window indicating progress
            // add the first one (none preceeding so cant calculate difference)
            Management.pixelVals.Add(1);
            for (int i = 1; i < Management.imgCount1; i++)
            {
                status.updateStatus((i + 1), Management.imgCount1);
                //Task<int> parallelOp = Task.Run(() => Management.calcPixelVal(Management.imageFiles1[i]));
                Task<int> parallelOp = Task.Run(() => Management.calcPixelValPixelWise(Management.imageFiles1[i], Management.imageFiles1[i-1]));
                Management.pixelVals.Add(parallelOp.Result);
            }
            status.Close();
        }
        private string PixelValErrors()
        // This method is used to raise an error when any of the values saved in Management.pixelVals are zero
        {
            // This variable is used to accumulate errors when calculating pixel values
            string pixelValError = String.Empty;

            // Show error if any files could not be parsed (pixel val 0)
            for (int i = 0; i < Management.imgCount1; i++)
            {
                if (Management.pixelVals[i] == 0) // Detect pixel vals of zero
                {
                    // removed - allow pixel val differences of zero.
                    //pixelValError = $"{pixelValError}\n{Management.imageFiles1[i]}";
                }
            }
            return pixelValError; // Exit method when one or more pixel vals are zero
        }
        private void GetPixelDiffs()
        // This method is used to save pixel difference values into the list Management.pixelDiff from the values saved in Management.pixelVals
        {
            Management.pixelDiff.Clear();

            // calculate the median
            System.Collections.Generic.List<int> sortedPixVals = Management.pixelVals.ToList<int>();
            sortedPixVals.Sort();
            double pixValMedian = sortedPixVals[(int)(Math.Round((double)(sortedPixVals.Count / 2)))];


            // calculate average pixval
            double pixValMean = Management.pixelVals.Average();

            // standard deviation
            double pixValSTD = Math.Sqrt(Management.pixelVals.Average(v => Math.Pow(v - pixValMean, 2)));
            double peakMax = 4;
            double peakMin = 2;

            // find maximum
            double pixValMax = Management.pixelVals.Max();

            peakMax = Math.Min(peakMax, (pixValMax-pixValMedian) / pixValSTD);
 
            for (int i = 0; i < Management.imgCount1; i++)
            {
                if (i == 0)
                {
                    Management.pixelDiff.Add(0);
                }
                else
                {
                    
                    if (Management.pixelVals[i] > (pixValMedian + peakMax * pixValSTD))
                    {
                        // cap peaks that are too big
                        Management.pixelDiff.Add(1);
                    }
                    /*
                    else if (Management.pixelVals[i] > (pixValMedian + peakMin * pixValSTD))
                    {
                        // enhance smaller peaks
                        Management.pixelDiff.Add(pixValMedian); // + 0.5* pixValSTD);
                    }
                    */
                    else                    
                    {
                        Management.pixelDiff.Add(Math.Max(0,(Management.pixelVals[i] - (int)pixValMedian))/(peakMax*pixValSTD));
                    }
                }
            }
        }
        private void CleanPixelDiffs()
        // This method is used modify pixel values from the start and end of the series to make the chart easier to read
        {
            int cleanPixelStart = Convert.ToInt32(Properties.Settings.Default.zeroPixelStart);
            int cleanPixelEnd = Convert.ToInt32(Properties.Settings.Default.zeroPixelEnd);

            if (cleanPixelStart != 0)
            {
                for (int i = 0; i < cleanPixelStart; i++)
                {
                    Management.pixelDiff[i] = 0;
                }
            }
            if (cleanPixelStart != 0)
            {
                for (int i = Management.pixelDiff.Count() - cleanPixelEnd - 1 ; i < Management.pixelDiff.Count(); i++)
                {
                    Management.pixelDiff[i] = 0;
                }
            }
        }
        private void CalcTimestamps()
        // This method is used to extract the timestamps from the filenames
        {
            // Calculate absolute timestamps
            Management.absTimeStamps.Clear();
            Regex rgx = new Regex(@"\d{8}-\d{2}-\d{2}-\d{2}");
            for (int i = 0; i < Management.imgCount1; i++)
            {
                DateTime temp;
                Match mat = rgx.Match(Path.GetFileName(Management.imageFiles1[i]));
                DateTime.TryParseExact(mat.ToString(), "yyyyMMdd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out temp);
                if (temp.ToString() != "1/01/0001 12:00:00 AM")
                {
                    Management.absTimeStamps.Add(temp);
                }         
            }
            // Calculate relative timestamps
            Management.relTimeStampsTS.Clear();
            Management.relTimeStampsDT.Clear();
            for (int i = 0; i < Management.absTimeStamps.Count(); i++)
            {
                if (i == 0)
                {
                    Management.relTimeStampsTS.Add(TimeSpan.Zero);
                    Management.relTimeStampsDT.Add(DateTime.MinValue);
                }
                else
                {
                    TimeSpan timeStampTS = Management.absTimeStamps[i] - Management.absTimeStamps[0];
                    Management.relTimeStampsTS.Add(timeStampTS);
                    DateTime timeStampDT = DateTime.MinValue;
                    timeStampDT = timeStampDT + timeStampTS;
                    Management.relTimeStampsDT.Add(timeStampDT);
                }
            }
        }      
        private void PlotSegmentation()
        // This method is used to plot orange segmenation lines in the chart.
        {
            for (int i = 1; i < Management.sleepIndex.Count(); i++)
            {
                string seriesName = $"Segment{i}";
                if (chart1.Series.IndexOf(seriesName) == -1)
                {
                    var newSegment = new System.Windows.Forms.DataVisualization.Charting.Series
                    {
                        Name = seriesName,
                        Color = System.Drawing.Color.DarkOrange,
                        BorderWidth = 2,
                        IsVisibleInLegend = false,
                        IsXValueIndexed = false,
                        Enabled = true,
                        ChartType = SeriesChartType.Line
                    };
                    chart1.Series.Add(newSegment);
                }
                chart1.Series[seriesName].Points.AddXY(Management.relTimeStampsDT[Management.sleepIndex[i]], 0);
                chart1.Series[seriesName].Points.AddXY(Management.relTimeStampsDT[Management.sleepIndex[i]], Management.pixelDiffMax);
            }
        }
        private void UnPlotSegmentation()
        // This method is used to remove orange segmenation lines in the chart.
        {
            int plottedSegments = chart1.Series.Count() - 4; // There are 4 chart series before segmentation is plotted
            for (int i = 1; i <= plottedSegments; i++)
            {
                chart1.Series[$"Segment{i}"].Points.Clear();
            }
        }

        ///***********************************************************************
        // Methods called by tool strip menu items go here
        private void MenuFileLoadImages()
        // This method is triggered when the file option is selected to load new files into the program.
        // First, the program is reset to its initial state.
        // The first time the program is run, the event handler is linked to the tick of the timer.
        // On subsequent runs, the timer is stopped and pictureBox1 and chart1 reinitialized.
        // The user is prompted to select a folder containing images.
        // If cancel is selected, the method exits.
        // If a folder is selected, the files in the folder are parsed according to the settings in the preferences menu.
        // A status window is opened in a new thread to keep the user informed of the progress in parsing the files.
        // If the folder contains any images that cannot be parsed, an error method is shown and the method exits.
        // The pixel values for each image are calculated and this is used to calculate pixel differences.
        // The chart is then initialized, pixel difference plot as Chart1.Series1 and threshold plot as Chart1.Series2.
        // The playback sequence is then initiated.
        // The Toolstrip File menu is then updated to allow processing segmentation
        {
            // Show warning message if data already loaded
            if (Management.imgCount1 > 0)
            {
                DialogResult dialogResult = MessageBox.Show($"Images from the following folder have already been loaded into the program:\n\n{Management.imageFolder}\n\nIf you proceed, any unsaved work will be lost. Are sure you want to proceed?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.No)
                {
                    return;
                }
            }

            // Reset to initial state
            ButtonStop();

            Management.imgCount1 = 0;
            Management.displaySeries1 = true;
            Management.sleepComments.Clear();
            textBox1.Text = "Notes go here";
            textBox1.Enabled = false;

            InitializeGUI();
            ClearSegmentation();
            label1.Text = "Folder:";

            // Conditions for first run
            if (Management.firstRun)
            {
                Management.firstRun = false;
                // Link timer to event handler
                myTimer.Tick += new EventHandler(TimerEventProcessor);
            }
            else
            {
                // Reset picturebox and chart
                pictureBox1.Image = null;
                chart1.Series.Clear();
                Management.chartShown = false;
            }

            // Prompt user to select folder and get relevant images from those folders
            Management.GetFolder();     // Prompt user to select folder containing images
            if (Management.imageFolder == "") // Exit if cancel is selected
            {
                return;
            }

            Management.GetFiles(1);     // Get images for series 1
            if (Properties.Settings.Default.fileContains2 != String.Empty)
            {
                Management.GetFiles(2); // Get images for series 2 if whitelist is not empty string
            }         

            // Exit the method if the list of image files cannot be counted -> I think this was to handle when "cancel" is pressed but may be able to be removed
            try
            {
                Management.imgCount1 = Management.imageFiles1.Count();
                Management.imgCount2 = Management.imageFiles2.Count();
            }
            catch (System.ArgumentNullException)
            {
                return;
            }

            // Ensure image count is same for both series
            if (Management.imgCount2 != 0 && Management.imgCount1 != Management.imgCount2)
            {
                MessageBox.Show($"Inconsistent numbers of files between Series 1 and Series 2.\nSeries 1 contains {Management.imgCount1} images and Series 2 contains {Management.imgCount2} images.\nPlease check the folder contents or adjust the filters in the preferences.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Initiate image display sequence if selected folder contains valid files
                if (Management.imgCount1 == 0)
            {
                // Display error if selected folder does not contain valid files
                MessageBox.Show("Please choose a folder containing valid images or check filters in preferences.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update label for folder location
            label1.Text = $"Folder: {Path.GetDirectoryName(Management.imageFiles1[0])}";

            // Get pixel values
            GetPixelVals();

            // Ensure pixel values don't contain errors
            string pixelValError = PixelValErrors();
            if (pixelValError != "")
            {
                MessageBox.Show($"The following files could not be parsed. Please delete them from the folder or change allowed file extensions and image name whitelist in the preferences.{pixelValError}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit method when one or more pixel vals are zero
            }

            // Get pixel differences
            GetPixelDiffs();

            // Modify pixel vals that need to be zero according to setting in preferences
            CleanPixelDiffs();

            // Save pixel val max to global variable for use when plotting playback position in chart, and set threshold line
            Management.pixelDiffMax = Management.pixelDiff.Max() * 100;
            Management.threshold = Management.pixelDiffMax;

            // Calculate timestamps
            CalcTimestamps();

            // Check to ensure there is same number of timestamps as image files
            if (Management.absTimeStamps.Count() != Management.imgCount1)
            {
                MessageBox.Show($"The image timestamps could not be parsed correctly. There are {Management.imgCount1} image files but only {Management.absTimeStamps.Count()} timestamps.\n\nThe program can proceed but the chart will only show a single X axis.\n\nAlternatively you can check the image timestamps and then reload the folder.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Management.absTimeStamps.Clear();
            }

            // Create sleep comments list
            for (int i = 0; i < Management.imgCount1; i++)
            {
                Management.sleepComments.Add(String.Empty);
            }

            // Allow comments
            textBox1.Enabled = true;

            // Plot the chart
            InitializeChart();
            PlotPixelDiff();
            PlotThresholdLine();
            LoadNewImage();

            // Allow process segmentation
            processSegmentationToolStripMenuItem.Enabled = true;
            Management.allowProcessSegmentation = true;
        }
        private void MenuFileExitProgram()
        // This method is used to exit the program.
        {
            this.Close();
        }
        private void MenuEditPreferences()
        // This method is used to configure and launch the preferences form (Form6).
        {
            Form6 preferences = new Form6();
            preferences.FormBorderStyle = FormBorderStyle.FixedDialog;
            preferences.MaximizeBox = false;
            preferences.MinimizeBox = false;
            preferences.ControlBox = false;
            preferences.StartPosition = FormStartPosition.CenterScreen;
            preferences.ShowDialog();
        }
        private void MenuViewEnterFullScreen()
        // This method is used to toggle the program into full screen mode.
        {
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            Management.fullScreenState = true;
        }
        private void MenuViewExitFullScreen()
        // This method is used to toggle the program out of full screen mode back to original size.
        {
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Normal;
            Management.fullScreenState = false;
        }
        private void MenuViewToggleFullScreen()
        // This method is use to toggle between full screen and windowed mode
        {
            if (Management.fullScreenState == true)
            {
                MenuViewExitFullScreen();
            }
            else
            {
                MenuViewEnterFullScreen();
            }
        }
        private void MenuFileProcessSegmentation()
        // Segmentation cannot be processed if image files have not yet been loaded
        // If segmentation has already been processed button changes to unprocess segmentation
        {
            if (Management.segmentationProcessed == true)
            {
                DialogResult dialogResult = MessageBox.Show("Sleep position segmentation already exists for this image series. If you proceed, existing data will be deleted. Are you sure you want to proceed?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes)
                {
                    UnPlotSegmentation();
                    ClearSegmentation();
                    Management.segmentationProcessed = false;
                    processSegmentationToolStripMenuItem.Text = "Process segmentation (F3)";
                    LoadNewImage();
                    return;
                }
                else if (dialogResult == DialogResult.No)
                {
                    return;
                }
            }

            // Process sleep segmentation
            Management.sleepIndex.Add(0);          
            int lookahead = Convert.ToInt32(Properties.Settings.Default.peakLookAhead);
            for (int i = 0; i < Management.pixelDiff.Count(); i++)
            {
                if (Management.pixelDiff[i] * 100 > Management.threshold)
                {
                    if (lookahead == 0)
                    {
                        Management.sleepIndex.Add(i + 1);
                    }
                    else
                    {
                        double[] tempArray = new double[lookahead];
                        int lookaheadMax = lookahead + i;
                        for (int j = i; j < lookaheadMax; j++)
                        {
                            tempArray[j - i] = Management.pixelDiff[j];
                        }
                        Management.sleepIndex.Add(Array.IndexOf(tempArray, tempArray.Max()) + i + 1);
                        i = lookaheadMax - 1;
                    }
                }
            }
            // Create sleep classification list
            for (int i = 0; i < Management.sleepIndex.Count(); i++)
            {
                Management.sleepPos.Add(0);
            }

            // Enable segmentation buttons
            Management.segmentationProcessed = true;
            processSegmentationToolStripMenuItem.Text = "Clear segmentation (F3)";

            // Plot segmentation and load image
            PlotSegmentation();
            LoadNewImage();

            // Allow export to CSV
            printReportToolStripMenuItem.Enabled = true;
            Management.allowExport2CSV = true;
        }
        private void ClearSegmentation()
        // Clears segmentation related variables
        {
            Management.segmentationProcessed = false;
            Management.sleepIndex.Clear();
            Management.sleepPos.Clear();       
        }
        private void MergeSegment()
        // Deletes segments
        // Only operates when playback position on segment line
        {
            // Exit if sleep segmentation not processed yet
            if (Management.segmentationProcessed == false)
            {
                return;
            }

            if (Management.sleepIndex.Contains(Management.imageCounter + 1))
            {
                int indexRemove = Management.sleepIndex.IndexOf(Management.imageCounter + 1);

                Management.sleepIndex.RemoveAt(indexRemove);
                Management.sleepPos.RemoveAt(indexRemove);
                UnPlotSegmentation();
                PlotSegmentation();
                LoadNewImage();
            }
        }
        private void CutSegment()
        // Inserts new segment lines onto the timeline
        {
            // Exit if sleep segmentation not processed yet
            if (Management.segmentationProcessed == false)
            {
                return;
            }

            // Disallow cut on first frame
            if (Management.imageCounter == 0)
            {
                return;
            }
            // Disallow cut on final frame
            if (Management.imageCounter + 1 == Management.imgCount1)
            {
                return;
            }
            // Disallow cut if frame already a segment line
            if (Management.sleepIndex.Contains(Management.imageCounter + 1))
            {
                return;
            }

            int indexInsert = Management.GetSleepIndex() + 1;

            Management.sleepIndex.Insert(indexInsert, Management.imageCounter + 1);
            Management.sleepPos.Insert(indexInsert, 0);

            UnPlotSegmentation();
            PlotSegmentation();
            LoadNewImage();
        }

        ///***********************************************************************
        // Tool strip menu item actions go here
        private void openImageFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MenuFileLoadImages();
        }
        private void processSegmentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MenuFileProcessSegmentation();
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MenuFileExitProgram();
        }
        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MenuEditPreferences();
        }
        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MenuViewToggleFullScreen();
        }
        private void printReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Management.Export2CSV();
        }
        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ButtonHelp();
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form5 about = new Form5();
            about.FormBorderStyle = FormBorderStyle.FixedDialog;
            about.MaximizeBox = false;
            about.MinimizeBox = false;
            about.ControlBox = false;
            about.StartPosition = FormStartPosition.CenterScreen;
            about.ShowDialog();
        }



        ///***********************************************************************
        // Methods called by buttons go here
        private void ButtonNavigateBack()
        // An if statement ensures that this button is only active while the timer is running and image files are loaded.
        // It is also inactive when the first image in the series is displayed to avoid index error when seeking backwards.
        // If active, Management.imageCounter is decremented by 1 and LoadNewImage()
        // Due to an issue with the timer, on the first occurance the button is pressed, Management.imageCounter must be decremented twice.
        // The if statement using the boolean Management.firstPress achieves this.
        {
            if (myTimer.Enabled == false && Management.imgCount1 != 0 && Management.imageCounter != 0)
            {
                if (Management.firstPress)
                {
                    Management.imageCounter--;
                    Management.firstPress = false;
                }
                Management.imageCounter--;
                LoadNewImage();
            }
        }
        private void ButtonStop()
        // The timer is stopped, Management.imageCounter is set to 0, and LoadNewImage() is called.
        {
            myTimer.Stop();
            Management.imageCounter = 0;
            if (Management.imgCount1 != 0)
            {
                LoadNewImage();
            }
        }
        private void ButtonPlay()
        // An if statement ensures this button is only active when Management.imgCount != 0 and when pressed the timer is started.
        {
            if (Management.imgCount1 != 0)
            {
                myTimer.Start();
            }
        }
        private void ButtonPause()
        // The timer is stopped and Management.firstPress is set to true.
        {
            myTimer.Stop();
            Management.firstPress = true;

        }
        private void ButtonNavigateForward()
        // An if statement ensures that this button is only active while the timer is running and image files are loaded.
        // It is also inactive when the final image in the series is displayed to avoid index error when seeking forwards.
        // Due to an issue with the timer, on the first occurance the button is pressed, Management.imageCounter does not need to be incremented.
        // The if statement using the boolean Management.firstPress achieves this.
        {
            if (myTimer.Enabled == false && Management.imgCount1 != 0 && Management.imageCounter != (Management.imgCount1 - 1))
            {
                if (Management.firstPress)
                {
                    Management.imageCounter--;
                    Management.firstPress = false;
                }
                Management.imageCounter++;
                LoadNewImage();
            }
        }
        private void ButtonThresholdUp()
        // Management.threshold is incremented by 0.5 and UpdateThreshold() is called to update the value displated in the GUI.
        // If the chart is displayed (Management.chartShown == true), PlotThresholdLine() is called to update the threshold line in the chart.
        {
            Management.threshold += 0.025;
            UpdateThreshold();
            if (Management.chartShown == true);
            {
                PlotThresholdLine();
            }
        }
        private void ButtonThresholdDown()
        // If Management.threshold == 0.5, an error is shown to notify the user that the threshold cannot be below 0.5.
        // Else Management.threshold is decremented by 0.5 and UpdateThreshold() is called to update the value displated in the GUI.
        // If the chart is displayed (Management.chartShown == true), PlotThresholdLine() is called to update the threshold line in the chart.
        {
            if (Management.threshold == 0.025)
            {
                MessageBox.Show("Cutoff cannot be lower than 0.025%", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Management.threshold -= 0.025;
                UpdateThreshold();
            }
            if (Management.chartShown == true) ;
            {
                PlotThresholdLine();
            }
        }
        private void ButtonTimerDown()
        // If Management.waitTime == 2, an error is shown to notify the user that the threshold cannot be below 2ms.
        // Else Management.waitTime is divided by two and UpdateDisplayTime() is called.
        {
            if (Management.waitTime == 2)
            {
                MessageBox.Show("Display time cannot be lower than 2 milliseconds", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Management.waitTime = Management.waitTime / 2;
                UpdateDisplayTime();
            }
        }
        private void ButtonTimerUp()
        // Management.waitTime is multiplied by two and UpdateDisplayTime() is called.
        {
            Management.waitTime = Management.waitTime * 2;
            UpdateDisplayTime();
        }
        private void ButtonTogglePlayPause()
        // Used to toggle the play state between pause and play
        {
            if (myTimer.Enabled == false)
            {
                ButtonPlay();
            }
            else
            {
                ButtonPause();
            }
        }
        private void ButtonToggleImageSeries()
        // This button is used to toggle wether Series 1 or Series 2 images are displayed in the picturebox by changing Management.displaySeries1
        {
            // Exit if series 2 contains no images
            if (Management.imgCount2 == 0)
            {
                return;
            }

            if (Management.displaySeries1)
            {
                Management.displaySeries1 = false;
            }
            else
            {
                Management.displaySeries1 = true;
            }
            if (myTimer.Enabled == false)
            {
                LoadNewImage();
            }
        }
        private void ButtonSegmentBack()
        // This button is used to navigate to the beginning of the previous sleep image segment
        {
            // Exit if sleep segmentation not processed yet
            if (Management.segmentationProcessed == false)
            {
                return;
            }

            if (Management.segmentationProcessed && Management.imgCount1 != 0)
            {
                if (Management.GetSleepIndex() == 0 || Management.sleepIndex[1] == Management.imageCounter + 1)
                {
                    Management.imageCounter = 0;
                }
                else if (Management.sleepIndex.Contains(Management.imageCounter + 1))
                {
                    Management.imageCounter = Management.sleepIndex[Management.GetSleepIndex() - 1] - 1;
                }
                else
                {
                    Management.imageCounter = Management.sleepIndex[Management.GetSleepIndex()] - 1;
                }
                if (myTimer.Enabled == false)
                {
                    LoadNewImage();
                }            
            }
        }
        private void ButtonSegmentForward()
        // This button is used to navigate to the beginning of the next sleep image segment
        {
            // Exit if sleep segmentation not processed yet
            if (Management.segmentationProcessed == false)
            {
                return;
            }

            if (Management.segmentationProcessed && Management.imgCount1 != 0 && Management.GetSleepIndex() != (Management.sleepIndex.Count() - 1))
            {
                Management.imageCounter = Management.sleepIndex[Management.GetSleepIndex() + 1] - 1;
                if (myTimer.Enabled == false)
                {
                    LoadNewImage();
                }          
            }
        }
        private void ButtonClassifySleepSegment()
        // This button opens Form4 which allows the user to classify sleep segments
        {
            // Exit if sleep segmentation not processed yet
            if (Management.segmentationProcessed == false)
            {
                return;
            }
            
            // Pause playback
            if (myTimer.Enabled == true)
            {
                ButtonPause();
            }

            // Open classify window
            Form4 classify = new Form4();
            classify.FormBorderStyle = FormBorderStyle.FixedDialog;
            classify.MaximizeBox = false;
            classify.MinimizeBox = false;
            classify.ControlBox = false;
            classify.StartPosition = FormStartPosition.CenterScreen;
            classify.ShowDialog();

            // Load image to refresh display
            LoadNewImage();
        }
        private void ButtonHelp()
        // This button opens help.pdf
        {
            Process.Start(".\\help.pdf");
        }


        ///***********************************************************************
        // Buttons actions go here
        private void button7_Click(object sender, EventArgs e)                          // Navigate back button
        {
            ButtonNavigateBack();
        }
        private void button6_Click(object sender, EventArgs e)                          // Stop button
        {
            ButtonStop();
        }
        private void button4_Click(object sender, EventArgs e)                          // Play button
        {
            ButtonPlay();
        }
        private void button5_Click(object sender, EventArgs e)                          // Pause button
        {
            ButtonPause();
        }
        private void button8_Click(object sender, EventArgs e)                          // Navigate forward button
        {
            ButtonNavigateForward();
        }
        private void button1_Click(object sender, EventArgs e)                          // Decrease playback speed
        {
            ButtonTimerUp();
        }
        private void button2_Click(object sender, EventArgs e)                          // Increase playback speed 
        {
            ButtonTimerDown();
        }
        private void button3_Click(object sender, EventArgs e)                          // Button toggle image series
        {
            ButtonToggleImageSeries();
        }
        private void button10_Click(object sender, EventArgs e)                         // Previous sleep segment
        {
            ButtonSegmentBack();
        }
        private void button11_Click(object sender, EventArgs e)                         // Next sleep segment
        {
            ButtonSegmentForward();
        }
        private void button9_Click(object sender, EventArgs e)                          // Cut sleep segment
        {
            CutSegment();
        }
        private void button17_Click(object sender, EventArgs e)                         // Merge sleep segment
        {
            MergeSegment();
        }
        private void button18_Click(object sender, EventArgs e)                         // Classify sleep segment
        {
            ButtonClassifySleepSegment();
        }


        ///***********************************************************************
        // Methods relating to keyboard shortcuts go here
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        // All keyboard shortcuts are defined in this method
        // From: http://stackoverflow.com/questions/400113/best-way-to-implement-keyboard-shortcuts-in-a-windows-forms-application
        {
            // Keyboard shorcuts relating to playback go here
            if (keyData == (Keys.Alt | Keys.Space))                                             // Toggle play pause when alt+space is pressed
            {
                ButtonTogglePlayPause();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Oemtilde))                                          // Stop playback when alt+tilde is pressed
            {
                ButtonStop();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Left))                                              // Navigate back when alt+left is pressed
            {
                ButtonNavigateBack();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Right))                                             // Navigate forward when alt+right is pressed
            {
                ButtonNavigateForward();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Oemplus))                                           // Decrease timer interval when alt+add is pressed
            {
                ButtonTimerDown();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.OemMinus))                                          // Increase timer interval when alt+minus is pressed
            {
                ButtonTimerUp();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.OemPipe))                                           // Toggle image display between Series 1 and Series 2
            {
                ButtonToggleImageSeries();
                LoadNewImage();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Down))                                              // Navigate back when alt+left is pressed
            {
                ButtonSegmentBack();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Up))                                                // Navigate forward when alt+right is pressed
            {
                ButtonSegmentForward();
                return true;
            }

            // Keyboard shortcuts relating to Toolstrip menu items go here
            if (keyData == (Keys.F1))                                                           // Open help when F1 is pressed
            {
                ButtonHelp();
                return true;
            }
            if (keyData == (Keys.F2))                                                           // Open image folder when F2 is pressed
            {
                MenuFileLoadImages();
                return true;
            }
            if (keyData == (Keys.F3))                                                           // Open process segmentation when F3 is pressed
            {
                MenuFileProcessSegmentation();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.F4))                                                // Close the program when alt+F4 is pressed
            {
                MenuFileExitProgram();
                return true;
            }
            if (keyData == (Keys.F7))                                                           // Export to CSV when F7 is pressed
            {
                Management.Export2CSV();
                return true;
            }
            if (keyData == (Keys.F8))                                                           // Open preferences form when F8 is pressed
            {
                MenuEditPreferences();
                return true;
            }
            if (keyData == (Keys.F11))                                                          // Toggle fullscreen when F11 is pressed
            {
                MenuViewToggleFullScreen();
                return true;
            }


            return base.ProcessCmdKey(ref msg, keyData);
        }

        ///***********************************************************************
        // Methods relating to tooltips go here
        private void button7_MouseHover(object sender, EventArgs e)
        // Tooltip for navigate back button
        {
            toolTip1.Show("Navigate backward by one frame (Alt Left)", button7);
        }
        private void button6_MouseHover(object sender, EventArgs e)
        // Tooltip for stop button
        {
            toolTip2.Show("Stop playback (Alt ~)", button6);
        }
        private void button4_MouseHover(object sender, EventArgs e)
        // Tooltip for play button
        {
            toolTip3.Show("Start playback (Alt Space)", button4);
        }
        private void button5_MouseHover(object sender, EventArgs e)
        // Tooltip for pause button
        {
            toolTip4.Show("Pause playback (Alt Space)", button5);
        }
        private void button8_MouseHover(object sender, EventArgs e)
        // Tooltip for navigate forward button
        {
            toolTip5.Show("Navigate forward by one frame (Alt Right)", button8);
        }
        private void button1_MouseHover(object sender, EventArgs e)
        // Tooltip for decrease playback speed button
        {
            toolTip6.Show("Decrease playback speed (Alt -)", button1);
        }
        private void button2_MouseHover(object sender, EventArgs e)
        // Tooltip for increase playback speed button
        {
            toolTip7.Show("Increase playback speed (Alt =)", button2);
        }
        private void button3_MouseHover(object sender, EventArgs e)
        // Tooltip for toggle image display button
        {
            toolTip8.Show("Toggle image display (Alt |)", button3);
        }
        private void button10_MouseHover(object sender, EventArgs e)
        // Tool tip for previous sleep segment button
        {
            toolTip9.Show("Navigate to start of previous sleep segment (Alt down)", button10);
        }
        private void button11_MouseHover(object sender, EventArgs e)
        // Tool tip for next sleep segment button
        {
            toolTip10.Show("Navigate to start of next sleep segment (Alt up)", button11);
        }
        private void button9_MouseHover(object sender, EventArgs e)
        // Tool tip for insert sleep segment button
        {
            toolTip11.Show("Insert marker to create new sleep segment", button9);
        }
        private void button17_MouseHover(object sender, EventArgs e)
        // Tool tip for remove sleep segments button
        {
            toolTip12.Show("Remove marker to merge sleep segments", button17);
        }
        private void button18_MouseHover(object sender, EventArgs e)
        // Tool tip for categorise sleep segment button
        {
            toolTip13.Show("Categorise sleep segment", button18);
        }

        ///***********************************************************************
        // Methods relating to textbox go here
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Management.sleepComments[Management.imageCounter] = textBox1.Text;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return;
            }
        }

        

        //***********************************************************************
    }
}
