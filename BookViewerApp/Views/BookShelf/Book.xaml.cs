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

using ef = Microsoft.Graphics.Canvas.Effects;


// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace BookViewerApp.Views.BookShelf
{
    public sealed partial class Book : UserControl
    {
        public Book()
        {
            this.InitializeComponent();

            //https://stackoverflow.com/questions/41101198/how-to-combine-multiple-effects-in-uwp-composition-api
        }
    }
}
