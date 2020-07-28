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
    public partial class MainWindow : System.Windows.Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            var OriginalVideoPath = this.OriginalVideoPath.Text;
            var CompressedVideoPath = this.CompressedVideoPath.Text;

            Task task = Task.Run(() => {
                PSNRCalc(OriginalVideoPath, CompressedVideoPath);
            });
        }

        private void PSNRCalc(String originalVideoPath, String compressedVideoPath) {
            var OriginalVideo = new VideoCapture(originalVideoPath);
            var CompressedVideo = new VideoCapture(compressedVideoPath);

            if (!OriginalVideo.IsOpened()) {
                MessageBox.Show("元映像のファイルが開けませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if(!CompressedVideo.IsOpened()) {
                MessageBox.Show("圧縮映像のファイルが開けませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            /*
            Mat OriginalColorImage;
            Mat OriginalGrayImage;
            Mat CompressedColorImage;
            Mat CompressedGrayImage;
            */
            
        }
    }
}
