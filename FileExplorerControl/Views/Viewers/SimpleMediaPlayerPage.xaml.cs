using System;
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

using System.Threading.Tasks;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace kurema.FileExplorerControl.Views.Viewers
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SimpleMediaPlayerPage : Page
    {
        public SimpleMediaPlayerPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            mediaPlayerMain.MediaPlayer.Dispose();

            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter is Windows.Media.Playback.IMediaPlaybackSource source)
            {
                mediaPlayerMain.Source = source;
                return;
            }

            base.OnNavigatedTo(e);
        }

        public async static Task<string[]> GetAvailableExtensionsAsync()
        {
            return new[] { ".MP4", ".MP3", ".FLAC" };
        }
    }
}
