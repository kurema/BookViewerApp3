using BookViewerApp.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BookViewerApp.Helper
{
    public class OpenWebCommand : ICommand
    {
        private Views.TabPage TabPage;

        public OpenWebCommand(TabPage tabPage, string address)
        {
            TabPage = tabPage ?? throw new ArgumentNullException(nameof(tabPage));
            Address = address ?? throw new ArgumentNullException(nameof(address));
        }

        public string Address { get; private set; }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return TabPage != null && Uri.TryCreate(Address, UriKind.Absolute, out _);
        }

        public void Execute(object parameter)
        {
            TabPage?.OpenTabWeb(Address);
        }
    }
}
