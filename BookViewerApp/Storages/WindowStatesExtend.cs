using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Storages.WindowStates;
public interface IWindowStateWindowTab { }
public partial class WindowStateWindowBookshelfTab : IWindowStateWindowTab { }
public partial class WindowStateWindowExplorerTab : IWindowStateWindowTab { }
public partial class WindowStateWindowSettingTab : IWindowStateWindowTab { }
public partial class WindowStateWindowViewerTab : IWindowStateWindowTab { }
public partial class WindowStateWindowTextEditorTab : IWindowStateWindowTab { }
public partial class WindowStateWindowBrowserTab : IWindowStateWindowTab { }
public partial class WindowStateWindowMediaPlayerTab : IWindowStateWindowTab { }