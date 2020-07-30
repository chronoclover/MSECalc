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
using OpenCvSharp;

namespace PSNRCalc {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    
    static class Constants {
        public const int CAP_PROP_BITRATE = 47;
    }
    public partial class MainWindow : System.Windows.Window {
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
            Mat OriginalColorImage;
            Mat OriginalGrayImage;
            Mat CompressedColorImage;
            Mat CompressedGrayImage;
            */

        }
    }
}
