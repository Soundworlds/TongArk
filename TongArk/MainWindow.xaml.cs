//
//
// Licensed under the MIT license.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Timers;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
using VideoFrameAnalyzer;
using System.IO;
using NAudio.Wave;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.Win32;

namespace TongArk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : System.Windows.Window
    {
        // Main structure that holds list of emoptions
        // and their values returning by Azure server.
        public struct EmotionList
        {
            public float value;
            public string name;
        }

        private EmotionServiceClient _emotionClient = null;
        private readonly FrameGrabber<LiveCameraResult> _grabber = null;
        private static readonly ImageEncodingParam[] s_jpegParams = {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
        };
        private readonly CascadeClassifier _localFaceDetector = new CascadeClassifier();
        private bool _fuseClientRemoteResults;
        private LiveCameraResult _latestResultsToDisplay = null;
        private AppMode _mode;
        private DateTime _startTime;

        public enum AppMode
        {
            Emotions
        }

        // Software agent declaration.
        Agent softwareAgent;

        // NAudio WaveIn :: recording stream.
        WaveIn waveIn;


        public MainWindow()
        {
            InitializeComponent();

            // Create grabber. 
            _grabber = new FrameGrabber<LiveCameraResult>();

            // Set up a listener for when the client receives a new frame.
            _grabber.NewFrameProvided += (s, e) =>
            {

                // The callback may occur on a different thread, so we must use the
                // MainWindow.Dispatcher when manipulating the UI. 
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Display the image in the left pane.
                    LeftImage.Source = e.Frame.Image.ToBitmapSource();

                    // If we're fusing client-side face detection with remote analysis, show the
                    // new frame now with the most recent analysis available. 
                    if (_fuseClientRemoteResults)
                    {
                        RightImage.Source = VisualizeResult(e.Frame);
                    }
                }));

                // See if auto-stop should be triggered. 
                if (Properties.Settings.Default.AutoStopEnabled && (DateTime.Now - _startTime) > Properties.Settings.Default.AutoStopTime)
                {
                    _grabber.StopProcessingAsync();
                }
            };

            // Create local face detector. 
            _localFaceDetector.Load("Data/haarcascade_frontalface_alt2.xml");

        }

        /// <summary> Function which submits a frame to the Emotion API. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the emotions returned by the API. </returns>
        private async Task<LiveCameraResult> EmotionAnalysisFunction(VideoFrame frame)
        {
            // Encode image. 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);

            // Get ordered list of emotions and their values
            GetEmotionsList(frame);

            // Count the API call. 
            Properties.Settings.Default.EmotionAPICallCount++;

            // Output. 
            return new LiveCameraResult { };
        }

        private BitmapSource VisualizeResult(VideoFrame frame)
        {
            // Draw any results on top of the image. 
            BitmapSource visImage = frame.Image.ToBitmapSource();

            var result = _latestResultsToDisplay;

            if (result != null) { }

            return visImage;
        }

        /// <summary> Populate CameraList in the UI, once it is loaded. </summary>
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Routed event information. </param>
        private void CameraList_Loaded(object sender, RoutedEventArgs e)
        {
            int numCameras = _grabber.GetNumCameras();

            if (numCameras == 0)
            {
                //MessageArea.Text = "No cameras found!";
            }

            var comboBox = sender as ComboBox;
            comboBox.ItemsSource = Enumerable.Range(0, numCameras).Select(i => string.Format("Camera {0}", i + 1));
            comboBox.SelectedIndex = 0;
        }

        /// <summary> Populate ModeList in the UI, once it is loaded. </summary>
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Routed event information. </param>
        private void ModeList_Loaded(object sender, RoutedEventArgs e)
        {
            var modes = (AppMode[])Enum.GetValues(typeof(AppMode));

            var comboBox = sender as ComboBox;
            comboBox.ItemsSource = modes.Select(m => m.ToString());
            comboBox.SelectedIndex = 0;
        }

        private void ModeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Disable "most-recent" results display. 
            _fuseClientRemoteResults = false;

            var comboBox = sender as ComboBox;
            var modes = (AppMode[])Enum.GetValues(typeof(AppMode));
            _mode = modes[comboBox.SelectedIndex];
            switch (_mode)
            {
                case AppMode.Emotions:
                    _grabber.AnalysisFunction = EmotionAnalysisFunction;
                    break;
                default:
                    _grabber.AnalysisFunction = null;
                    break;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CameraList.HasItems)
            {
                return;
            }

            // Create agent.
            softwareAgent = new Agent();

            int populationSize = 1000;
            float crossoverValue = Properties.Settings.Default.GACrossover / 100f;
            float mutationValue = Properties.Settings.Default.GAMutation / 100f;
            int generationsTotal = 2;
            int mutationStep = Properties.Settings.Default.GAMutationStep;

            // Software Agent initialization.
            softwareAgent.Init(populationSize, crossoverValue, mutationValue, generationsTotal, mutationStep);

            // Clean leading/trailing spaces in API keys. 
            Properties.Settings.Default.EmotionAPIKey = Properties.Settings.Default.EmotionAPIKey.Trim();

            // Create API clients. 
            _emotionClient = new EmotionServiceClient(Properties.Settings.Default.EmotionAPIKey);

            // How often to analyze. 
            _grabber.TriggerAnalysisOnInterval(Properties.Settings.Default.AnalysisInterval);

            // Record start time, for auto-stop
            _startTime = DateTime.Now;

            // Initialize counters
            Properties.Settings.Default.EmotionAPICallCount = 0;
            Properties.Settings.Default.GAEpochCallCount = 0;

            // Start agent recording
            StartRecording();

            await _grabber.StartProcessingCameraAsync(CameraList.SelectedIndex);
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            

            StopRecording();

            softwareAgent.DeInit();
            softwareAgent = null;

            await _grabber.StopProcessingAsync();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = 1 - SettingsPanel.Visibility;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Hidden;
            Properties.Settings.Default.Save();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void SetAgentVolume(object sender, ElapsedEventArgs e) { }
        
        //Recording stops
        void StopRecording()
        {
            //MessageBox.Show("StopRecording");
            waveIn.StopRecording();
        }

        //Finishing recording
        private void waveIn_RecordingStopped(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(new EventHandler(waveIn_RecordingStopped), sender, e);
            }
            else
            {
                //waveIn.Dispose();
                waveIn = null;
            }
        }
        //Begim recording
        private void StartRecording()
        {
            try
            {
                ///////////MessageBox.Show("Start Recording");
                waveIn = new WaveIn();
                //Дефолтное устройство для записи (если оно имеется)
                waveIn.DeviceNumber = 0;
                //Прикрепляем к событию DataAvailable обработчик, возникающий при наличии записываемых данных
                waveIn.DataAvailable += waveIn_DataAvailable;
                //Прикрепляем обработчик завершения записи
                waveIn.RecordingStopped += waveIn_RecordingStopped;
                //Формат wav-файла - принимает параметры - частоту дискретизации и количество каналов(здесь mono)
                waveIn.WaveFormat = new WaveFormat(44100, 1);
                //Начало записи
                waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //Прерываем запись
        private void RecordStop()
        {
            if (waveIn != null)
            {
                StopRecording();
            }
        }

        //Получение данных из входного буфера 
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new EventHandler<WaveInEventArgs>(waveIn_DataAvailable), sender, e);
            }
            else
            {
                if (softwareAgent != null)
                {
                    try
                    {
                        softwareAgent.RunEpoch(e.Buffer);
                        // Count the GA Epochs call. 
                        Properties.Settings.Default.GAEpochCallCount++;

                        AudioWave.Source = Visualization.DrawAudio(e.Buffer, (int)AudioWave.Width, (int)AudioWave.Height);
                        AudioSpectrum.Source = Visualization.DrawSpectrum(softwareAgent.targetData, (int)AudioSpectrum.Width, (int)AudioSpectrum.Height);
                    }
                    catch
                    {

                    }
                    
                }
            }
        }

        async void GetEmotionsList(VideoFrame frame)
        {
            string percents = "";

            var client = new HttpClient();

            // Request headers - replace this example key with your valid key.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Properties.Settings.Default.EmotionAPIKey);

            // NOTE: You must use the same region in your REST call as you used to obtain your subscription keys.
            //   For example, if you obtained your subscription keys from westcentralus, replace "westus" in the 
            //   URI below with "westcentralus".
            string uri = "https://westus.api.cognitive.microsoft.com/emotion/v1.0/recognize?";
            HttpResponseMessage response;
            string responseContent;

            byte[] byteData = frame.Image.ToBytes();

            using (var content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);
                responseContent = response.Content.ReadAsStringAsync().Result;
            }

            //A peak at the JSON response.
            List<string> emotion_name = new List<string>()
            { "anger", "contempt", "disgust", "fear", "happiness", "neutral", "sadness", "surprise" };

            List<EmotionList> emotions = new List<EmotionList>() { };

            JsonTextReader reader = new JsonTextReader(new StringReader(responseContent));
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    var jt = reader.TokenType;
                    var jv = reader.Value;

                    switch (jt)
                    {
                        case JsonToken.PropertyName:

                            foreach (string en in emotion_name)
                            {
                                if (en == jv.ToString())
                                {
                                    reader.Read();
                                    EmotionList e = new EmotionList { name = en, value = float.Parse(reader.Value.ToString(), System.Globalization.NumberStyles.Float) };
                                    emotions.Add(e);
                                    break;
                                }
                            }
                            break;
                    }
                }
            }

            var sortedEmotions = from em in emotions
                              orderby em.value descending
                              select em;

            foreach (EmotionList el in sortedEmotions)
            {
                percents += el.name + ": " + el.value + Environment.NewLine; ;
            }

            List<EmotionList> x = new List<EmotionList>() { };
            x = sortedEmotions.ToList();
            if (softwareAgent != null)
            {
                if (x.Count != 0 && x.First<EmotionList>().name != "neutral")
                {
                    softwareAgent.ChangeSoundPattern(x);
                }
            }

            await this.Dispatcher.BeginInvoke((Action)(() =>
            {
                // Count the API call. 
                Properties.Settings.Default.EmotionAPICallCount++;
                RightImage.Source = VisualizeResult(frame);
                txtEmotionsList.Text = percents;
            }));
        }

        void ChangeSoundPatternFilePath(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            OpenFileDialog dlg = new OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name
            if (result == true)
            {
                // Change path
                Properties.Settings.Default.SoundPatternFile = dlg.FileName;
                if(softwareAgent != null)
                {
                    softwareAgent.SoundPatternFilePath = dlg.FileName;
                }
            }

        }

        void ChangeSoundElementsFolderPath(object sender, RoutedEventArgs e)
        {
            // Create FolderBrowserDialog 
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();

            // Create Folder DialogResult
            System.Windows.Forms.DialogResult dlgResult = new System.Windows.Forms.DialogResult();

            // Display FolderBrowserDialog by calling ShowDialog method 
            dlgResult = dlg.ShowDialog();

            // Get the selected folder path
            if (dlgResult == System.Windows.Forms.DialogResult.OK)
            {
                // Change path
                Properties.Settings.Default.SoundElementsFolder = dlg.SelectedPath;
                if (softwareAgent != null)
                {
                    softwareAgent.SoundElementsFolderPath = dlg.SelectedPath;
                }
            }

        }
        }
    }
