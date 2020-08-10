using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using OpenCvSharp;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Reflection;

namespace PSNRCalc {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    
    static class Constants {
        public const int CAP_PROP_BITRATE = 47;
        public const int CAP_PROP_FRAME_COUNT = 7;
        public const int CAP_PROP_POS_FRAMES = 1;
    }

    public class MeasureData {
        public int Bitrate { get; set; }
        public double PSNR { get; set; }
        public override string ToString() => $"Bitrate:{Bitrate}, Psnr:{PSNR}";
    }

    public partial class MainWindow : MetroWindow {
        public ObservableCollection<string> Logs = new ObservableCollection<string>();
        public string jsonPath = "data.json";

        public int originalStartFrame;
        public int compressedStartFrame;

        public MainWindow() {
            InitializeComponent();
            var assembly = Assembly.GetExecutingAssembly();
            (LogOutput.Items as INotifyCollectionChanged).CollectionChanged += this.LogOutput_CollectionChanged;
            LogOutput.ItemsSource = Logs;
            Logs.Add(assembly.GetName().Name + " v" + assembly.GetName().Version.ToString());
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            if(string.IsNullOrWhiteSpace(this.OrigPath.Text) || string.IsNullOrWhiteSpace(this.CompPath.Text)) {
                PrintLog("ファイルのパス指定が正しくありません。");
                return;
            }
            CalcBase();
        }

        private void OpenVideo(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog() {
                Filter = "MPEG-4 Video|*.mp4|Matroska Video|*.mkv|MPEG-TS|*.ts;*.m2ts|Audio Video Interleave|*.avi|All Files|*.*",
                DefaultExt = ".mp4"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if(result == true) {
                if (((Button)sender).Name == ButtonOpenOrig.Name) {
                    OrigPath.Text = dlg.FileName;
                } else {
                    CompPath.Text = dlg.FileName;
                }
            }
        }

        private void PrintLog(string log) {
            Logs.Add("[" + DateTime.Now + "] " + log);
        }

        private void CalcBase() {
            var originalVideo = new VideoCapture(this.OrigPath.Text);
            var compressedVideo = new VideoCapture(this.CompPath.Text);
            var targetFrames = Convert.ToInt32(CalcFrame.Text);

            var currentOriginalFrame = new Mat();
            var currentCompressedFrame = new Mat();

            string jsonString;

            double currentPSNR;
            double PSNR = 0;

            List<MeasureData> psnrDatas = new List<MeasureData>();

            originalStartFrame = Convert.ToInt32(OrigStartFrame.Text);
            compressedStartFrame = Convert.ToInt32(CompStartFrame.Text);

            UIToggle(false);

            if (!originalVideo.IsOpened()) {
                PrintLog("元映像のファイルが開けませんでした。");
                UIToggle(true);
                return;
            }
            if(!compressedVideo.IsOpened()) {
                PrintLog("圧縮映像のファイルが開けませんでした。");
                UIToggle(true);
                return;
            }

            if (File.Exists(jsonPath)) {
                jsonString = File.ReadAllText(jsonPath);
                try {
                    PrintLog(jsonPath + "が存在します。読み込みを行います...");
                    psnrDatas = JsonSerializer.Deserialize<List<MeasureData>>(jsonString);
                } catch (JsonException e) {
                    PrintLog("<JsonException> 既存のJsonデータが壊れています。ファイルを消去して今回の計算結果を上書きします。");
                }
            }

            int bitrate = Convert.ToInt32(compressedVideo.Get(Constants.CAP_PROP_BITRATE)) / 1000;
            PrintLog("ビットレート：" + bitrate.ToString() + "Mbps");

            var originalFrameCount = originalVideo.Get(Constants.CAP_PROP_FRAME_COUNT);
            var compressedFrameCount = compressedVideo.Get(Constants.CAP_PROP_FRAME_COUNT);

            PrintLog("総フレーム数　元映像：" + originalFrameCount.ToString() + "　圧縮映像：" + compressedFrameCount.ToString());
            PrintLog("計算開始フレーム　元映像：" + originalStartFrame.ToString() + "　圧縮映像：" + compressedStartFrame.ToString());
            PrintLog("計算対象：" + targetFrames.ToString() + "フレーム");

            originalVideo.Set(Constants.CAP_PROP_POS_FRAMES, originalStartFrame);
            compressedVideo.Set(Constants.CAP_PROP_POS_FRAMES, compressedStartFrame);

            for(int i = 1; i <= targetFrames; i++) {
                originalVideo.Read(currentOriginalFrame);
                compressedVideo.Read(currentCompressedFrame);
                currentPSNR = Cv2.PSNR(currentOriginalFrame, currentCompressedFrame);
                PrintLog("Current Frame = " + i + ", PSNR = " + currentPSNR);
                PSNR += currentPSNR;
            }

            PSNR /= targetFrames;
            PrintLog("計算終了 PSNR = " + PSNR);

            psnrDatas.Add(new MeasureData() { Bitrate = bitrate, PSNR = PSNR });
            psnrDatas.Sort((a, b) => a.Bitrate - b.Bitrate);

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(psnrDatas, new JsonSerializerOptions { WriteIndented = true }));
            PrintLog(jsonPath + "に保存しました");

            originalVideo.Release();
            compressedVideo.Release();

            UIToggle(true);
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
