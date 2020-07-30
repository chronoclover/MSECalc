using System;
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
using System.Windows.Shapes;
using OpenCvSharp;
using MahApps.Metro.Controls;

namespace PSNRCalc {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    
    static class Constants {
        public const int CAP_PROP_BITRATE = 47;
    }

    public class MeasureData {
        public int Bitrate { get; set; }
        public double Psnr { get; set; }
        public override string ToString() => $"Bitrate:{Bitrate}, Psnr:{Psnr}";
    }

    public partial class MainWindow : MetroWindow {
        public MainWindow() {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if(string.IsNullOrWhiteSpace(this.OrigPath.Text) || string.IsNullOrWhiteSpace(this.CompPath.Text)) {
                MessageBox.Show("ファイルのパス指定が正しくありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CalcPrep();
        }

        private void CalcPrep() {
            var OriginalVideo = new VideoCapture(this.OrigPath.Text);
            var CompressedVideo = new VideoCapture(this.CompPath.Text);

            if (!OriginalVideo.IsOpened()) {
                MessageBox.Show("元映像のファイルが開けませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if(!CompressedVideo.IsOpened()) {
                MessageBox.Show("圧縮映像のファイルが開けませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var Bitrate = CompressedVideo.Get(Constants.CAP_PROP_BITRATE);
            MessageBox.Show(Convert.ToInt32(Bitrate / 1000).ToString() + "Mbps", "ビットレート", MessageBoxButton.OK, MessageBoxImage.Information);

            /*
            string json = "[{\"Bitrate\":4,\"Psnr\":35}]";

            List<MeasureData> Datas = JsonSerializer.Deserialize<List<MeasureData>>(json);

            Datas.Add(new MeasureData() { Bitrate = 2, Psnr = 20 });
            Datas.Sort((a, b) => a.Bitrate - b.Bitrate);

            Console.WriteLine(JsonSerializer.Serialize(Datas, new JsonSerializerOptions { WriteIndented = true }));
            */

            /*
            Mat OriginalColorImage;
            Mat OriginalGrayImage;
            Mat CompressedColorImage;
            Mat CompressedGrayImage;
            */

        }
    }
}
