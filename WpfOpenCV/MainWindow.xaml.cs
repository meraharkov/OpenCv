using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using WpfOpenCV.Entities;
using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;
using Image = System.Windows.Controls.Image;


namespace WpfOpenCV
{ 
    public partial class MainWindow : Window
    {
        private static List<WebCamDevice> _webCamDevices;
        private CvCapture cap = null;
        private List<BackgroundWorker>  _backgroundWorkers= new List<BackgroundWorker>();
        private List<WebCameraInfo> _webCameraInfos = new List<WebCameraInfo>();

        private const string Size640 = "640x480";
        private const string Size800 = "800x600";
        private const string Size1024 = "1024x768";
        private const string Size1152 = "1152x864";
        private const string Size1280 = "1280x960";



        private int WindowWidth { get; set; }
        private int WindowHeight { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            
            _webCamDevices = VideoHelper.GetWebCamDevices();
            if (_webCamDevices != null && _webCamDevices.Any())
            {
                FillToolStripComboBox();
            }

            this.Title = "RPX-CAM";
        }


        private void CaptureCamera(BackgroundWorker worker, string webCamIndexName, int cameraIndex)
        {
            worker.DoWork += ReadCamera;
            worker.ProgressChanged += worker_ProgressChanged;
        }

        private void ReadCamera(object sender, DoWorkEventArgs e)
        {
            var videoFrameContainer = (WebCameraInfo)e.Argument;
            int indexWebCamera = videoFrameContainer.IndexWebCamera;
            CvCapture capture;
            using ( capture = CvCapture.FromCamera( indexWebCamera))
                {

                    capture.FrameWidth = WindowWidth; //320;//
                    capture.FrameHeight = WindowHeight; //240; //
                    IplImage frame;
                    while (true)
                    {
                       

                        frame = Cv.QueryFrame(capture);

                        if (frame != null)
                        {
                            var videoFrame = new VideoFrameContainer()
                            {
                                Name = videoFrameContainer.AdditionalCameraInfo.CameraIndex,
                                Frame = frame
                            };
                            _backgroundWorkers[indexWebCamera].ReportProgress(0, videoFrame);
                        }
                        else
                        {
                            capture.Dispose();
                            capture = CvCapture.FromCamera(indexWebCamera);
                        }
                    }
                }
        }


        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        { 
           if (e.UserState != null)
           {
               var videoFrame = (VideoFrameContainer)e.UserState;
               var image = videoFrame.Frame;

               try
               {
                   var imageSource = MainGrid.Children.OfType<Image>().First(x => x.Name == videoFrame.Name);
                   imageSource.Source = image.ToWriteableBitmap();
               }
               catch (Exception exception)
               {
                   var message = exception.Message;
               }
           } 
        }
        
        public void FillToolStripComboBox()
        {
            if (_webCamDevices != null && _webCamDevices.Any())
            {
                CreationVideoFramesForCameras(_webCamDevices);
                RunWorkers();
            }
        }

        private void CreationVideoFramesForCameras(List<WebCamDevice> webCamDevices)
        {
            CreateColumn(webCamDevices.Count);
            CreateRows(webCamDevices.Count);
            
            Image image;
            int cameraIndex = 0;

            for (int row = 0; row < MainGrid.RowDefinitions.Count; row++)
            {
                for (int column = 0; column < MainGrid.ColumnDefinitions.Count; column++)
                {
                    if (cameraIndex == webCamDevices.Count)
                        break;

                    image = new Image();
                    image.Name = "image" + cameraIndex.ToString();
                    image.IsEnabled = true;
                    image.SetValue(Grid.ColumnProperty, column);
                    image.SetValue(Grid.RowProperty, row);

                    MainGrid.Children.Add(image);
                    SetupWorker(image.Name, cameraIndex);

                    AddInfoAboutCamera(image.Name, column, row,  webCamDevices[cameraIndex].Name, cameraIndex);
                    cameraIndex++;
                }
            }
        }

        private void AddInfoAboutCamera(string nameImageCtrl, int numberColumn, int rowNumber, string deviceName, int cameraIndex)
        {
            _webCameraInfos.Add(new WebCameraInfo()
            {
                AdditionalCameraInfo = new AdditionalCameraInfo()
                {

                    CameraIndex = nameImageCtrl,
                    NumberColumnInGrid = numberColumn,
                    NumberRowInGrid = rowNumber
                },
                DeviceName = deviceName,
                IndexWebCamera = cameraIndex

            });
        }

        private void CreateColumn(int countWebCameras)
        {
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition(){Name = "firstColumn"});
            if (countWebCameras > 1)
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Name = "secondtColumn"
                });
            
        }

        private void CreateRows(int countWebCameras)
        {
           var countColumns = 2;
           double countRow = (double)countWebCameras/countColumns;
           var countRows = (int)Math.Ceiling(countRow);

            for (int i = 0; i < countRows; i++)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition());
            }
        }

        private void SetupWorker(string nameVideoFrame, int cameraIndex)
        {
            var backgroundWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            _backgroundWorkers.Add(backgroundWorker);

            CaptureCamera(backgroundWorker, nameVideoFrame, cameraIndex);
        }

        private void RunWorkers()
        {
            for (int i = 0; i < _backgroundWorkers.Count; i++)
            {
                _backgroundWorkers[i].RunWorkerAsync(_webCameraInfos[i]);
            }
        }
        
        /* ========================================================== */

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var sizeName = new List<string>();

            sizeName.Add(Size640);
            sizeName.Add(Size800);
            sizeName.Add(Size1024);
            sizeName.Add(Size1152);
            sizeName.Add(Size1280);

            var comboBox = sender as ComboBox;
            comboBox.ItemsSource = sizeName;
            comboBox.SelectedIndex = 0;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var value = comboBox.SelectedItem as string;
          
            int width, height = 0;
            switch (value)
            {
                case Size640:
                    width = 640;
                    height = 480;
                    ResizeVideo(width, height);
                    break;
                case Size800:
                    width = 800;
                    height = 600;
                    ResizeVideo(width, height);
                    break;

                case Size1024:
                    width = 1024;
                    height = 768;
                    ResizeVideo(width, height);
                    break;

                case Size1152:
                    width = 1152;
                    height = 864;
                    ResizeVideo(width, height);
                    break;

                case Size1280:
                    width = 1280;
                    height = 960;
                    ResizeVideo(width, height);
                    break;
            }
        }

        private void ChangeCamera(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var camerInfo = (AdditionalCameraInfo)comboBox.SelectedValue;

            if (camerInfo.CameraIndex != null)
            {
                ShowOneCamera(camerInfo);
            }
            else
            {
                ShowAllCameras();
            }
        }

        private void ShowOneCamera(AdditionalCameraInfo camerInfo)
        {
            var videoFrames = MainGrid.Children.OfType<Image>().ToList();

            HideAllVideoFrames(videoFrames);

            ShowVideoFrameByCameraIndex(videoFrames, camerInfo);
        }

        private void ShowVideoFrameByCameraIndex(List<Image> videoFrames, AdditionalCameraInfo camerInfo)
        {
            var imageSource = videoFrames.First(x => x.Name == camerInfo.CameraIndex);
            imageSource.Visibility = System.Windows.Visibility.Visible;

            var currentColumn = MainGrid.ColumnDefinitions[camerInfo.NumberColumnInGrid];
            currentColumn.Width = new GridLength(1, GridUnitType.Star);

            var currentRow = MainGrid.RowDefinitions[camerInfo.NumberRowInGrid];
            currentRow.Height = new GridLength(1, GridUnitType.Star);
        }

        private void HideAllVideoFrames(List<Image> videoFrames)
        {
            foreach (var frame in videoFrames)
            {
                frame.Visibility = System.Windows.Visibility.Hidden;
            }

            foreach (var column in MainGrid.ColumnDefinitions)
            {
                column.Width = new GridLength(0, GridUnitType.Star);
            }

            foreach (var row in MainGrid.RowDefinitions)
            {
                row.Height = new GridLength(0, GridUnitType.Star);
            }
        }

        private void ShowAllCameras()
        {
            var videoFrames = MainGrid.Children.OfType<Image>().ToList();

            foreach (var frame in videoFrames)
            {
                frame.Visibility = System.Windows.Visibility.Visible;
            }

            foreach (var column in MainGrid.ColumnDefinitions)
            {
                column.Width = new GridLength(50, GridUnitType.Star);
            }

            foreach (var row in MainGrid.RowDefinitions)
            {
                row.Height = new GridLength(100, GridUnitType.Star);
            }
        }

        private void LoadCameras(object sender, RoutedEventArgs e)
        {
            var nameDevices = new List<Dictionary<AdditionalCameraInfo, string>>();

            var allCameras = new Dictionary<AdditionalCameraInfo, string>();
            allCameras.Add(new AdditionalCameraInfo(), "All Cameras");
            nameDevices.Add(allCameras);


            foreach (var camera in _webCameraInfos)
            {
                var dict = new Dictionary<AdditionalCameraInfo, string>();
                dict.Add(camera.AdditionalCameraInfo, camera.DeviceName);
                nameDevices.Add(dict);
            }

            var comboBox = sender as ComboBox;
            comboBox.ItemsSource = nameDevices;
            comboBox.SelectedIndex = 0;
        }

        private void ResizeVideo(int width, int height)
        {
            Application.Current.MainWindow.Width = width;
            Application.Current.MainWindow.Height = height;

            WindowWidth = width;
            WindowHeight = height;

        }
    }
}
