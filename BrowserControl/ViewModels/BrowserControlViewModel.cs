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
                    _Content.UnviewableContentIdentified -= _Content_UnviewableContentIdentified;
                }
                SetProperty(ref _Content, value);
                if (_Content != null)
                {
                    _Content.NavigationStarting += Content_NavigationStarting;
                    _Content.NavigationCompleted += Content_NavigationCompleted;
                    _Content.NavigationFailed += Content_NavigationFailed;
                    _Content.UnviewableContentIdentified += _Content_UnviewableContentIdentified;
                }
            }
        }

        private void _Content_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
        {
            OnPropertyChanged(nameof(this.Uri));
        }

        private ObservableCollection<DownloadItemViewModel> _Downloads = new ObservableCollection<DownloadItemViewModel>();
        public ObservableCollection<DownloadItemViewModel> Downloads { get => _Downloads; set => SetProperty(ref _Downloads, value); }


        private string _HomePage;
        public string HomePage { get => _HomePage; set => SetProperty(ref _HomePage, value); }


        private string _Title;
        public string Title { get => _Title; set => SetProperty(ref _Title, value); }



        private void Content_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            ReloadCommand.IsLoaded = true;
            ReloadCommand.OnCanExecuteChanged();
            Title = Content.DocumentTitle;
            OnPropertyChanged(nameof(Uri));
        }

        private void Content_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            ReloadCommand.IsLoaded = true;
            ReloadCommand.OnCanExecuteChanged();
            Title = Content.DocumentTitle;
            OnPropertyChanged(nameof(Uri));
        }

        private void Content_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            ReloadCommand.IsLoaded = false;
            ReloadCommand.OnCanExecuteChanged();
            Title = Content.DocumentTitle;
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
                        if (value.Contains(".") && !value.Contains(" ") && !value.Contains("　"))
                        {
                            Content?.Navigate(new Uri("https://" + value));
                        }
                        else
                        {
                            Content?.Navigate(new Uri("https://www.google.com/search?q=" + value));
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


        private Commands.GoBackForwardCommand _GoBackForwardCommand;
        public Commands.GoBackForwardCommand GoBackForwardCommand => _GoBackForwardCommand = _GoBackForwardCommand ?? new Commands.GoBackForwardCommand(this);

        private Commands.ReloadCommand _ReloadCommand;
        public Commands.ReloadCommand ReloadCommand => _ReloadCommand = _ReloadCommand ?? new Commands.ReloadCommand(this);

        private Commands.NavigateCommand _NavigateCommand;
        public Commands.NavigateCommand NavigateCommand => _NavigateCommand = _NavigateCommand ?? new Commands.NavigateCommand(this);

        public class Commands
        {
            public abstract class CommandBase : System.Windows.Input.ICommand
            {
                public event EventHandler CanExecuteChanged;

                public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());


                protected BrowserControlViewModel _Content;
                public BrowserControlViewModel Content
                {
                    get => _Content;
                    set
                    {
                        if (_Content != null) _Content.PropertyChanged -= Value_PropertyChanged;
                        _Content = value;
                        if (_Content != null) _Content.PropertyChanged += Value_PropertyChanged;
                    }
                }

                private void Value_PropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(BrowserControlViewModel.Content))
                    {
                        if (_ContentView != null) _ContentView.ContentLoading -= UpdateContent;
                        _ContentView = Content.Content;
                        if (_ContentView != null) _ContentView.ContentLoading += UpdateContent;

                    }
                }

                private WebView _ContentView;

                public WebView ContentView
                {
                    get => Content.Content; 
                }

                protected void UpdateContent(WebView sender, WebViewContentLoadingEventArgs args)
                {
                    CanExecuteChanged?.Invoke(this, new EventArgs());
                }

                public abstract bool CanExecute(object parameter);

                public abstract void Execute(object parameter);
            }

            public class GoBackForwardCommand : CommandBase
            {
                public GoBackForwardCommand()
                {
                }

                public GoBackForwardCommand(BrowserControlViewModel model)
                {
                    this.Content = model;
                }


                private bool GetDirection(object s) => s?.ToString().ToLower() == "forward";

                public override bool CanExecute(object parameter)
                {
                    if (GetDirection(parameter)) return ContentView?.CanGoForward ?? false;
                    else return ContentView?.CanGoBack ?? false;
                }

                public override void Execute(object parameter)
                {
                    if (GetDirection(parameter)) ContentView?.GoForward();
                    else ContentView?.GoBack();
                }
            }

            public class ReloadCommand : CommandBase
            {
                public ReloadCommand()
                {
                }

                public ReloadCommand(BrowserControlViewModel model)
                {
                    this.Content = model;
                }

                public bool IsLoaded { get; set; } = false;

                public override bool CanExecute(object parameter)
                {
                    return IsLoaded;
                }

                public override void Execute(object parameter)
                {
                    ContentView?.Refresh();
                }
            }

            public class NavigateCommand : CommandBase
            {
                public NavigateCommand()
                {
                }

                public NavigateCommand(BrowserControlViewModel model)
                {
                    this.Content = model;
                }


                public override bool CanExecute(object parameter)
                {
                    return Content?.Content != null && parameter != null;
                }

                public override void Execute(object parameter)
                {
                    if (Content != null) Content.Uri = parameter.ToString();
                }
            }
        }
    }
}
