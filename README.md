# BookViewerApp 3
Major update of BookViewer.  
Ver 2 failed.

## ToDo
とりあえず今の予定。
思いついた時にチェックします。
自分用のメモなので英語にはしない。

- [ ] 見開き機能
- - [ ] ViewModelと連携
- - [ ] 強制1ページ表示
- - [ ] Controlに切り替えUI
- - [ ] ContextMenuに切り替えUI
- [x] Epub対応
- - [x] epub.js+WebView。~~Zip展開はUWP側で。~~
- - [x] [bibi](https://github.com/satorumurmur/bibi)+WebView (Bibiの方が良い感じだった。)
- [x] 追加機能分の多言語対応
- [ ] ContextMenu
- - [x] Library/Items/Unregister
- - [x] Bookmarks/Preset/HidePreset
- - [ ] Storages/Folder/Rename
- [ ] Viewer
- - [ ] Sibling view
- [ ] Library
- - [x] Storage
- - [x] ViewModel
- - [ ] View/Library
- - [ ] View/Management
- [x] History
- - [x] Storage
- - [ ] ~~Restore~~ (起動時リストアは…とりあえず却下)
- - [x] View/ViewModel (File Managerで)
- [x] File Manager (Libraryと分ける必要ある？ 全フォルダアクセス権は与えたくない。)
- - [x] View/Tree
- - [x] View/Main
- - [x] Detail
- [ ] Monetize (以前は1円も収入がなかった。やる気出なかった理由)
- - [ ] ~~UWP ad~~ サードパーティーもサポートされてるMicrosoft Advertising SDKがキャンセルされるのかは良く分からない。新しいタブのページ。
- - [ ] ~~Amazon Affiliate。正直微妙。このアプリに合わない。~~
- - [ ] Donation. 幅広いチャンネルから。
- - [ ] Original ad. Kindle本・招待コード・YouTubeチャンネル(今後)。
- [ ] プロトコル対応
- - [ ] Samba. (Windows 10 Mobileが死んだ今は意味がないけど、独自でやりたい。)
- - [ ] SFTP? (面倒。不要。)
- - [ ] Cloud storage. (PDFをクラウドに上げる人ならいるかもね。)
- [x] toc
- - [x] CBZ (フォルダ構造から)
- - [x] PDF (iText的なもので解析)
- - [x] ViewModels / Views
- [x] iText
- - [x] 右綴じ確認
- [x] Password
- - [x] UI
- - [x] File
- - [x] Save password
- [ ] Publish!
- - [ ] Publish as beta.
- - [ ] Publish as product.

## Issue
- [x] 高速スクロール時、SetBitmapAsyncの順序が前後する。対策方法が思いつかん。

## Thanks
Italian translation is by Emanuele (https://github.com/Manu99it).
Thank you!

