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
using Microsoft.Extensions.Primitives;
using BookViewerApp.Helper;

namespace BookViewerApp.Storages;

public class SettingStorage
{
	public static Windows.Storage.ApplicationDataContainer LocalSettings { get { return Windows.Storage.ApplicationData.Current.LocalSettings; } }
	public static Windows.Storage.ApplicationDataContainer RoamingSettings { get { return Windows.Storage.ApplicationData.Current.RoamingSettings; } }

	public static class SettingKeys
	{
		public const string DefaultSpreadType = "DefaultSpreadType";
		public const string DefaultFullScreen = "DefaultFullScreen";
		public const string SaveLastReadPage = "SaveLastReadPage";
		public const string DefaultPageReverse = "DefaultPageReverse";
		public const string RememberPageDirection = "RememberPageDirection";
		public const string RespectPageDirectionInfo = "RespectPageDirectionInfo";
		public const string ShowRightmostAndLeftmost = "ShowRightmostAndLeftmost";
		public const string SyncBookmarks = "SyncBookmarks";
		public const string TileWidth = "TileWidth";
		public const string TileHeight = "TileHeight";
		public const string BackgroundBrightness = "BackgroundBrightness";
		public const string ScrollAnimation = "ScrollAnimation";
		public const string FolderNameToExclude = "FolderNameToExclude";
		public const string BookNameTrim = "BookNameTrim";
		public const string SortNaturalOrder = "SortNaturalOrder";
		public const string SortCoverComesFirst = "SortCoverComesFirst";
		public const string ZoomButtonShowTimespan = "ZoomButtonShowTimespan";
		public const string CommandBarShowTimespan = "CommandBarShowTimespan";
		public const string WebHomePage = "WebHomePage";
		public const string WebSearchEngine = "WebSearchEngine";
		public const string ShowPresetBookmarks = "ShowPresetBookmarks";
		public const string ExplorerContentStyle = "ExplorerContentStyle";
		public const string ExplorerIconSize = "ExplorerIconSize";
		public const string ShowHistories = "ShowHistories";
		public const string MaximumHistoryCount = "MaximumHistoryCount";
		public const string EpubViewerType = "EpubViewerType";
		public const string PdfRenderScaling = "PdfRenderScaling";
		public const string ShowBookmarkFavicon = "ShowBookmarkFavicon";
		public const string FetchThumbnailsBackground = "FetchThumbnailsBackground";
		public const string DefaultBrowserExternal = "DefaultBrowserExternal";
		public const string EpubViewerDarkMode = "EpubViewerDarkMode";
		public const string ViewerSlideshowLastTimeSpan = "ViewerSlideshowLastTimeSpan";
		public const string BrowserUseWebView2 = "BrowserUseWebView2";
		public const string BrowserUserAgent = "BrowserUserAgent";
		public const string BrowserAdBlockEnabled = "BrowserAdBlockEnabled";
		public const string Theme = "Theme";
		public const string BrowserSearchComplitionService = "BrowserSearchComplitionService";
		public const string ScreenBrightnessOverride = "ScreenBrightnessOverride";

	}

	private static SettingInstance[] _SettingInstances = null;
	public static SettingInstance[] SettingInstances
	{
		get
		{
			return _SettingInstances ??= new SettingInstance[]
				{
						new SettingInstance(SettingKeys.DefaultSpreadType,Views.SpreadPagePanel.ModeEnum.Default, new TypeConverters.EnumConverter<Views.SpreadPagePanel.ModeEnum>(),group:"Viewer"),
						new SettingInstance(SettingKeys.Theme,SettingEnums.Theme.AcrylicAuto, new TypeConverters.EnumConverter<SettingEnums.Theme>(),group:"Viewer"),
						new SettingInstance(SettingKeys.DefaultFullScreen,false,new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.SaveLastReadPage,true,new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.DefaultPageReverse,false,new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.RememberPageDirection,true,new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.RespectPageDirectionInfo,true,new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.ShowRightmostAndLeftmost,false,new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.SyncBookmarks,true,new TypeConverters.BoolConverter(),group:"Cloud"),
						new SettingInstance(SettingKeys.TileWidth,300.0,new TypeConverters.DoubleConverter(),group:"Obsolete",isVisible:false) { Minimum=0, Maximum=1000 },
						new SettingInstance(SettingKeys.TileHeight,300.0,new TypeConverters.DoubleConverter(),group:"Obsolete",isVisible:false){ Minimum=0, Maximum=1000 },
						new SettingInstance(SettingKeys.BackgroundBrightness,90.0,new TypeConverters.DoubleConverter(),group:"Viewer"){ Minimum=0, Maximum=100 },
						new SettingInstance(SettingKeys.ScrollAnimation,true,new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.FolderNameToExclude,null,new TypeConverters.RegexConverter(),group:"Library",isVisible:false),
						new SettingInstance(SettingKeys.BookNameTrim,null,new TypeConverters.RegexConverter(),group:"Library",isVisible:false),
						new SettingInstance(SettingKeys.SortNaturalOrder,true,new TypeConverters.BoolConverter(),group:"Viewer"),//PerfectViewerが対応したのでデフォルトをtrueにしておきます
                        new SettingInstance(SettingKeys.SortCoverComesFirst,false,new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.ZoomButtonShowTimespan,4.0,new TypeConverters.DoubleConverter(),group:"Obsolete",isVisible:false){ Minimum = 0, Maximum = 10},
						new SettingInstance(SettingKeys.CommandBarShowTimespan,0.0,new TypeConverters.DoubleConverter(),group:"Obsolete",isVisible:false){ Minimum = 0, Maximum = 10},
						new SettingInstance(SettingKeys.WebHomePage,"https://www.google.com/",new TypeConverters.StringConverter(),group:"Browser"),
						new SettingInstance(SettingKeys.WebSearchEngine,"http://www.google.com/search?q=%s",new TypeConverters.StringConverter(),group:"Browser"),
						new SettingInstance(SettingKeys.ShowPresetBookmarks,true,new TypeConverters.BoolConverter(),group:"Explorer"),
						new SettingInstance(SettingKeys.ExplorerContentStyle,kurema.FileExplorerControl.ViewModels.ContentViewModel.ContentStyles.Icon,new TypeConverters.EnumConverter<kurema.FileExplorerControl.ViewModels.ContentViewModel.ContentStyles>(),group:"Explorer",isVisible:false),
						new SettingInstance(SettingKeys.ExplorerIconSize,75.0,new TypeConverters.DoubleConverter(),group:"Explorer",isVisible:false),
						new SettingInstance(SettingKeys.ShowHistories,true,new TypeConverters.BoolConverter(),group:"Explorer"),
						new SettingInstance(SettingKeys.MaximumHistoryCount,100,new TypeConverters.IntConverter(),group:"Explorer",isVisible:false){ Minimum = 0, Maximum = 500},//MRUで履歴を管理するようにしたので非表示にしました。
                        new SettingInstance(SettingKeys.EpubViewerType,SettingEnums.EpubViewerType.Bibi, new TypeConverters.EnumConverter<SettingEnums.EpubViewerType>(),group:"Viewer"),
						new SettingInstance(SettingKeys.PdfRenderScaling,true,new TypeConverters.BoolConverter(),group:"Viewer"),
                        //new SettingInstance(SettingKeys.ShowBookmarkFavicon,false,new TypeConverters.BoolConverter(),group:"Explorer"),
                        new SettingInstance(SettingKeys.FetchThumbnailsBackground,true,new TypeConverters.BoolConverter(),group:"Explorer"),
						new SettingInstance(SettingKeys.DefaultBrowserExternal,false,new TypeConverters.BoolConverter(),group:"Explorer"),
						new SettingInstance(SettingKeys.EpubViewerDarkMode,false, new TypeConverters.BoolConverter(),group:"Viewer"),
						new SettingInstance(SettingKeys.ViewerSlideshowLastTimeSpan,20.0,new TypeConverters.DoubleConverter(),group:"Viewer",isVisible:false),
						new SettingInstance(SettingKeys.BrowserUseWebView2,false,new TypeConverters.BoolConverter(),group:"Browser"),
						new SettingInstance(SettingKeys.BrowserUserAgent,string.Empty,new TypeConverters.StringConverter(),group:"Browser"),
						new SettingInstance(SettingKeys.BrowserAdBlockEnabled,false,new TypeConverters.BoolConverter(),group:"Browser",isVisible:false),
                        // We disable search completion as default to comply GDPR or something.
                        new SettingInstance(SettingKeys.BrowserSearchComplitionService,kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionOptions.Dummy,new TypeConverters.EnumConverter<kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionOptions>(),group:"Browser"),
						new SettingInstance(SettingKeys.ScreenBrightnessOverride,-1,new TypeConverters.DoubleConverter(),group:"Viewer",IsLocal:true,onChangedAction:_=>{ App.OverrideBrightness(); }){ Minimum = -1, Maximum = 100},

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
			//if (Storages.SettingStorage.GetValue(Storages.SettingStorage.SettingKeys.Theme) is not Storages.SettingStorage.SettingEnums.Theme) return;
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

		public enum Theme
		{
			//Auto, Light, Dark,
			AcrylicAuto, AcrylicLight, AcrylicDark,
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

		public object Minimum { get; set; }
		public object Maximum { get; set; }

		public string GroupName { get; set; }

		public bool IsVisible { get; set; }
		public event EventHandler ValueChanged;

		private Windows.Storage.ApplicationDataContainer Setting => IsLocal ? LocalSettings : RoamingSettings;
		private Windows.Storage.ApplicationDataContainer SettingAlternative => IsLocal ? RoamingSettings : LocalSettings;

		public Type GetGenericType()
		{
			return Converter.GetConvertType();
		}

		public SettingInstance(string Key, object DefaultValue, ITypeConverter Converter, bool IsLocal = true, Func<object, bool> CheckValid = null, string group = "", bool isVisible = true, Action<SettingInstance> onChangedAction = null)
		{
			this.Key = Key;
			this.StringResourceKey = "Setting_" + Key;
			this.DefaultValue = DefaultValue;
			this.IsLocal = IsLocal;
			this.Converter = Converter;
			this.IsValidObject = CheckValid ?? new Func<object, bool>((a) => { return Converter.TryGetTypeGeneral(a.ToString(), out object result); });
			this.GroupName = group;
			this.IsVisible = isVisible;
			if (onChangedAction is not null) this.ValueChanged += (s, _) => onChangedAction(s as SettingInstance);
		}

		public void SetValue(object Value)
		{
			if (!IsValid(Value)) return;

			Setting.CreateContainer(Key, Windows.Storage.ApplicationDataCreateDisposition.Always);
			Setting.Values[Key] = Converter.GetStringGeneral(Value);
			ValueChanged?.Invoke(this, new EventArgs());
		}

		public void SetValueAsString(string Value)
		{
			if (Converter.TryGetTypeGeneral(Value, out object result))
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
			if (!Setting.Values.TryGetValue(Key, out object data))
			{
				if (!SettingAlternative.Values.TryGetValue(Key, out data))
				{
					return DefaultValue;
				}
			}

			{
				if (Converter.TryGetTypeGeneral(data.ToString(), out object result))
				{
					return result;
				}
				else
				{
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
				if (bool.TryParse(value, out bool data))
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
				if (int.TryParse(value, out int data))
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
			private Array enumValuesCache = null;
			private Type typeCache = null;

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
				typeCache ??= typeof(T);
				var values = enumValuesCache ??= typeCache.GetEnumValues();
				foreach (var t in values)
				{
					var name = Enum.GetName(typeCache, t);
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
				if (double.TryParse(value, out double data))
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
				if (value is not T) return null;
				System.Xml.Serialization.XmlSerializer xs = new(value.GetType());
				using System.IO.TextWriter tw = new System.IO.StringWriter();
				xs.Serialize(tw, value);
				return tw.ToString();
			}

			public bool TryGetTypeGeneral(string value, out object result)
			{
				System.Xml.Serialization.XmlSerializer xs = new(value.GetType());
				using System.IO.TextReader tr = new System.IO.StringReader(value);
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
