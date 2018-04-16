//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System;
    using System.Linq;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Threading;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        string drive = "D:\\";
        string loggingDirectory = "sleepData\\";
        string sessionDirectory;
        
        // Create a timer for saving the frames at a regular interval
        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        int hardCodedSecondInterval = 8; // 8 seconds between samples
       
        
        /// <summary>
        ///  /******** Details for Depth Map ***********************
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        /// <summary>
        /// /******** Details for colour map ***********************
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly uint bytesPerPixel = 0;

        /// <summary>
        /// /******** Details for Infrared ***********************
        /// Maximum value (as a float) that can be returned by the InfraredFrame
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        /// <summary>
        /// The value by which the infrared source data will be scaled
        /// </summary>
        private const float InfraredSourceScale = 0.75f;

        /// <summary>
        /// Smallest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// Largest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        /*****************************************************************/
        
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for infrared frames
        /// </summary>
        private InfraredFrameReader infraredFrameReader = null;

        /// <summary>
        /// Description (width, height, etc) of the infrared frame data
        /// </summary>
        private FrameDescription infraredFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap infraredBitmap = null;

        /// <summary>
        /// Reader for depth frames
        /// </summary>
        private DepthFrameReader depthFrameReader = null;

        /// <summary>
        /// Description of the data contained in the depth frame
        /// </summary>
        private FrameDescription depthFrameDescription = null;
            
        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap depthBitmap = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] depthPixels = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        /// <summary>
        /// Intermediate storage for receiving frame data from the sensor
        /// </summary>
        private byte[] colorPixels = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;


        private void initializeTimer()
        {
            SliderLegend.Visibility = System.Windows.Visibility.Hidden;
            SliderValueDisplay.Visibility = System.Windows.Visibility.Hidden;
            SamplingInterval.Visibility = System.Windows.Visibility.Hidden;
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            //dispatcherTimer.Interval = new TimeSpan(0, 0, (int)SamplingInterval.Value);
            // hard code for trial
            dispatcherTimer.Interval = new TimeSpan(0, 0, hardCodedSecondInterval);
            
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {

            // write depth image to disk
            if (depthCheckbox.IsChecked.Value)
            {
                saveDepthImage();
            }

            // write colour image to disk
            if (colourCheckbox.IsChecked.Value)
            {
                saveColourImage();
            }

            // write infrared image to disk
            if (infraCheckbox.IsChecked.Value)
            {
                saveInfraImage();
            }

        }




        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // setup logging directory

            // check that the drive exists
            var drives = DriveInfo.GetDrives();
            if (drives.Where(data =>data.Name == drive).Count() !=1)
            {
                // if logging drive does not exist (usually D), set logging drive to C drive.
                drive = "C:\\";
            }
            // check that the main logging folder exists, otherwise create it
            if (!Directory.Exists(@drive+loggingDirectory))
            {
                Directory.CreateDirectory(@drive + loggingDirectory);
            }


            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // For Colour Frames

            // open the reader for the color frames
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // rgba is 4 bytes per pixel
            this.bytesPerPixel = colorFrameDescription.BytesPerPixel;

            // allocate space to put the pixels to be rendered
            this.colorPixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * this.bytesPerPixel];

            // create the bitmap to display
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // end For Colour Frames

            // open the reader for the depth frames
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            // wire handler for frame arrival
            this.depthFrameReader.FrameArrived += this.Reader_FrameArrived;

            // get FrameDescription from DepthFrameSource
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            // create the bitmap to display
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            //For Infrared Frames
            // open the reader for the infrared frames
            this.infraredFrameReader = this.kinectSensor.InfraredFrameSource.OpenReader();

            // wire handler for frame arrival
            this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;

            // get FrameDescription from InfraredFrameSource
            this.infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;

            // create the bitmap to display
            this.infraredBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);

            // end of infrared frames

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;



            // initialize the components (controls) of the window
            this.InitializeComponent();


            // initialize the timer
            initializeTimer();

        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource DepthImageSource
        {
            get
            {
                return this.depthBitmap;
            }
        }

        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource InfraRedImageSource
        {
            get
            {
                return this.infraredBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.depthFrameReader != null)
            {
                // DepthFrameReader is IDisposable
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }

            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.infraredFrameReader != null)
            {
                // InfraredFrameReader is IDisposable
                this.infraredFrameReader.Dispose();
                this.infraredFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.depthBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.depthBitmap));

                string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectScreenshot-Depth-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }



        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void saveColourImage()
        {
            if (this.colorBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .jpg file
                BitmapEncoder encoder = new JpegBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

                //string time = System.DateTime.Now.ToString("yyyyMMdd'-'hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                string time = System.DateTime.Now.ToString("yyyyMMdd'-'HH'-'mm'-'ss");

                string myPhotos = drive + loggingDirectory + sessionDirectory;

                string path = Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".jpg");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }


        private void saveDepthImage()
        {
            if (this.depthBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.depthBitmap));

                //string time = System.DateTime.Now.ToString("yyyyMMdd'-'hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                string time = System.DateTime.Now.ToString("yyyyMMdd'-'HH'-'mm'-'ss");

                string myPhotos = drive + loggingDirectory + sessionDirectory;

                string path = Path.Combine(myPhotos, "KinectScreenshot-Depth-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }

        /// <summary>
        /// Saving the infrared image
        /// </summary>
        ///
        private void saveInfraImage()
        {
            if (this.infraredBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();


                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.infraredBitmap));

               // string time = System.DateTime.Now.ToString("yyyyMMdd'-'hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                string time = System.DateTime.Now.ToString("yyyyMMdd'-'HH'-'mm'-'ss");

                string myPhotos = drive + loggingDirectory + sessionDirectory;

                string path = Path.Combine(myPhotos, "KinectScreenshot-Infrared-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            bool colorFrameProcessed = false;

            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                    {
                        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                        {
                            colorFrame.CopyRawFrameDataToArray(this.colorPixels);
                        }
                        else
                        {
                            colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, ColorImageFormat.Bgra);
                        }

                        colorFrameProcessed = true;
                    }
                }
            }

            // we got a frame, render
            if (colorFrameProcessed)
            {
                this.RenderColorPixels();


            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderColorPixels()
        {
            this.colorBitmap.WritePixels(
                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                this.colorPixels,
                this.colorBitmap.PixelWidth * (int)this.bytesPerPixel,
                0);
        }


        /// <summary>
        /// Handles the depth frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance
                            
                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }


                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();

            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }


        /// <summary>
        /// Handles the infrared frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            // InfraredFrame is IDisposable
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    // the fastest way to process the infrared frame data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                    {
                        // verify data and write the new infrared frame data to the display bitmap
                        if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / this.infraredFrameDescription.BytesPerPixel)) &&
                            (this.infraredFrameDescription.Width == this.infraredBitmap.PixelWidth) && (this.infraredFrameDescription.Height == this.infraredBitmap.PixelHeight))
                        {
                            this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrameData">Pointer to the InfraredFrame image data</param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            this.infraredBitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this.infraredBitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / this.infraredFrameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            this.infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, this.infraredBitmap.PixelWidth, this.infraredBitmap.PixelHeight));

            // unlock the bitmap
            this.infraredBitmap.Unlock();
        }



        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dispatcherTimer.Interval = new TimeSpan(0, 0, (int)SamplingInterval.Value);
            //SliderValueDisplay.Text = ((int)SamplingInterval.Value).ToString() + " seconds";
            SliderValueDisplay.Text = ("Disabled for test");
        }

        private bool going = false;

        private void log(string logMessage)
        {
            using (StreamWriter w = File.AppendText(drive+loggingDirectory+sessionDirectory+"\\log.txt"))
            {
                w.WriteLine("\r\nLog Entry : ");
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                w.WriteLine("  :");
                w.WriteLine("  :{0}", logMessage);
                w.WriteLine("-------------------------------");
            }

        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            if (going)
            {
                dispatcherTimer.Stop();
                log("Logging Stopped");
                GoButton.Content = "Go!";
                toggleControlActivation();
                going = false;
                this.StatusText = "logging stopped....";
            }
            else
            {
                toggleControlActivation();
                // create logging session directory
                sessionDirectory = System.DateTime.Now.ToString("yyyyMMdd'-'hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                Directory.CreateDirectory(@drive + loggingDirectory + sessionDirectory);

                // write logging started event to session directory
                log("Logging Started");


                dispatcherTimer.Start();
                GoButton.Content = "Stop!";
                going = true;
                this.StatusText = "logging started....";
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        void toggleControlActivation()
        {
            colourCheckbox.IsEnabled = !colourCheckbox.IsEnabled;
            depthCheckbox.IsEnabled = !depthCheckbox.IsEnabled;
            infraCheckbox.IsEnabled = !infraCheckbox.IsEnabled;
            SamplingInterval.IsEnabled = !SamplingInterval.IsEnabled;
        }

    }
}
