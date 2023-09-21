using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using BookViewerApp.Storages;
using BookViewerApp.Views;
using System.Threading.Tasks;

namespace BookViewerApp;

/// <summary>
/// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
/// </summary>
sealed partial class App : Application
{
	/// <summary>
	/// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
	///最初の行であるため、main() または WinMain() と論理的に等価です。
	/// </summary>
	public App()
	{
		this.InitializeComponent();
		this.Suspending += OnSuspending;
		LoadStorages();

		//Easier than DI.
		kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionManager.DefaultSearchComplitionProvider = () =>
		{
			if (SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserSearchComplitionService) is not kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionOptions choice) return new kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionManager.SearchComplitionDummy();
			return choice switch
			{
				kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionOptions.Google => kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionManager.SearchComplitionGoogleSingle,
				kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionOptions.Yahoo => kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionManager.SearchComplitionYahooSingle,
				kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionOptions.Bing => kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionManager.SearchComplitionBingSingle,
				_ => kurema.BrowserControl.Helper.SearchComplitions.SearchComplitionManager.SearchComplitionDummySingle,
			};
		};
	}

	private async void LoadStorages()
	{
		await BookInfoStorage.LoadAsync();
		//await LicenseStorage.LoadAsync();
		await LibraryStorage.Content.GetContentAsync();
		//await HistoryStorage.Content.GetContentAsync();
		await PathStorage.Content.GetContentAsync();
	}

	static Windows.Graphics.Display.BrightnessOverride _BrightnessOverride;

	public static void OverrideBrightnessStop() { try { _BrightnessOverride?.StopOverride(); } catch { } }

	public static void OverrideBrightnessUpdate()
	{
		try
		{
			if (SettingStorage.GetValue(SettingStorage.SettingKeys.ScreenBrightnessOverride) is double bvalue && bvalue >= 0)
			{
				_BrightnessOverride ??= Windows.Graphics.Display.BrightnessOverride.GetForCurrentView();
				if (_BrightnessOverride is not null and { IsSupported: true })
				{
					_BrightnessOverride.SetBrightnessLevel(Math.Clamp(bvalue / 100, 0, 1), Windows.Graphics.Display.DisplayBrightnessOverrideOptions.UseDimmedPolicyWhenBatteryIsLow);
					_BrightnessOverride.StartOverride();
				}
			}
		}
		catch
		{
		}
	}

	public static void OverrideBrightness()
	{
		try
		{
			if (SettingStorage.GetValue(SettingStorage.SettingKeys.ScreenBrightnessOverride) is double bvalue && bvalue >= 0)
			{
				//Following code may have issue of blinking, because it stops and then start again.
				//We also consider there may be an environment with multiple monitors and each can adjustable brightness. Really?
				//Anyway, if there's no blinking issue this should be fine.
				_BrightnessOverride?.StopOverride();
				_BrightnessOverride = Windows.Graphics.Display.BrightnessOverride.GetForCurrentView();
				if (_BrightnessOverride is not null and { IsSupported: true })
				{
					_BrightnessOverride.SetBrightnessLevel(Math.Clamp(bvalue / 100, 0, 1), Windows.Graphics.Display.DisplayBrightnessOverrideOptions.UseDimmedPolicyWhenBatteryIsLow);
					_BrightnessOverride.StartOverride();
				}
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// アプリケーションがエンド ユーザーによって正常に起動されたときに呼び出されます。他のエントリ ポイントは、
	/// アプリケーションが特定のファイルを開くために起動されたときなどに使用されます。
	/// </summary>
	/// <param name="e">起動の要求とプロセスの詳細を表示します。</param>
	protected override async void OnLaunched(LaunchActivatedEventArgs e)
	{
#if DEBUG
		if (System.Diagnostics.Debugger.IsAttached)
		{
			this.DebugSettings.EnableFrameRateCounter = true;
		}
#endif

		OverrideBrightness();

		// ウィンドウに既にコンテンツが表示されている場合は、アプリケーションの初期化を繰り返さずに、
		// ウィンドウがアクティブであることだけを確認してください
		if (Window.Current.Content is not Frame rootFrame)
		{
			// ナビゲーション コンテキストとして動作するフレームを作成し、最初のページに移動します
			rootFrame = new Frame();

			rootFrame.NavigationFailed += OnNavigationFailed;

			if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
			{
				//TODO: 以前中断したアプリケーションから状態を読み込みます
			}

			// フレームを現在のウィンドウに配置します
			Window.Current.Content = rootFrame;
		}

		if (rootFrame.Content is null)
		{
			// ナビゲーション スタックが復元されない場合は、最初のページに移動します。
			// このとき、必要な情報をナビゲーション パラメーターとして渡して、新しいページを
			//構成します
			if ((bool)SettingStorage.GetValue(SettingStorage.SettingKeys.RestorePreviousSession))
			{
				try
				{
					var content = await WindowStatesStorage.Content.GetContentAsync();
					var last = content.Last;
					_ = Task.Run(async () =>
					{
						//Ensure that startup failures do not occur again.
						//content.Last = new();
						//await WindowStatesStorage.Content.SaveAsync();
					});
					rootFrame.Navigate(typeof(TabPage), last);
				}
				catch
				{
					rootFrame.Navigate(typeof(TabPage), e.Arguments);
				}
			}
			else
			{
				rootFrame.Navigate(typeof(TabPage), e.Arguments);
			}

		}
		// 現在のウィンドウがアクティブであることを確認します
		Window.Current.Activate();
	}

	/// <summary>
	/// 特定のページへの移動が失敗したときに呼び出されます
	/// </summary>
	/// <param name="sender">移動に失敗したフレーム</param>
	/// <param name="e">ナビゲーション エラーの詳細</param>
	void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
	{
		throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
	}

	/// <summary>
	/// アプリケーションの実行が中断されたときに呼び出されます。
	/// アプリケーションが終了されるか、メモリの内容がそのままで再開されるかに
	/// かかわらず、アプリケーションの状態が保存されます。
	/// </summary>
	/// <param name="sender">中断要求の送信元。</param>
	/// <param name="e">中断要求の詳細。</param>
	private async void OnSuspending(object sender, SuspendingEventArgs e)
	{
		var deferral = e.SuspendingOperation.GetDeferral();

		await BookInfoStorage.SaveAsync();
		await LibraryStorage.Content.SaveAsync();
		await LibraryStorage.RoamingBookmarks.SaveAsync();
		{
			var content = await Storages.WindowStatesStorage.Content.GetContentAsync();
			if (Window.Current.Content is TabPage tp1) content.Last = tp1.GetWindowStates();
			else if ((Window.Current.Content as Frame)?.Content is TabPage tp2) content.Last = tp2.GetWindowStates();
			await Storages.WindowStatesStorage.Content.SaveAsync();
		}

		OverrideBrightnessStop();

		//TODO: アプリケーションの状態を保存してバックグラウンドの動作があれば停止します
		deferral.Complete();
	}

	protected override void OnFileActivated(FileActivatedEventArgs args)
	{
		OverrideBrightness();

		if (Window.Current?.Content is Frame f)
		{
#pragma warning disable CS0612 // 型またはメンバーが旧型式です
			if (f.Content is BookFixed2Viewer v2)
			{
				v2.SaveInfo();
			}
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
			else if (f.Content is BookFixed3Viewer v3)
			{
				v3.CloseOperation();
			}
			else if (f.Content is TabPage tp)
			{
				tp.OpenTabBook(args.Files);
				return;
			}
		}

		var rootFrame = new Frame();
		rootFrame.Navigate(typeof(TabPage), args);

		if (Window.Current != null)
		{
			Window.Current.Content = rootFrame;
			Window.Current.Activate();
		}
		//base.OnFileActivated(args);
	}
}
