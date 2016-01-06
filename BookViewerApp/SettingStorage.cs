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


        public interface ISettingInstance
        {

        }

        public class SettingInstance<type>
        {
            public string StringResourceKey;

            public string Key { get; private set; }
            public bool IsLocal { get; private set; }
            public type DefaultValue { get; private set; }

            public TypeConverter<type> Converter { get; private set; }
            private type Cache;

            private Windows.Storage.ApplicationDataContainer Setting { get { return (IsLocal ? LocalSettings : RoamingSettings); } }

            public Type GetGenericType()
            {
                return typeof(type);
            }

            public SettingInstance(string Key,string StringResourceKey,type DefaultValue,TypeConverter<type> Converter,bool IsLocal = true)
            {
                this.Key = Key;
                this.StringResourceKey = StringResourceKey;
                this.DefaultValue = DefaultValue;
                this.IsLocal = IsLocal;
                this.Converter = Converter;
            }

            public void SetValue(type Value)
            {
                Cache = Value;
                Setting.CreateContainer(Key, Windows.Storage.ApplicationDataCreateDisposition.Always);
                Setting.Values[Key] = Converter.GetString(Value);
            }

            public void SetValueAsString(string Value)
            {
                type result;
                if (Converter.TryGetType(Value, out result))
                    SetValue(result);
            }

            public string GetValueAsString(string Value)
            {
                return Converter.GetString(GetValue());
            }

            public type GetValue()
            {
                object data;
                if( Setting.Values.TryGetValue(Key, out data) == false)
                {
                    Cache = DefaultValue;
                    return DefaultValue;
                }
                else
                {
                    type result;
                    if(Converter.TryGetType(data.ToString(),out result))
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

        public interface TypeConverter<type>
        {
            String GetString(type value);
            bool TryGetType(string value,out type result);
        }

        public class TypeConverters
        {
            public class StringConverter : TypeConverter<string>
            {
                public string GetString(string value)
                {
                    return value;
                }

                public bool TryGetType(string value, out string result)
                {
                    result = value;
                    return true;
                }
            }

            public class IntConverter : TypeConverter<int>
            {
                public string GetString(int value)
                {
                    return value.ToString();
                }

                public bool TryGetType(string value, out int result)
                {
                    return int.TryParse(value, out result);
                }
            }

            public class DoubleConverter : TypeConverter<double>
            {
                public string GetString(double value)
                {
                    return value.ToString();
                }

                public bool TryGetType(string value, out double result)
                {
                    return double.TryParse(value, out result);
                }
            }

            public class SerializableConverter<type> : TypeConverter<type>
            {
                public string GetString(type value)
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(value.GetType());
                    using (System.IO.TextWriter tw = new System.IO.StringWriter())
                    {
                        xs.Serialize(tw, value);
                        return tw.ToString();
                    }
                }

                public type AsType(string value)
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(value.GetType());
                    using (System.IO.TextReader tr = new System.IO.StringReader(value))
                    {
                        return (type)xs.Deserialize(tr);
                    }
                }

                public bool TryGetType(string value, out type result)
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
