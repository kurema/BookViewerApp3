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

namespace BookViewerApp
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
                        new SettingInstance("DefaultFullScreen","DefaultFullScreen",false,new TypeConverters.BoolConverter()),
                        new SettingInstance("DefaultPageReverse","DefaultPageReverse",false,new TypeConverters.BoolConverter()),
                        new SettingInstance("ShowRightmostAndLeftmost","ShowRightmostAndLeftmost",false,new TypeConverters.BoolConverter()),
                        new SettingInstance("SyncBookmarks","SyncBookmarks",true,new TypeConverters.BoolConverter()),
                        new SettingInstance("SaveLastReadPage","SaveLastReadPage",true,new TypeConverters.BoolConverter()),
                        new SettingInstance("TileWidth","TileWidth",300.0,new TypeConverters.DoubleConverter()) {Minimum=0,Maximum=1000 },
                        new SettingInstance("TileHeight","TileHeight",300.0,new TypeConverters.DoubleConverter()){Minimum=0,Maximum=1000 },
                        new SettingInstance("BackgroundBrightness","BackgroundBrightness",90.0,new TypeConverters.DoubleConverter()){Minimum=0,Maximum=100 },
                        new SettingInstance("ScrollAnimation","ScrollAnimation",true,new TypeConverters.BoolConverter()),
                        new SettingInstance("FolderNameToExclude","FolderNameToExclude",null,new TypeConverters.RegexConverter()),
                        new SettingInstance("BookNameTrim","BookNameTrim",null,new TypeConverters.RegexConverter()),
                        new SettingInstance("SortNaturalOrder","SortNaturalOrder",false,new TypeConverters.BoolConverter()),
                        new SettingInstance("SortCoverComesFirst","SortCoverComesFirst",false,new TypeConverters.BoolConverter()),
                    };
            }
        }

        public static object GetValue(string Key)
        {
            foreach(var item in SettingInstances)
            {
                if (item.Key == Key)
                {
                    return item.GetValue();
                }
            }
            return null;
        }

        public class SettingInstance
        {
            public string StringResourceKey;

            public string Key { get; private set; }
            public bool IsLocal { get; private set; }
            public object DefaultValue { get; private set; }
            private Func<object, bool> IsValidObject { get; set; }

            public ITypeConverter Converter { get; private set; }
            private object Cache;

            public object Minimum{ get; set; }
            public object Maximum { get; set; }

            private Windows.Storage.ApplicationDataContainer Setting => (IsLocal ? LocalSettings : RoamingSettings);

            public Type GetGenericType()
            {
                return Converter.GetConvertType();
            }

            public SettingInstance(string Key,string StringResourceKey,object DefaultValue,ITypeConverter Converter,bool IsLocal = true, Func<object, bool> CheckValid=null)
            {
                this.Key = Key;
                this.StringResourceKey = "Setting_"+StringResourceKey;
                this.DefaultValue = DefaultValue;
                this.IsLocal = IsLocal;
                this.Converter = Converter;
                this.IsValidObject = (a) => { object result; return Converter.TryGetTypeGeneral(a.ToString(), out result); };
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
                if( Setting.Values.TryGetValue(Key, out data) == false)
                {
                    Cache = DefaultValue;
                    return DefaultValue;
                }
                else
                {
                    object result;
                    if(Converter.TryGetTypeGeneral(data.ToString(),out result))
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
                    return value.ToString();
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
                    try{
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
                        try {
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
