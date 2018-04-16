using System;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;

namespace sleep.monitor
{
    class Management
    {

        ///***********************************************************************
        // Global variables used by Form1 (main form)

        public static string imageFolder = String.Empty;                                                    // Used to hold the name of the folder containing the images
        public static string[] imageFiles1 = new string[] { };                                              // Empty array of strings to hold list of files for Series1
        public static string[] imageFiles2 = new string[] { };                                              // Empty array of strings to hold list of files for Series2
        public static int imgCount1 = 0;                                                                    // Count of images in Series1
        public static int imgCount2 = 0;                                                                    // Count of images in Series2
        public static List<int> pixelVals = new List<int>();                                                // Used to store values of pixels for Series1
        public static List<double> pixelDiff = new List<double>();                                          // Used to store values of pixel differences for Series1
        public static List<DateTime> absTimeStamps = new List<DateTime>();                                  // Absolute value timestamps for each image
        public static List<TimeSpan> relTimeStampsTS = new List<TimeSpan>();                                // Relative timestamps as TimeSpan
        public static List<DateTime> relTimeStampsDT = new List<DateTime>();                                // Relative timestamps as DateTime

        public static bool displaySeries1 = false;                                                           // Determines which series of images will be displayed in picturebox
        public static int imageCounter = 0;                                                                 // Index for image to be displayed
        public static string displayImage = String.Empty;                                                   // String of image to be displayed

        public static bool chartShown = false;                                                              // To track whether or not chart is shown
        public static double pixelDiffMax = 0;                                                              // Maximum pixel difference value, used for y coordinate of playback line in chart
        public static double threshold = 0;                                                                 // Threshold line in chart
        public static bool chartTitle = false;                                                              // Used to ensure that chart title is only displayed once
        public static bool allowExport2CSV = false;                                                         // Prevents export to CSV prior to sleep segmentation
        public static bool allowProcessSegmentation = false;                                                // Prevents process segmentation prior to loading images

        public static bool fullScreenState = false;                                                         // Use to track whether or not program is in full screen mode

        public static int waitTime = Convert.ToInt32(Properties.Settings.Default.speedDefault);             // Interval time for tick of timer
        public static bool firstRun = true;                                                                 // Used to link the timer to the event handler only once the first time the program is run
        public static bool firstPress = true;                                                               // Used to adjust the behaviour of the forwards and back buttons due to interaction with the timer
        public static bool chartClick = false;                                                              // Used to change the behaviour of the timer when using real time scrubbing
        public static int waitTimeOld = 0;                                                                  // Used to save the value of the wait time when using real time scrubbing
        public static bool prevPlayState = true;                                                            // Used to save whether the program was in play state or paused before using real time scrubbing
        public static bool changeThreshold = false;                                                         // Determines whether the timer changes the Threshold line or the Playback line when in chart mode
        public static int frameSkip = 2;                                                                    // Number of frames to skip per tick of play time.

        public static List<int> sleepIndex = new List<int>();                                               // Used to store values of sleep segments
        public static List<int> sleepPos = new List<int>();                                                 // Used to store values relating to the classification of sleeping positions
        public static List<string> sleepComments = new List<string>();                                      // Used to store comments relating to the classification of sleeping positions
        public static bool segmentationProcessed = false;                                                   // Used to determine whether or not segmentation has been processed as this determines whether cutting and joining sleep positions is allowed

        ///***********************************************************************
        // Methods used by Form1
        public static int calcPixelValPixelWise(string imageFile1, string imageFile2)
        // This method receives two image locations as strings and returns the pixel-wise subtracted.
        // absolute val.
        // When the file is not a valid image, a try/catch statement is used to catch the error, and a pixel val of 0 is returned instead.
        {
            int pixelVal = new int();
            try
            {
                // load images
                Image<Gray, byte> myImage1 = new Image<Gray, byte>(imageFile1);
                Image<Gray, byte> myImage2 = new Image<Gray, byte>(imageFile2);
                // subtract images and take absolute difference value of differences
                myImage1 = myImage1.AbsDiff(myImage2);      
            
                // get sum of differences
                Gray sum = myImage1.GetSum();
                myImage1.Dispose(); myImage2.Dispose();// Dispose of object to reduce memory utilisation
                pixelVal = Convert.ToInt32(sum.Intensity);
                return pixelVal;
            }
            catch (System.ArgumentException)
            {
                return 0;
            }
        }



        public static int calcPixelVal(string imageFile)
        // This method receives the image location as a string and returns the pixel val for the image.
        // When the file is not a valid image, a try/catch statement is used to catch the error, and a pixel val of 0 is returned instead.
        {
            int pixelVal = new int();
            try
            {
                Image<Gray, byte> myImage = new Image<Gray, byte>(imageFile);
                Gray sum = myImage.GetSum();
                myImage.Dispose(); // Dispose of object to reduce memory utilisation
                pixelVal = Convert.ToInt32(sum.Intensity);
                return pixelVal;
            }
            catch (System.ArgumentException)
            {
                return 0;
            }
        }
        public static string humanReadableFileSize(string filename)
        // This method receives the image location as a string and returns the image file size in a human readable form.
        // Source: http://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = new FileInfo(filename).Length;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }
            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.##} {1}", len, sizes[order]);
            return result;
        }
        public static void GetFolder()
        // This method opens a File Browser Dialog to prompt the user to navigate to the folder where the image files are saved.
        {
            // Create and open folder dialog
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Properties.Settings.Default.folderDefault;
            fbd.ShowNewFolderButton = false;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            {
                imageFolder = "";
            }
            else
            {
                imageFolder = fbd.SelectedPath;
            }
        }
        public static void GetFiles(int seriesNo)
        // The contents of folder are parsed according to the settings saved in the properties in the fileExtn and fileContains fields.
        // An array of strings is returned once all the files in the folder have been parsed.
        {
            // Parse preferences relating to file selection
            string[] fileExtn = Properties.Settings.Default.fileExtn.Split(' ').ToArray();
            if (seriesNo == 1)
            {
                string[] fileContains = Properties.Settings.Default.fileContains1.Split(' ').ToArray();
                try // Error handling required when cancel is pressed
                {
                    imageFiles1 = Directory.EnumerateFiles(imageFolder, "*.*")
                        .Where(file => fileExtn.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                        .Where(file => fileContains.All(x => file.ToLower().Contains(x)))
                        .ToArray();
                }
                catch (System.ArgumentException)
                {
                    return;
                }
            }
            else
            {
                string[] fileContains = Properties.Settings.Default.fileContains2.Split(' ').ToArray();
                try
                {
                    imageFiles2 = Directory.EnumerateFiles(imageFolder, "*.*")
                    .Where(file => fileExtn.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                    .Where(file => fileContains.All(x => file.ToLower().Contains(x)))
                    .ToArray();
                }
                catch (System.ArgumentException)
                {
                    return;
                }
            }
        }
        public static int GetSleepIndex()
        // This method is used to translate imageCounter to sleepIndex
        {
            // Return zero to avoid System.InvalidOperationException when image counter is zero
            if (imageCounter == 0)
            {
                return 0;
            }
            
            // This part should be re written using Where
            List<int> tempList = new List<int>();
            foreach (int i in sleepIndex)
            {
                if (imageCounter + 2 > i)
                {
                    tempList.Add(i);
                }                  
            }
            return sleepIndex.IndexOf(tempList.Max());
        }
        public static TimeSpan[] GetTimes(int segmentNo)
        // This method received a segment number as an int and returns the DateTime values for the start and end of that segment retrieved from the list timeStamps
        {
            // DateTime[] timeStartEnd = new DateTime[2];
            TimeSpan[] timeStartEnd = new TimeSpan[2];
            if (segmentNo == 0) // First segment
            {
                timeStartEnd[0] = relTimeStampsTS[0];
                timeStartEnd[1] = relTimeStampsTS[sleepIndex[segmentNo + 1] - 1];
            }
            else if (segmentNo == sleepIndex.Count() - 1) // Last segment
            {
                timeStartEnd[0] = (relTimeStampsTS[sleepIndex[segmentNo] - 1]);
                timeStartEnd[1] = relTimeStampsTS.Last();
            }
            else // All other segments
            {
                timeStartEnd[0] = (relTimeStampsTS[sleepIndex[segmentNo] - 1]);
                timeStartEnd[1] = relTimeStampsTS[sleepIndex[segmentNo + 1] - 1];
            }
            return timeStartEnd;
        }
        public static void Export2CSV()
        // Exports program data to CSV file
        {
            if (allowExport2CSV == false)
            {
                return;
            }            
            
            // Output file
            string filePath = Path.Combine(imageFolder, "output.csv");

            // Show warning if output file already exists
            if (File.Exists(filePath))
            {
                DialogResult dialogResult = MessageBox.Show($"The output file {filePath} already exists. Would you like to overwrite it?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.No)
                {
                    return;
                }
            }

            // Header
            var csv = new StringBuilder();
            var newLine = "Client: ";
            csv.AppendLine(newLine);
            newLine = $"Generated: {DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss")}";
            csv.AppendLine(newLine);
            newLine = $"Folder: {imageFolder}";
            csv.AppendLine(newLine);
            newLine = $"Threshold value: {Math.Round(threshold,4)}%";
            csv.AppendLine(newLine);
            newLine = $"Total sleep time: {relTimeStampsTS.Last() - relTimeStampsTS.First()}";
            csv.AppendLine(newLine);
            newLine = $"Images in series: {imgCount1}";
            csv.AppendLine(newLine);
            newLine = "*************************************************************************************************";
            csv.AppendLine(newLine);

            // Sleep posture segments
            newLine = "Image #,Segment #,Image Name,Comment,Side,SSL,Position,Segment Duration,Elapsed Time,Time of Day";
            csv.AppendLine(newLine);

            // Create arrays to store summary data
            int catNum = 17; // Aligns to number of categories 16 + 1 for 0 for uncategorised
            TimeSpan[] sumTime = new TimeSpan[catNum];
            int[] sumOccurances = new int[catNum];
            int imageIndexNo = 0;
            int segmentNo = 0;
            int sleepPosNum = 0;

            if (Management.sleepIndex.Count() > 1)
            {
                for (int i = 0; i < imgCount1; i++)
                {
                    if (i == 0)
                    {
                        TimeSpan[] timeStartEnd = GetTimes(0);
                        TimeSpan timeDuration = timeStartEnd[1] - timeStartEnd[0];
                        newLine = $"1,1,{Path.GetFileName(imageFiles1.First())},{sleepComments.First()},,,{sleepPos.First()},{Math.Round(timeDuration.TotalMinutes,1)} minutes,{relTimeStampsTS.First()},{absTimeStamps.First()}";
                        csv.AppendLine(newLine);

                        sumTime[sleepPos.First()] = timeDuration;
                        sumOccurances[sleepPos.First()] = 1;
                    }
                    else
                    {
                        imageIndexNo = i - 1;
                        if (sleepIndex.Contains(i))
                        {
                            segmentNo = sleepIndex.IndexOf(i) + 1;
                            sleepPosNum = sleepPos[segmentNo - 1];
                            
                            TimeSpan[] timeStartEnd = GetTimes(segmentNo - 1);
                            TimeSpan timeDuration = timeStartEnd[1] - timeStartEnd[0];
                            newLine = $"{i},{segmentNo},{Path.GetFileName(imageFiles1[imageIndexNo])},{sleepComments[imageIndexNo]},,,{sleepPosNum},{Math.Round(timeDuration.TotalMinutes, 1)} minutes,{relTimeStampsTS[imageIndexNo]},{absTimeStamps[imageIndexNo]}";
                            csv.AppendLine(newLine);

                            sumTime[sleepPosNum] = sumTime[sleepPosNum] + timeDuration;
                            sumOccurances[sleepPosNum] = sumOccurances[sleepPosNum] + 1;

                            continue;
                        }
                        if (sleepComments[imageIndexNo] != String.Empty && imageIndexNo != 0)
                        {
                            newLine = $"{i},{segmentNo},{Path.GetFileName(imageFiles1[imageIndexNo])},{sleepComments[imageIndexNo]},,,,,{relTimeStampsTS[imageIndexNo]},{absTimeStamps[imageIndexNo]}";
                            csv.AppendLine(newLine);
                        }
                    }
                }
            }
            else // 1 segment
            {
                TimeSpan timeDuration = Management.relTimeStampsTS.Last() - Management.relTimeStampsTS.First();
                newLine = $"1,1,{Path.GetFileName(imageFiles1.First())},{sleepComments.First()},,,{sleepPos.First()},{Math.Round(timeDuration.TotalMinutes,1)} minutes,{relTimeStampsTS.First()},{absTimeStamps.First()}";
                csv.AppendLine(newLine);

                sumTime[sleepPos.First()] = timeDuration;
                sumOccurances[sleepPos.First()] = 1;

                for (int i = 0; i < imgCount1; i++)
                {
                    if (sleepComments[i] != String.Empty)
                    {
                        newLine = $"{i},1,{Path.GetFileName(imageFiles1[i])},{sleepComments[i]},,,,,{relTimeStampsTS[imageIndexNo]},{absTimeStamps[imageIndexNo]}";
                        csv.AppendLine(newLine);
                    }
                }
            }
            newLine = "*************************************************************************************************";
            csv.AppendLine(newLine);

            // Sleep posture summary
            newLine = "Sleep Position,Total Duration,% of Total,# Occurrences,Average Duration";
            csv.AppendLine(newLine);

            TimeSpan sumTimeTotal = sumTime.Aggregate((t1, t2) => t1 + t2);

            for (int i = 0; i < catNum; i++)
            {
                double timePercent = 0;
                if (sumTime[i].TotalMinutes != 0)
                {
                    timePercent = Math.Round(sumTime[i].TotalMinutes / sumTimeTotal.TotalMinutes * 100, 1);
                }

                double avDuration = 0;
                if (sumOccurances[i] != 0)
                {
                    avDuration = Math.Round(sumTime[i].TotalMinutes / sumOccurances[i], 1);
                }

                newLine = $"{i},{Math.Round(sumTime[i].TotalMinutes,1)} minutes,{timePercent},{sumOccurances[i]},{avDuration} minutes";
                //newLine = $"{i},{Math.Round(sumTime[i].TotalMinutes, 1)},{sumOccurances[i]},";
                csv.AppendLine(newLine);
            }
            // Write to file
            try
            {
                File.WriteAllText(filePath, csv.ToString());
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show($"The output file cannot be accessed. If you have it open in another program please close it and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }            
            MessageBox.Show($"Data written to file:\n{filePath}", "Process complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        ///***********************************************************************
        // Global variables used by Form2 (preferences form)

        public static string errorList = String.Empty;                                                      // Used to accumulate errors while parsing inputs in preferences form

        ///***********************************************************************
    }
}
