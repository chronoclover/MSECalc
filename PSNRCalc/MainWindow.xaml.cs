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
    }

    public class MeasureData {
        public int Bitrate { get; set; }
        public double Psnr { get; set; }
        public override string ToString() => $"Bitrate:{Bitrate}, Psnr:{Psnr}";
    }

    public partial class MainWindow : MetroWindow {
        public ObservableCollection<string> Logs = new ObservableCollection<string>();
        public string jsonPath = "./data.json";

        public MainWindow() {
            InitializeComponent();
            var assembly = Assembly.GetExecutingAssembly();
            (LogOutput.Items as INotifyCollectionChanged).CollectionChanged += this.LogOutput_CollectionChanged;
            LogOutput.ItemsSource = Logs;
            Logs.Add(assembly.GetName().Name + " v" + assembly.GetName().Version.ToString());
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            if(string.IsNullOrWhiteSpace(this.OrigPath.Text) || string.IsNullOrWhiteSpace(this.CompPath.Text)) {
                MessageBox.Show("ファイルのパス指定が正しくありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            CalcPrep();
        }

        private void OpenOrigFile(object sender, RoutedEventArgs e) {
            OpenVideo(false);
        }

        private void OpenCompFile(object sender, RoutedEventArgs e) {
            OpenVideo(true);
        }

        private void OpenVideo(bool isComp) {
            var dlg = new OpenFileDialog() {
                Filter = "MPEG-4 Video|*.mp4|Matroska Video|*.mkv|MPEG-TS|*.ts;*.m2ts|Audio Video Interleave|*.avi|All Files|*.*",
                DefaultExt = ".mp4"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if(result == true) {
                if (isComp) {
                    CompPath.Text = dlg.FileName;
                } else {
                    OrigPath.Text = dlg.FileName;
                }
            }
        }

        private void CalcPrep() {
            var originalVideo = new VideoCapture(this.OrigPath.Text);
            var compressedVideo = new VideoCapture(this.CompPath.Text);

            int[] calculated = new int[100];

            Logs.Clear();

            if (File.Exists("./data.json")) {
                var jsonString = File.ReadAllText(jsonPath);
                Logs.Add("既存の計算データを読み込み：" + Path.GetFullPath(jsonPath));
            }

            if (!originalVideo.IsOpened()) {
                MessageBox.Show("元映像のファイルが開けませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if(!compressedVideo.IsOpened()) {
                MessageBox.Show("圧縮映像のファイルが開けませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double bitrate = compressedVideo.Get(Constants.CAP_PROP_BITRATE);
            MessageBox.Show(Convert.ToInt32(bitrate / 1000).ToString() + "Mbps", "ビットレート", MessageBoxButton.OK, MessageBoxImage.Information);

            var originalFrameCount = originalVideo.Get(Constants.CAP_PROP_FRAME_COUNT);
            var compressedFrameCount = compressedVideo.Get(Constants.CAP_PROP_FRAME_COUNT);

            Logs.Add("元映像の総フレーム数：" + originalFrameCount.ToString());
            Logs.Add("圧縮映像の総フレーム数：" + compressedFrameCount.ToString());
            
            /*
            string json = "[{\"Bitrate\":4,\"Psnr\":35}]";

            List<MeasureData> Datas = JsonSerializer.Deserialize<List<MeasureData>>(json);

            Datas.Add(new MeasureData() { Bitrate = 2, Psnr = 20 });
            Datas.Sort((a, b) => a.Bitrate - b.Bitrate);

            Console.WriteLine(JsonSerializer.Serialize(Datas, new JsonSerializerOptions { WriteIndented = true }));
            */

            /*
            Mat OriginalColorImage;
            Mat CompressedColorImage;
            */

        }
        /*
        private void LogOutput_TargetUpdated(object sender, DataTransferEventArgs e) {
            (LogOutput.ItemsSource as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(LogOutput_CollectionChanged);
        }
        */
        private void LogOutput_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    this.LogOutput.ScrollIntoView(this.LogOutput.Items[e.NewStartingIndex]);
                    break;
            }
        }
    }
}
