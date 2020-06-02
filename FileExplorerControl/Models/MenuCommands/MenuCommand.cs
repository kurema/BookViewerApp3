using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace kurema.FileExplorerControl.Models
{
    public class MenuCommand
    {
        public MenuCommand(string title, Action<object> action, Func<object, bool> canExecute = null)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Command = new Helper.DelegateCommand(action ?? throw new ArgumentNullException(nameof(action)), canExecute);
        }

        public MenuCommand(string title, ICommand command)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public MenuCommand(string title, params MenuCommand[] items)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Items = new ObservableCollection<MenuCommand>(items);
        }

        public string Title { get; set; }
        public ICommand Command { get; set; } = new Helper.DelegateCommand(a => { }, a => false);
        public ObservableCollection<MenuCommand> Items { get; } = new ObservableCollection<MenuCommand>();

        public bool HasChild => Items != null && Items.Count > 0;

        public static IEnumerable<MenuFlyoutItemBase> GetMenuFlyoutItems(MenuCommand[] menus) => menus.Select(a => a.GetMenuFlyoutItem());

        public MenuFlyoutItemBase GetMenuFlyoutItem()
        {
            var menu = this;
            if (menu.HasChild)
            {
                var result = new MenuFlyoutSubItem()
                {
                    Text = menu.Title,
                };
                foreach (var item in menu.Items)
                {
                    result.Items.Add(item.GetMenuFlyoutItem());
                }
                return result;
            }
            else
            {
                return new MenuFlyoutItem()
                {
                    Text = menu.Title,
                    Command = menu.Command
                };
            }
        }

    }
}
