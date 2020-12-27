using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OpenCvSharp;
using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Linq;

namespace PSNRCalc {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    
    static class Constants {
        public const int CAP_PROP_FRAME_COUNT = 7;
        public const int CAP_PROP_POS_FRAMES = 1;
    }

    public class MeasureData {
        public string Timestamp { get; set; }
        public double PSNR { get; set; }
        public override string ToString() => $"Timestamp:{Timestamp}, PSNR:{PSNR}";
    }

    public partial class MainWindow : MetroWindow {
        public ObservableCollection<string> Logs = new ObservableCollection<string>();

        public string jsonPath = "data.json";

        public MainWindow() {
            InitializeComponent();
            var assembly = Assembly.GetExecutingAssembly();
            (LogOutput.Items as INotifyCollectionChanged).CollectionChanged += this.LogOutput_CollectionChanged;
            LogOutput.ItemsSource = Logs;
            Logs.Add(assembly.GetName().Name + " v" + assembly.GetName().Version.ToString());
        }

        private async void Start_Click(object sender, RoutedEventArgs e) {
            if(string.IsNullOrWhiteSpace(this.OrigPath.Text) || string.IsNullOrWhiteSpace(this.CompPath.Text)) {
                PrintLog("ファイルのパス指定が正しくありません。");
                return;
            }
            await CalcBase();
        }

        private void OpenVideo(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog() {
                Filter = "XAVC / ProRes|*.mxf;*.mov|All Files|*.*"
            };
            bool? result = dlg.ShowDialog();
            if(result == true) {
                if (((Button)sender).Name == ButtonOpenOrig.Name) {
                    OrigPath.Text = dlg.FileName;
                } else {
                    CompPath.Text = dlg.FileName;
                }
            }
        }

        private void PrintLog(string log) {
            Dispatcher.Invoke(() => {
                Logs.Add("[" + DateTime.Now + "] " + log);
            });
        }

        private async Task<int> CalcBase() {
            var originalVideo = new VideoCapture(this.OrigPath.Text);
            var compressedVideo = new VideoCapture(this.CompPath.Text);

            var targetFrames = Convert.ToInt32(CalcFrame.Text);
            var originalStartFrame = Convert.ToInt32(OrigStartFrame.Text);
            var compressedStartFrame = Convert.ToInt32(CompStartFrame.Text);

            double PSNR = 0;

            string jsonString;
            List<MeasureData> psnrDatas = new List<MeasureData>();

            UIToggle(false);

            if (!originalVideo.IsOpened()) {
                PrintLog("元映像のファイルが開けませんでした。");
                UIToggle(true);
                return 1;
            }
            if (!compressedVideo.IsOpened()) {
                PrintLog("圧縮映像のファイルが開けませんでした。");
                UIToggle(true);
                return 1;
            }

            var t = await Task.Run(() => {

                var currentOriginalFrame = new Mat();
                var currentCompressedFrame = new Mat();

                double currentPSNR;

                if (File.Exists(jsonPath)) {
                    jsonString = File.ReadAllText(jsonPath);
                   try {
                       PrintLog(jsonPath + "が存在します。読み込みを行います...");
                       psnrDatas = JsonSerializer.Deserialize<List<MeasureData>>(jsonString);
                    } catch (JsonException) {
                        PrintLog("<JsonException> 既存のJsonデータが壊れています。ファイルを消去して今回の計算結果を上書きします。");
                    }
                }

                var originalFrameCount = originalVideo.Get(Constants.CAP_PROP_FRAME_COUNT);
                var compressedFrameCount = compressedVideo.Get(Constants.CAP_PROP_FRAME_COUNT);

                PrintLog("総フレーム数　元映像：" + originalFrameCount.ToString() + "　圧縮映像：" + compressedFrameCount.ToString());
                PrintLog("計算開始フレーム　元映像：" + originalStartFrame.ToString() + "　圧縮映像：" + compressedStartFrame.ToString());
                PrintLog("計算対象：" + targetFrames.ToString() + "フレーム");

                originalVideo.Set(Constants.CAP_PROP_POS_FRAMES, originalStartFrame);
                compressedVideo.Set(Constants.CAP_PROP_POS_FRAMES, compressedStartFrame);

            
                for (int i = 1; i <= targetFrames; i++) {
                    originalVideo.Read(currentOriginalFrame);
                    compressedVideo.Read(currentCompressedFrame);
                    currentPSNR = Cv2.PSNR(currentOriginalFrame, currentCompressedFrame);
                    PrintLog("Current Frame = " + i + ", PSNR = " + currentPSNR);
                    PSNR += currentPSNR;
                }
                return 0;
            });

            PSNR /= targetFrames;
            PrintLog("計算終了 平均PSNR = " + PSNR);

            psnrDatas.Add(new MeasureData() { Timestamp = DateTime.Now.ToString(), PSNR = PSNR });
            psnrDatas = psnrDatas
                .OrderBy(data => data.Timestamp)
                .ToList();

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(psnrDatas, new JsonSerializerOptions { WriteIndented = true }));
            PrintLog(jsonPath + "に保存しました");

            originalVideo.Release();
            compressedVideo.Release();

            UIToggle(true);
            return t;
        }

        private void LogOutput_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    this.LogOutput.ScrollIntoView(this.LogOutput.Items[e.NewStartingIndex]);
                    break;
            }
        }

        private void UIToggle(bool isEnabled) {
            bool isReadOnly = !isEnabled;

            ButtonProgressAssist.SetIsIndicatorVisible(ButtonStart, isReadOnly);
            ButtonProgressAssist.SetIsIndeterminate(ButtonStart, isReadOnly);

            OrigPath.IsReadOnly = isReadOnly;
            CompPath.IsReadOnly = isReadOnly;
            CalcFrame.IsReadOnly = isReadOnly;
            OrigStartFrame.IsReadOnly = isReadOnly;
            CompStartFrame.IsReadOnly = isReadOnly;

            ButtonStart.IsEnabled = isEnabled;
            ButtonOpenOrig.IsEnabled = isEnabled;
            ButtonOpenComp.IsEnabled = isEnabled;
        }
    }
}
