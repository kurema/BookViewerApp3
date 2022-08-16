using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using static BookViewerApp.Storages.SettingStorage;

namespace BookViewerApp.Managers;
public static class ThemeManager
{
    public static bool IsDarkTheme
    {
        get
        {
            return CurrentElementTheme switch
            {
                ElementTheme.Dark => true,
                ElementTheme.Light => false,
                _ => Application.Current.RequestedTheme == ApplicationTheme.Dark,
            };
        }
    }

    public static ElementTheme CurrentElementTheme
    {
        get
        {
            if (GetValue(SettingKeys.Theme) is not SettingEnums.Theme theme) return ElementTheme.Default;
            switch (theme)
            {
                case SettingEnums.Theme.Light:
                case SettingEnums.Theme.AcrylicLight:
                    return ElementTheme.Light;
                case SettingEnums.Theme.Dark:
                case SettingEnums.Theme.AcrylicDark:
                    return ElementTheme.Dark;
                default:
                case SettingEnums.Theme.AcrylicAuto:
                case SettingEnums.Theme.Auto:
                    return ElementTheme.Default;
            }
        }
    }

    public static bool IsMica
    {
        get
        {
            var theme = (SettingEnums.Theme)GetValue(SettingKeys.Theme);
            return theme switch
            {
                SettingEnums.Theme.Auto or SettingEnums.Theme.Light or SettingEnums.Theme.Dark => true,
                SettingEnums.Theme.AcrylicAuto or SettingEnums.Theme.AcrylicLight or SettingEnums.Theme.AcrylicDark => false,
                _ => false,
            } && Environment.OSVersion.Version.Build >= 22000;
            //I think it's better to use ApiInformation. But what's the correct argument?
            //ApiInformation.IsApiContractPresent("");
        }
    }
}
