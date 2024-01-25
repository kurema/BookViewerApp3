This is failed attempt to make a OPDS library using xsd.exe. It failed.

[Source](https://github.com/opds-community/specs)

# Convert
```bash
trang -I rnc -O xsd opds.rnc opds.xsd 2> message.log
xsd.exe opds.xsd /c /edb /e:library /l:CS /n:BookViewerApp.Storages.OPDS
```

# Fix
[This issue](https://github.com/opds-community/specs/issues/3) is applied.
