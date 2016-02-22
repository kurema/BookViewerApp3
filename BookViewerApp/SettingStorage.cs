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
                return _SettingInstances=_SettingInstances ??
                    new SettingInstance[]
                    {
                        new SettingInstance("DefaultFullScreen","DefaultFullScreen",false,new TypeConverters.BoolConverter()),
                        new SettingInstance("DefaultPageReverse","DefaultPageReverse",false,new TypeConverters.BoolConverter()),
                        new SettingInstance("ShowRightmostAndLeftmost","ShowRightmostAndLeftmost",true,new TypeConverters.BoolConverter()),
                        new SettingInstance("SyncBookmarks","SyncBookmarks",true,new TypeConverters.BoolConverter()),
                        new SettingInstance("SaveLastReadPage","SaveLastReadPage",true,new TypeConverters.BoolConverter()),
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

            public TypeConverter Converter { get; private set; }
            private object Cache;

            private Windows.Storage.ApplicationDataContainer Setting { get { return (IsLocal ? LocalSettings : RoamingSettings); } }

            public Type GetGenericType()
            {
                return Converter.GetConvertType();
            }

            public SettingInstance(string Key,string StringResourceKey,object DefaultValue,TypeConverter Converter,bool IsLocal = true, Func<object, bool> CheckValid=null)
            {
                this.Key = Key;
                this.StringResourceKey = "Setting_"+StringResourceKey;
                this.DefaultValue = DefaultValue;
                this.IsLocal = IsLocal;
                this.Converter = Converter;
                this.IsValidObject = (a) => { return true; };
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
                if (obj.GetType() == GetGenericType())//?
                {
                    return IsValidObject(obj);
                }
                return false;
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

        public interface TypeConverter
        {
            String GetStringGeneral(object value);
            bool TryGetTypeGeneral(string value, out object result);

            Type GetConvertType();
        }


        public class TypeConverters
        {
            public class StringConverter : TypeConverter
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

            public class BoolConverter : TypeConverter
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

            public class IntConverter : TypeConverter
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

            public class DoubleConverter : TypeConverter
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

            public class SerializableConverter<type> : TypeConverter
            {
                public Type GetConvertType()
                {
                    return typeof(type);
                }

                public string GetStringGeneral(object value)
                {
                    if (!(value is type)) return null;
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
                            result = default(type);
                            return false;
                        }
                        if (xs.CanDeserialize(xr))
                        {
                            result = (type)xs.Deserialize(xr);
                            return true;
                        }
                        else
                        {
                            result = default(type);
                            return false;
                        }
                    }
                }
            }
        }
    }
}
