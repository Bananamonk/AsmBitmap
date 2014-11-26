using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Interop;
using System.Diagnostics;

namespace JA_Image_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsRunning { get; set; }
        bool DisplayTotalTime { get; set; }
        Stopwatch sw = new Stopwatch();
        Bitmap BitmapToPlayWith;
        String pathToImage;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ImagePanel_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                pathToImage = files[0];
                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                ImageSlot.Source = new BitmapImage(new Uri(@files[0]));
                convertedImageSlot.Source = ImageSlot.Source;
                BitmapToPlayWith = new Bitmap(files[0]);
                StackPanel1.IsEnabled = true;
                StackPanel2.IsEnabled = true;
            }
        }

        private void PieprzISólButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SepiaButton_Click(object sender, RoutedEventArgs e)
        {
                sw.Start();
                BitmapToPlayWith = BitmapToPlayWith.CopyAsSepiaTone(); 
                sw.Stop();
                this.ShowResults();
        }
        private void ShowResults()
        {
            TextBoxTime.Text = sw.Elapsed.ToString();
            sw.Reset();
            convertedImageSlot.Source = loadBitmap(BitmapToPlayWith);
            BitmapToPlayWith = new Bitmap(pathToImage);
        }
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }
        private void RozmycieButton_Click(object sender, RoutedEventArgs e)
        {
                sw.Start();

                BitmapToPlayWith = BitmapToPlayWith.Blur();

                sw.Stop();
                this.ShowResults();

        }

        private void Wyostrzenie_Click(object sender, RoutedEventArgs e)
        {
                sw.Start();
                BitmapToPlayWith = BitmapToPlayWith.Sharpening();
                sw.Stop();
                this.ShowResults();
        }
    }
}