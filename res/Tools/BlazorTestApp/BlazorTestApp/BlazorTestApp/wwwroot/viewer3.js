var Book3;
(function (Book3) {
    class Vector2d {
        constructor(x = 0, y = 0) {
            this.X = x;
            this.Y = y;
        }
        Reset() {
            this.X = 0;
            this.Y = 0;
        }
        Set(x = 0, y = 0) {
            this.X = x;
            this.Y = y;
        }
        Duplicate() {
            return new Vector2d(this.X, this.Y);
        }
    }
    class Size2d {
        constructor(width = 0, height = 0) {
            this.Width = width;
            this.Height = height;
        }
    }
    let PageStates;
    (function (PageStates) {
        PageStates[PageStates["Full"] = 0] = "Full";
        PageStates[PageStates["LeftHalf"] = 1] = "LeftHalf";
        PageStates[PageStates["RightHalf"] = 2] = "RightHalf";
    })(PageStates || (PageStates = {}));
    let PageModes;
    (function (PageModes) {
        PageModes[PageModes["Simple"] = 0] = "Simple";
        PageModes[PageModes["AutoDetect"] = 1] = "AutoDetect";
        PageModes[PageModes["Spread"] = 2] = "Spread";
        PageModes[PageModes["Portrait"] = 3] = "Portrait";
    })(PageModes || (PageModes = {}));
    let PageDirections;
    (function (PageDirections) {
        PageDirections[PageDirections["Left"] = 0] = "Left";
        PageDirections[PageDirections["Right"] = 1] = "Right";
        PageDirections[PageDirections["Down"] = 2] = "Down";
    })(PageDirections || (PageDirections = {}));
    class Page {
    }
    class Helper {
        static IsImage(f) {
            const li = f.lastIndexOf(".");
            const ext = li < 0 ? "" : f.substring(li);
            switch (ext.toUpperCase()) {
                case ".JPEG":
                case ".JPG":
                case ".JFIF":
                case ".PJPEG":
                case ".PJP":
                case ".SVG":
                case ".GIF":
                case ".WEBP":
                case ".PNG":
                case ".APNG":
                case ".AVIF":
                case ".BMP":
                case ".ICO":
                case ".CUR":
                case ".TIF":
                case ".TIFF":
                    return true;
                default:
                    return false;
            }
        }
    }
})(Book3 || (Book3 = {}));
