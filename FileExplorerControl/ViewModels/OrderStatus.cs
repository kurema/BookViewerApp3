using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace kurema.FileExplorerControl.ViewModels;

public partial class FileItemViewModel
{
    public class OrderStatus : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion


        private string _Key;
        public string Key { get => _Key; set => SetProperty(ref _Key, value); }


        private bool _KeyIsAscending;
        public bool KeyIsAscending { get => _KeyIsAscending; set => SetProperty(ref _KeyIsAscending, value); }


        private Func<IEnumerable<FileItemViewModel>, IEnumerable<FileItemViewModel>> _OrderDelegate;
        public Func<IEnumerable<FileItemViewModel>, IEnumerable<FileItemViewModel>> OrderDelegate
        {
            get => _OrderDelegate; set
            {
                SetProperty(ref _OrderDelegate, value);
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public bool IsEmpty => _OrderDelegate == null;

        public OrderStatus GetBasicOrder(string key, bool isAscending)
        {
            var resultOrder = new OrderStatus();

            void SetSortState<T>(Func<FileItemViewModel, T> func, bool isAscendingArg)
            {
                if (isAscendingArg) resultOrder.OrderDelegate = a => a.OrderBy(func);
                else resultOrder.OrderDelegate = a => a.OrderByDescending(func);
            }

            switch (key)
            {
                case "Title":
                    SetSortState(b => b.Title, isAscending);
                    break;
                case "Size":
                    SetSortState(b => b.Size ?? 0, isAscending);
                    break;
                case "Date":
                    SetSortState(b => b.LastModified.Ticks, isAscending);
                    break;
                default:
                    return new OrderStatus();
            }
            resultOrder.Key = key;
            resultOrder.KeyIsAscending = isAscending;
            return resultOrder;
        }

        public OrderStatus GetShiftedBasicOrder(string key)
        {
            if (Key == key)
            {
                if (KeyIsAscending)
                {
                    return GetBasicOrder(key, false);
                }
                else
                {
                    return new OrderStatus();
                }
            }
            else
            {
                return GetBasicOrder(key, true);
            }
        }
    }
}
