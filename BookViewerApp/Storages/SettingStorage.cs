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

namespace BookViewerApp.Storages
{
    public class SettingStorage
    {
        public static Windows.Storage.ApplicationDataContainer LocalSettings { get { return Windows.Storage.ApplicationData.Current.LocalSettings; } }
        public static Windows.Storage.ApplicationDataContainer RoamingSettings { get { return Windows.Storage.ApplicationData.Current.RoamingSettings; } }

        private static SettingInstance[] _SettingInstances = null;
        public static SettingInstance[] SettingInstances
        {
            get
            {
                return _SettingInstances = _SettingInstances ??
                    new SettingInstance[]
                    {
                        new SettingInstance("DefaultFullScreen",false,new TypeConverters.BoolConverter(),group:"Viewer"),
                        new SettingInstance("DefaultPageReverse",false,new TypeConverters.BoolConverter(),group:"Viewer"),
                        new SettingInstance("ShowRightmostAndLeftmost",false,new TypeConverters.BoolConverter(),group:"Viewer"),
                        new SettingInstance("SyncBookmarks",true,new TypeConverters.BoolConverter(),group:"Cloud"),
                        new SettingInstance("SaveLastReadPage",true,new TypeConverters.BoolConverter(),group:"Viewer"),
                        new SettingInstance("TileWidth",300.0,new TypeConverters.DoubleConverter(),group:"Obsolete",isVisible:false) {Minimum=0,Maximum=1000 },
                        new SettingInstance("TileHeight",300.0,new TypeConverters.DoubleConverter(),group:"Obsolete",isVisible:false){Minimum=0,Maximum=1000 },
                        new SettingInstance("BackgroundBrightness",90.0,new TypeConverters.DoubleConverter(),group:"Viewer"){Minimum=0,Maximum=100 },
                        new SettingInstance("ScrollAnimation",true,new TypeConverters.BoolConverter(),group:"Viewer"),
                        new SettingInstance("FolderNameToExclude",null,new TypeConverters.RegexConverter(),group:"Library",isVisible:false),
                        new SettingInstance("BookNameTrim",null,new TypeConverters.RegexConverter(),group:"Library",isVisible:false),
                        new SettingInstance("SortNaturalOrder",true,new TypeConverters.BoolConverter(),group:"Viewer"),//PerfectViewerが対応したのでデフォルトをtrueにしておきます
                        new SettingInstance("SortCoverComesFirst",false,new TypeConverters.BoolConverter(),group:"Viewer"),
                        new SettingInstance("ZoomButtonShowTimespan",4.0,new TypeConverters.DoubleConverter(),group:"Obsolete",isVisible:false){Minimum = 0,Maximum = 10},
                        new SettingInstance("CommandBarShowTimespan",0.0,new TypeConverters.DoubleConverter(),group:"Obsolete",isVisible:false){Minimum = 0,Maximum = 10},
                        new SettingInstance("WebHomePage","https://www.google.com/",new TypeConverters.StringConverter(),group:"Browser"),
                        new SettingInstance("WebSearchEngine","http://www.google.com/search?q=%s",new TypeConverters.StringConverter(),group:"Browser"),
                        new SettingInstance("RespectPageDirectionInfo",true,new TypeConverters.BoolConverter(),group:"Viewer"),
                        new SettingInstance("ShowPresetBookmarks",true,new TypeConverters.BoolConverter(),group:"Explorer"),
                        new SettingInstance("ExplorerContentStyle",kurema.FileExplorerControl.ViewModels.ContentViewModel.ContentStyles.Icon,new TypeConverters.EnumConverter<kurema.FileExplorerControl.ViewModels.ContentViewModel.ContentStyles>(),group:"Explorer",isVisible:false),
                        new SettingInstance("ExplorerIconSize",75.0,new TypeConverters.DoubleConverter(),group:"Explorer",isVisible:false),
                        new SettingInstance("ShowHistories",true,new TypeConverters.BoolConverter(),group:"Explorer"),
                        new SettingInstance("MaximumHistoryCount",100,new TypeConverters.IntConverter(),group:"Explorer",isVisible:false){Minimum = 0,Maximum = 500},//MRUで履歴を管理するようにしたので非表示にしました。
                        new SettingInstance("EpubViewerType",SettingEnums.EpubViewerType.Bibi, new TypeConverters.EnumConverter<SettingEnums.EpubViewerType>(),group:"Viewer"),
                        new SettingInstance("DefaultSpreadType",Views.SpreadPagePanel.ModeEnum.Default, new TypeConverters.EnumConverter<Views.SpreadPagePanel.ModeEnum>(),group:"Viewer"),
                        new SettingInstance("PdfRenderScaling",true,new TypeConverters.BoolConverter(),group:"Viewer"),
                    };
                //How to add resource when you add SettingInstance:
                //1. Open Resource/en-US/Resources.resw
                //2. Add entries nemad:
                //   * Setting_*.Title
                //   * Setting_*.Description
                //3. Build (with Multilingual App Toolkit)
                //4. Edit files in MultilingualResources/
                //Notebook:
                //Setting_.Title
                //Setting_.Description

                //Sample:
                //if ((bool)Storages.SettingStorage.GetValue("")){}
            }
        }

        public static object GetValue(string Key)
        {
            foreach (var item in SettingInstances)
            {
                if (item.Key == Key)
                {
                    return item.GetValue();
                }
            }
            return null;
        }

        public static void SetValue(string Key, object value)
        {
            SettingInstances?.FirstOrDefault(a => a.Key == Key)?.SetValue(value);
        }

        public static class SettingEnums
        {
            public enum EpubViewerType
            {
                Bibi, EpubJsReader
            }
        }

        public class SettingInstance
        {
            //SettingEntry sounds better. But I keep it this way for now.
            public string StringResourceKey;

            public string Key { get; private set; }
            public bool IsLocal { get; private set; }
            public object DefaultValue { get; private set; }
            private Func<object, bool> IsValidObject { get; set; }

            public ITypeConverter Converter { get; private set; }
            private object Cache;

            public object Minimum { get; set; }
            public object Maximum { get; set; }

            public string GroupName { get; set; }

            public bool IsVisible { get; set; }

            private Windows.Storage.ApplicationDataContainer Setting => (IsLocal ? LocalSettings : RoamingSettings);

            public Type GetGenericType()
            {
                return Converter.GetConvertType();
            }

            public SettingInstance(string Key, object DefaultValue, ITypeConverter Converter, bool IsLocal = true, Func<object, bool> CheckValid = null, string group = "", bool isVisible = true)
            {
                this.Key = Key;
                this.StringResourceKey = "Setting_" + Key;
                this.DefaultValue = DefaultValue;
                this.IsLocal = IsLocal;
                this.Converter = Converter;
                this.IsValidObject = CheckValid ?? new Func<object, bool>((a) => { object result; return Converter.TryGetTypeGeneral(a.ToString(), out result); });
                this.GroupName = group;
                this.IsVisible = isVisible;

            }

            public void SetValue(object Value)
            {
                if (!IsValid(Value)) return;

                Cache = Value;
                Setting.CreateContainer(Key, Windows.Storage.ApplicationDataCreateDisposition.Always);
                Setting.Values[Key] = Converter.GetStringGeneral(Value);
            }

            public void SetValueAsString(string Value)
            {
                object result;
                if (Converter.TryGetTypeGeneral(Value, out result))
                    SetValue(result);
            }

            public string GetValueAsString()
            {
                return Converter.GetStringGeneral(GetValue());
            }

            public bool IsValid(object obj)
            {
                //if (obj.GetType() == GetGenericType())//?
                {
                    return IsValidObject(obj);
                }
                //return false;
            }

            public object GetValue()
            {
                object data;
                if (Setting.Values.TryGetValue(Key, out data) == false)
                {
                    Cache = DefaultValue;
                    return DefaultValue;
                }
                else
                {
                    object result;
                    if (Converter.TryGetTypeGeneral(data.ToString(), out result))
                    {
                        Cache = result;
                        return result;
                    }
                    else
                    {
                        Cache = DefaultValue;
                        return DefaultValue;
                    }
                }
            }
        }

        public interface ITypeConverter
        {
            String GetStringGeneral(object value);
            bool TryGetTypeGeneral(string value, out object result);

            Type GetConvertType();
        }


        public class TypeConverters
        {
            public class StringConverter : ITypeConverter
            {
                public Type GetConvertType()
                {
                    return typeof(string);
                }

                public string GetStringGeneral(object value)
                {
                    return value?.ToString() ?? "";
                }

                public bool TryGetTypeGeneral(string value, out object result)
                {
                    result = value;
                    return true;
                }
            }

            public class BoolConverter : ITypeConverter
            {
                public Type GetConvertType()
                {
                    return typeof(bool);
                }

                public string GetStringGeneral(object value)
                {
                    return value.ToString();
                }

                public bool TryGetTypeGeneral(string value, out object result)
                {
                    bool data;
                    if (bool.TryParse(value, out data))
                    {
                        result = data;
                        return true;
                    }
                    result = null;
                    return false;
                }
            }

            public class IntConverter : ITypeConverter
            {
                public Type GetConvertType()
                {
                    return typeof(int);
                }

                public string GetStringGeneral(object value)
                {
                    return value.ToString();
                }

                public bool TryGetTypeGeneral(string value, out object result)
                {
                    int data;
                    if (int.TryParse(value, out data))
                    {
                        result = data;
                        return true;
                    }
                    result = null;
                    return false;
                }

            }

            public class EnumConverter<T> : ITypeConverter where T : Enum
            {
                public Type GetConvertType()
                {
                    return typeof(T);
                }

                public string GetStringGeneral(object value)
                {
                    return value.ToString();
                }

                public bool TryGetTypeGeneral(string value, out object result)
                {
                    var values = typeof(T).GetEnumValues();
                    foreach (var t in values)
                    {
                        var name = Enum.GetName(typeof(T), t);
                        if (name == value)
                        {
                            result = (T)t;
                            return true;
                        }
                    }
                    {
                        result = default(T);
                        return false;
                    }
                }
            }

            public class DoubleConverter : ITypeConverter
            {
                public Type GetConvertType()
                {
                    return typeof(double);
                }

                public string GetStringGeneral(object value)
                {
                    return value.ToString();
                }

                public bool TryGetTypeGeneral(string value, out object result)
                {
                    double data;
                    if (double.TryParse(value, out data))
                    {
                        result = data;
                        return true;
                    }
                    result = null;
                    return false;
                }
            }

            public class RegexConverter : ITypeConverter
            {
                public Type GetConvertType()
                {
                    return typeof(System.Text.RegularExpressions.Regex);
                }

                public string GetStringGeneral(object value)
                {
                    if (value is System.Text.RegularExpressions.Regex)
                    {
                        return (value as System.Text.RegularExpressions.Regex).ToString();
                    }
                    else return value?.ToString();
                }

                public bool TryGetTypeGeneral(string value, out object result)
                {
                    try
                    {
                        result = new System.Text.RegularExpressions.Regex(value);
                        return true;
                    }
                    catch
                    {
                        result = null;
                        return false;
                    }
                }
            }

            public class SerializableConverter<T> : ITypeConverter
            {
                public Type GetConvertType()
                {
                    return typeof(T);
                }

                public string GetStringGeneral(object value)
                {
                    if (!(value is T)) return null;
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(value.GetType());
                    using (System.IO.TextWriter tw = new System.IO.StringWriter())
                    {
                        xs.Serialize(tw, value);
                        return tw.ToString();
                    }
                }

                public bool TryGetTypeGeneral(string value, out object result)
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(value.GetType());
                    using (System.IO.TextReader tr = new System.IO.StringReader(value))
                    {
                        System.Xml.XmlReader xr;
                        try
                        {
                            xr = System.Xml.XmlReader.Create(tr);
                        }
                        catch
                        {
                            result = default(T);
                            return false;
                        }
                        if (xs.CanDeserialize(xr))
                        {
                            result = (T)xs.Deserialize(xr);
                            return true;
                        }
                        else
                        {
                            result = default(T);
                            return false;
                        }
                    }
                }
            }
        }
    }
}
