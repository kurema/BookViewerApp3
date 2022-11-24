using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Helper;

public class DelegateCommand : kurema.FileExplorerControl.Helper.DelegateCommand
{
    public DelegateCommand(Action<object> executeDelegate, Func<object, bool> canExecuteDelegate = null) : base(executeDelegate, canExecuteDelegate)
    {
    }
}
