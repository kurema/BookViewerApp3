```
System.Reflection.MissingMetadataException
  HResult=0x80131543
  Message=Windows.Foundation.IReference`1<Double>
  Source=<例外のソースを評価できません>
  スタック トレース:
<例外のスタック トレースを評価できません>
```

```
 	[マネージドからネイティブへの移行]	
 	System.Private.Interop.dll!System.Runtime.InteropServices.McgTypeHelpers.McgFakeMetadataType.UnderlyingSystemType.get() 行 137	C#
 	System.Private.Reflection.Core.dll!System.Reflection.Runtime.TypeInfos.RuntimeTypeInfo.IsAssignableFrom(System.Type c) 行 274	C#
 	System.Private.Reflection.Core.dll!System.Reflection.Runtime.TypeInfos.RuntimeTypeInfo.IsAssignableFrom(System.Reflection.TypeInfo typeInfo) 行 264	C#
 	Microsoft.UI.Xaml.Markup.dll!Microsoft.UI.Xaml.Markup.XamlReflectionType.XamlReflectionType(System.Type underlyingType)	不明
 	Microsoft.UI.Xaml.Markup.dll!Microsoft.UI.Xaml.Markup.XamlReflectionType.Create(System.Type typeID)	不明
 	Microsoft.UI.Xaml.Markup.dll!Microsoft.UI.Xaml.Markup.ReflectionXamlMetadataProvider.CreateXamlType(System.Type typeID)	不明
 	Microsoft.UI.Xaml.Markup.dll!Microsoft.UI.Xaml.Markup.ReflectionXamlMetadataProvider.getXamlType(System.Type typeID)	不明
 	Microsoft.UI.Xaml.Markup.dll!Microsoft.UI.Xaml.Markup.ReflectionXamlMetadataProvider.GetXamlType(System.Type type)	不明
>	BrowserControl.dll!kurema.BrowserControl.BrowserControl_XamlTypeInfo.XamlTypeInfoProvider.GetXamlTypeByType(System.Type type) 行 79	C#
 	BrowserControl.dll!kurema.BrowserControl.BrowserControl_XamlTypeInfo.XamlMetaDataProvider.GetXamlType(System.Type type) 行 40	C#
 	BookViewerApp.exe!BookViewerApp.BookViewerApp_XamlTypeInfo.XamlTypeInfoProvider.CheckOtherMetadataProvidersForType(System.Type type) 行 2518	C#
 	BookViewerApp.exe!BookViewerApp.BookViewerApp_XamlTypeInfo.XamlTypeInfoProvider.GetXamlTypeByType(System.Type type) 行 133	C#
 	BookViewerApp.exe!BookViewerApp.BookViewerApp_XamlTypeInfo.XamlMetaDataProvider.GetXamlType(System.Type type) 行 92	C#
 	BookViewerApp.exe!BookViewerApp.App.GetXamlType(System.Type type) 行 39	C#
 	BookViewerApp.McgInterop.dll!Windows.UI.Xaml.Markup.IXamlMetadataProvider__Impl.Vtbl.GetXamlType__n(System.IntPtr pComThis, System.Type__Impl.UnsafeType unsafe_type, void** unsafe_result__retval)	C#
 	[ネイティブからマネージドへの移行]	
 	[マネージドからネイティブへの移行]	
 	System.Private.Interop.dll!System.Runtime.InteropServices.McgMarshal.ActivateInstance(string typeName) 行 1244	C#
 	BookViewerApp.McgInterop.dll!Microsoft.UI.Xaml.Controls.XamlControlsResources.XamlControlsResources()	C#
 	BookViewerApp.exe!BookViewerApp.BookViewerApp_XamlTypeInfo.XamlTypeInfoProvider.Activate_0_XamlControlsResources() 行 665	C#
 	BookViewerApp.exe!BookViewerApp.BookViewerApp_XamlTypeInfo.Activator.Invoke()	C#
 	BookViewerApp.exe!BookViewerApp.BookViewerApp_XamlTypeInfo.XamlUserType.ActivateInstance() 行 7235	C#
 	BookViewerApp.McgInterop.dll!__Interop.ReverseComStubs.Stub_14(object __this, void** unsafe_result__retval, System.IntPtr __methodPtr)	C#
 	BookViewerApp.McgInterop.dll!Windows.UI.Xaml.Markup.IXamlType__Impl.Vtbl.ActivateInstance__n(System.IntPtr pComThis, void** unsafe_result__retval)	C#
```

* https://github.com/microsoft/microsoft-ui-xaml/issues/2545
* https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/4254
* https://stackoverflow.com/questions/70088739/uwp-app-starts-on-startup-when-compiled-with-net-native (No answer)

* [XamlControlsResources.properties.cpp](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dev/Generated/XamlControlsResources.properties.cpp)
