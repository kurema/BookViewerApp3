using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

namespace kurema.BrowserControl.ViewModels
{
    public class BrowserControlViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName]string propertyName = "",
            System.Action onChanged = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private WebView _Content;
        public WebView Content
        {
            get => _Content; set
            {
                if (_Content != null)
                {
                    _Content.NavigationStarting -= Content_NavigationStarting;
                    _Content.NavigationCompleted -= Content_NavigationCompleted;
                    _Content.NavigationFailed -= Content_NavigationFailed;
                }
                SetProperty(ref _Content, value);
                GoBackForwardCommand.Content = value;
                if (_Content != null)
                {
                    _Content.NavigationStarting += Content_NavigationStarting;
                    _Content.NavigationCompleted += Content_NavigationCompleted;
                    _Content.NavigationFailed += Content_NavigationFailed;
                }
            }
        }

        private void Content_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            OnPropertyChanged(nameof(Uri));
        }

        private void Content_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            OnPropertyChanged(nameof(Uri));
        }

        private void Content_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            OnPropertyChanged(nameof(Uri));
        }

        public string Uri
        {
            get => Content?.Source?.ToString();
            set
            {
                try
                {
                    if (!value.Contains("://"))
                    {
                        if (value.Contains("."))
                        {
                            Content?.Navigate(new Uri("http://" + value));
                        }
                        else
                        {
                            Content?.Navigate(new Uri("https://www.google.co.jp/search?q=" + value));
                        }
                    }
                    else
                    {
                        Content?.Navigate(new Uri(value));
                    }
                }
                catch { }
            }
        }


        private GoBackForwardCommandClass _GoBackForwardCommand;
        public GoBackForwardCommandClass GoBackForwardCommand { get => _GoBackForwardCommand = _GoBackForwardCommand ?? new GoBackForwardCommandClass(); }

        //I wonder what name is good for case like this...
        public class GoBackForwardCommandClass : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;

            public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());

            private WebView _Content;

            public WebView Content
            {
                get => _Content; set
                {
                    if (_Content != null) _Content.ContentLoading -= UpdateContent;
                    _Content = value;
                    if (_Content != null) _Content.ContentLoading += UpdateContent;
                }
            }

            private void UpdateContent(WebView sender, WebViewContentLoadingEventArgs args)
            {
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }

            private bool GetDirection(object s) => s?.ToString().ToLower() == "forward";

            public bool CanExecute(object parameter)
            {
                if (GetDirection(parameter)) return _Content?.CanGoForward ?? false;
                else return _Content?.CanGoBack ?? false;
            }

            public void Execute(object parameter)
            {
                if (GetDirection(parameter)) _Content?.GoForward();
                else _Content?.GoBack();
            }
        }

        public class ReloadCommandClass : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;

            private WebView _Content;

            public WebView Content
            {
                get => _Content; set
                {
                    if (_Content != null) _Content.ContentLoading -= UpdateContent;
                    _Content = value;
                    if (_Content != null) _Content.ContentLoading += UpdateContent;
                }
            }

            private void UpdateContent(WebView sender, WebViewContentLoadingEventArgs args)
            {
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }


            public bool CanExecute(object parameter)
            {
                throw new NotImplementedException();
            }

            public void Execute(object parameter)
            {
                throw new NotImplementedException();
            }

            public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());

        }
    }
}
