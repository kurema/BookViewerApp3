using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kurema.BrowserControl.Helper;
public static class Functions
{
    public static bool IsWebView2Supported
    {
        get
        {
            try
            {
                //https://github.com/MicrosoftEdge/WebView2Feedback/issues/2545
                var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (string.IsNullOrEmpty(version)) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
