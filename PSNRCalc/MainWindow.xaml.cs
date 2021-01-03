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
        public ObservableCollection<string> AdvLogs = new ObservableCollection<string>();

        public string jsonPath = "data.json";

        public MainWindow() {
            InitializeComponent();
            var assembly = Assembly.GetExecutingAssembly();
            (LogOutput.Items as INotifyCollectionChanged).CollectionChanged += this.LogOutput_CollectionChanged;
            LogOutput.ItemsSource = Logs;
            //Logs.Add(assembly.GetName().Name + " v" + assembly.GetName().Version.ToString());
            (advLogOutput.Items as INotifyCollectionChanged).CollectionChanged += this.advLogOutput_CollectionChanged;
            advLogOutput.ItemsSource = AdvLogs;
        }

        private async void Start_Click(object sender, RoutedEventArgs e) {
            if(string.IsNullOrWhiteSpace(this.OrigPath.Text) || string.IsNullOrWhiteSpace(this.CompPath.Text)) {
                PrintLog("ファイルのパス指定が正しくありません。");
                return;
            }
            await Calc();
        }

        private async void AdvStartClick(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(this.advOrigPath.Text) || string.IsNullOrWhiteSpace(this.advFolderPath.Text)) {
                AdvPrintLog("ファイルのパス指定が正しくありません。");
                return;
            }

            try {
                string[] files = Directory.GetFiles(advFolderPath.Text, "*.mov", SearchOption.TopDirectoryOnly);
                foreach (string file in files) {
                    await AdvCalc(file);
                }
            } catch (Exception ex) {
                AdvLogs.Add(ex.ToString());
                UIToggle(true);
                return;
            }

            AdvUIToggle(true);
        }

        private void OpenVideo(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog() {
                Filter = "XAVC / ProRes|*.mxf;*.mov|All Files|*.*",
                Title = "ファイルを選択してください。"
            };
            bool? result = dlg.ShowDialog();
            if(result == true) {
                if (((Button)sender).Name == ButtonOpenOrig.Name)　{
                    OrigPath.Text = dlg.FileName;
                } else if(((Button)sender).Name == ButtonOpenComp.Name) {
                    CompPath.Text = dlg.FileName;
                } else {
                    advOrigPath.Text = dlg.FileName;
                }
            }
        }

        private void OpenFolder(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog() {
                Filter = "Folder|.",
                FileName = "SelectFolder",
                CheckFileExists = false,
                Title = "フォルダを選択してください。"
            };
            advFolderPath.Text = ((bool)dlg.ShowDialog())? Path.GetDirectoryName(dlg.FileName) : "";
        }

        private void PrintLog(string log) {
            Dispatcher.Invoke(() => {
                Logs.Add("[" + DateTime.Now + "] " + log);
            });
        }

        private void AdvPrintLog(string log)
        {
            Dispatcher.Invoke(() => {
                AdvLogs.Add("[" + DateTime.Now + "] " + log);
            });
        }

        private async Task<int> Calc() {
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

            psnrDatas.Add(new MeasureData() { Timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), PSNR = PSNR });
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

        private async Task<int> AdvCalc(string compPath) {
            var originalVideo = new VideoCapture(this.advOrigPath.Text);
            var compressedVideo = new VideoCapture(compPath);

            if (!originalVideo.IsOpened()) {
                AdvPrintLog("元映像のファイルが開けませんでした。");
                AdvUIToggle(true);
                return 1;
            }
            if (!compressedVideo.IsOpened()) {
                AdvPrintLog("圧縮映像のファイルが開けませんでした。");
                AdvUIToggle(true);
                return 1;
            }

            var targetFrames = Convert.ToInt32(advCalcFrame.Text);
            var originalStartFrame = Convert.ToInt32(advOrigStartFrame.Text);
            var compressedStartFrame = 1;

            var penult = new Mat();
            var ultima = new Mat();

            AdvUIToggle(false);

            AdvPrintLog("圧縮映像 " + compPath + " の開始フレーム検出を行います・・・");

            var t = await Task.Run(() => {
                for (int i = 1; i < Int32.MaxValue; i++) {
                    compressedVideo.Set(Constants.CAP_PROP_POS_FRAMES, i);
                    compressedVideo.Read(penult);
                    compressedVideo.Read(ultima);

                    if (Cv2.PSNR(penult, ultima) < 10d) {
                        compressedStartFrame = i + 2; // XXX: なんで1じゃなくて2を足すと正しいフレームになるのか
                        break;
                    }
                }
                return 0;
            });

            AdvPrintLog("開始フレームを検出しました -> #" + compressedStartFrame);

            double PSNR = 0;

            string jsonString;
            List<MeasureData> psnrDatas = new List<MeasureData>();

            t = await Task.Run(() => {

                var currentOriginalFrame = new Mat();
                var currentCompressedFrame = new Mat();

                double currentPSNR;

                if (File.Exists(jsonPath)) {
                    jsonString = File.ReadAllText(jsonPath);
                    try {
                        AdvPrintLog(jsonPath + "が存在します。読み込みを行います...");
                        psnrDatas = JsonSerializer.Deserialize<List<MeasureData>>(jsonString);
                    } catch (JsonException) {
                        AdvPrintLog("<JsonException> 既存のJsonデータが壊れています。ファイルを消去して今回の計算結果を上書きします。");
                    }
                }

                var originalFrameCount = originalVideo.Get(Constants.CAP_PROP_FRAME_COUNT);
                var compressedFrameCount = compressedVideo.Get(Constants.CAP_PROP_FRAME_COUNT);

                AdvPrintLog("総フレーム数　元映像：" + originalFrameCount.ToString() + "　圧縮映像：" + compressedFrameCount.ToString());
                AdvPrintLog("計算開始フレーム　元映像：" + originalStartFrame.ToString() + "　圧縮映像：" + compressedStartFrame.ToString());
                AdvPrintLog("計算対象：" + targetFrames.ToString() + "フレーム");

                originalVideo.Set(Constants.CAP_PROP_POS_FRAMES, originalStartFrame);
                compressedVideo.Set(Constants.CAP_PROP_POS_FRAMES, compressedStartFrame);


                for (int i = 1; i <= targetFrames; i++) {
                    originalVideo.Read(currentOriginalFrame);
                    compressedVideo.Read(currentCompressedFrame);
                    currentPSNR = Cv2.PSNR(currentOriginalFrame, currentCompressedFrame);
                    AdvPrintLog("Current Frame = " + i + ", PSNR = " + currentPSNR);
                    PSNR += currentPSNR;
                }
                return 0;
            });

            PSNR /= targetFrames;
            AdvPrintLog("計算終了 平均PSNR = " + PSNR);

            psnrDatas.Add(new MeasureData() { Timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), PSNR = PSNR });
            psnrDatas = psnrDatas
                .OrderBy(data => data.Timestamp)
                .ToList();

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(psnrDatas, new JsonSerializerOptions { WriteIndented = true }));
            AdvPrintLog(jsonPath + "に保存しました");

            originalVideo.Release();
            compressedVideo.Release();

            return t;
        }

        private void LogOutput_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    LogOutput.ScrollIntoView(LogOutput.Items[e.NewStartingIndex]);
                    break;
            }
        }

        private void advLogOutput_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    advLogOutput.ScrollIntoView(advLogOutput.Items[e.NewStartingIndex]);
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

        private void AdvUIToggle(bool isEnabled) {
            bool isReadOnly = !isEnabled;

            ButtonProgressAssist.SetIsIndicatorVisible(advButtonStart, isReadOnly);
            ButtonProgressAssist.SetIsIndeterminate(advButtonStart, isReadOnly);

            advOrigPath.IsReadOnly = isReadOnly;
            advFolderPath.IsReadOnly = isReadOnly;
            advCalcFrame.IsReadOnly = isReadOnly;
            advOrigStartFrame.IsReadOnly = isReadOnly;

            advButtonStart.IsEnabled = isEnabled;
            advButtonOpenOrig.IsEnabled = isEnabled;
            advButtonOpenFolder.IsEnabled = isEnabled;
        }
    }
}
