## PSNR Calculator

![GitHub](https://img.shields.io/github/license/chronoclover/PSNRCalc)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/chronoclover/PSNRCalc)

### これはなに
卒業研究で行うPSNR(Peak Signal to Noise Ratio)の計算をやってもらうアプリケーションです。  
先行研究者が作成したC++のコードを参考に、C#で書き直しています。  

### 何ができるの
 - ファイル単位でのPSNRの計算
 - フォルダ内のファイルに対してのPSNR一括計算
 - JSONファイルとして結果の出力 
 
### 使い方
 - **無保証なうえ、見ればわかると思うので解説はしません。**

### 注意事項
 - あくまで卒業研究用に作成しているため、相当偏った仕様となっています。  
 - 製作者はC#なんもわからんので、バグっても修正できないかもしれません。
 - 本アプリケーションを使用して何らかの被害が出ても、責任は負えません。 

### 動作環境
 - Windows 10 (x64のみ)  
Windows 10 Pro 20H2のみでテストしています。Windows 7でも動くかもしれませんが、Windows 10以降を推奨します。  

 - [.NET 5.0](https://dotnet.microsoft.com/download)  
ランタイムがデフォルトでインストールされないため、インストールが必要です。  

### 開発環境・言語
Windows 10 Pro 20H2・Visual Studio 2019で開発しています。C# + WPF製です。

### ライセンス
 - [MIT License](./LICENSE.txt)

### 使用ライブラリ
以下のライブラリを使用しています。  

 - [OpenCvSharp4](https://github.com/shimat/opencvsharp)
 - [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
 - [MahApps.Metro](https://mahapps.com)
 - [Extended.Wpf.Toolkit](https://github.com/xceedsoftware/wpftoolkit)
