﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Ocr;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage.FileProperties;
using Windows.Graphics.Imaging;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using SimpleOcr10.Models;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.CompilerServices;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SimpleOcr10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private ObservableCollection<OcrResultDisplay> _resultsList = new ObservableCollection<OcrResultDisplay>();
        public ObservableCollection<OcrResultDisplay> ResultsList
        {
            get { return _resultsList; }
            set
            {
                if(value != _resultsList)
                {
                    _resultsList = value;
                    OnPropertyChanged(ResultsList);
                }
            }
        }

        private string _widthString = "W: 0";
        public string WidthString
        {
            get { return _widthString; }
            set
            {
                if(_widthString == value)
                {
                    return;
                }
                _widthString = value;
                OnPropertyChanged(WidthString);
            }
        }

        private string _heightString = "H: 0";
        public string HeightString
        {
            get { return _heightString; }
            set
            {
                if (_heightString == value)
                {
                    return;
                }
                _heightString = value;
                OnPropertyChanged(HeightString);
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            ResultsList.Clear();
            StatusBlock.Text = "Running...";
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.CommitButtonText = "Open";
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpe");
            openPicker.FileTypeFilter.Add(".bmp");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".gif");
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            
            foreach (StorageFile file in files)
            {
                try
                {
                    var result = await ProcessImage(file);
                    ResultsList.Add(result);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            
            StatusBlock.Text = "Ready";

        }

        private async Task<OcrResultDisplay> ProcessImage(StorageFile file)
        {            
            SoftwareBitmap bitmap;
            ImageSource source;
            using (var imgStream = await file.OpenAsync(FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(imgStream);                
                bitmap = await decoder.GetSoftwareBitmapAsync();                                
            }

            if(bitmap == null)
            {
                source = new BitmapImage();
                return new OcrResultDisplay { OcrString = "No text found.\n", OcrImage = source};
            }

            OcrEngine engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en"));
            OcrResult result = await engine.RecognizeAsync(bitmap);
            StringBuilder sb = new StringBuilder();
            if (result.Lines == null)
            {
                source = new SoftwareBitmapSource();
                bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                await ((SoftwareBitmapSource)source).SetBitmapAsync(bitmap);
                return new OcrResultDisplay { OcrString = "No text found.\n", OcrImage = source };
            }
            foreach (var line in result.Lines)
            {
                foreach (var word in line.Words)
                {
                    sb.Append(word.Text + " ");
                }
                sb.AppendLine();
            }

            source = new SoftwareBitmapSource();
            bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            await ((SoftwareBitmapSource)source).SetBitmapAsync(bitmap);
            return new OcrResultDisplay { OcrString = sb.ToString(), OcrImage = source };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(object property, [CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void FlyoutRemove_Click(object sender, RoutedEventArgs e)
        {
            if(OcrResultsList.SelectedItems?.Count == ResultsList.Count)
            {
                ResultsList.Clear();
            }
            if (OcrResultsList.SelectedItems?.Count > 1)
            {
                List<int> indicesToRemove = OcrResultsList.SelectedItems
                    .Select(x => ResultsList.IndexOf(x as OcrResultDisplay))
                    .OrderByDescending(x => x)
                    .ToList();

                foreach(int i in indicesToRemove)
                {
                    ResultsList.RemoveAt(i);
                }
            }
            else
            {
                MenuFlyoutItem item = (MenuFlyoutItem)e.OriginalSource;
                OcrResultDisplay ocrItem = (OcrResultDisplay)item.DataContext;
                ResultsList.Remove(ocrItem);
            }
        }

        private void OcrListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }
            var flyout = FlyoutBase.GetAttachedFlyout(element) as MenuFlyout;
            flyout?.ShowAt(this, e.GetPosition(null));
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WidthString = $"W: {e.NewSize.Width}";
            HeightString = $"W: {e.NewSize.Height}";            
        }
    }
}
