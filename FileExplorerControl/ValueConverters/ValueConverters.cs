using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Data;


namespace kurema.FileExplorerControl.Helper.ValueConverters
{
    public class IntToStringValueConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var text = (parameter?.ToString() ?? "").Split(':');
            int cnt = 0;
            int.TryParse(value?.ToString() ?? "0", out cnt);
            return text[Math.Max(0, Math.Min(text.Length - 1, cnt))];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
