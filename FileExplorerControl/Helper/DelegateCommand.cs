using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kurema.FileExplorerControl.Helper;

public class DelegateCommand : System.Windows.Input.ICommand
{
    public event EventHandler CanExecuteChanged;

    public Func<object, bool> CanExecuteDelegate;
    public Action<object> ExecuteDelegate;

    public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());

    public DelegateCommand(Action<object> executeDelegate, Func<object, bool> canExecuteDelegate = null)
    {
        ExecuteDelegate = executeDelegate ?? throw new ArgumentNullException(nameof(executeDelegate));
        CanExecuteDelegate = canExecuteDelegate;
    }

    public bool CanExecute(object parameter)
    {
        return CanExecuteDelegate?.Invoke(parameter) ?? true;
    }

    public void Execute(object parameter)
    {
        ExecuteDelegate?.Invoke(parameter);
    }
}

public class DelegateAsyncCommand : System.Windows.Input.ICommand
{
    public event EventHandler CanExecuteChanged;

    public Func<object, bool> CanExecuteDelegate;
    public Func<object, Task> ExecuteDelegate;

    public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());

    public DelegateAsyncCommand(Func<object, Task> executeDelegate, Func<object, bool> canExecuteDelegate = null)
    {
        ExecuteDelegate = executeDelegate ?? throw new ArgumentNullException(nameof(executeDelegate));
        CanExecuteDelegate = canExecuteDelegate;
    }

    public bool CanExecute(object parameter)
    {
        return CanExecuteDelegate?.Invoke(parameter) ?? true;
    }

    public async void Execute(object parameter)
    {
        await ExecuteDelegate?.Invoke(parameter);
    }

    public async Task ExecuteAsync(object parameter)
    {
        await ExecuteDelegate?.Invoke(parameter);
    }

}
