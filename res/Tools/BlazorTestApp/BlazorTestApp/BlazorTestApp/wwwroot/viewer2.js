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
var PageStates;
(function (PageStates) {
    PageStates[PageStates["Full"] = 0] = "Full";
    PageStates[PageStates["LeftHalf"] = 1] = "LeftHalf";
    PageStates[PageStates["RightHalf"] = 2] = "RightHalf";
})(PageStates || (PageStates = {}));
var PageModes;
(function (PageModes) {
    PageModes[PageModes["Simple"] = 0] = "Simple";
    PageModes[PageModes["AutoDetect"] = 1] = "AutoDetect";
    PageModes[PageModes["Spread"] = 2] = "Spread";
    PageModes[PageModes["Portrait"] = 3] = "Portrait";
})(PageModes || (PageModes = {}));
var PageDirections;
(function (PageDirections) {
    PageDirections[PageDirections["Left"] = 0] = "Left";
    PageDirections[PageDirections["Right"] = 1] = "Right";
    PageDirections[PageDirections["Down"] = 2] = "Down";
})(PageDirections || (PageDirections = {}));
class CursorManager {
    constructor(drawCallback, pageShiftCallback) {
        this.EventCache = [];
        this.LastClickTime = -1;
        this.PreviousDiff = -1;
        this.ShiftPoint = new Vector2d();
        this.DrawCallback = drawCallback;
        this.PageShiftCallback = pageShiftCallback;
        this.CurrentWindow = window;
    }
    Setup(canvas, w) {
        this.CurrentWindow = w;
        w.addEventListener('resize', this.OnResize, false);
        canvas.Canvas.addEventListener("pointerdown", this.OnPointerDown, false);
        canvas.Canvas.addEventListener("pointermove", this.OnPointerMove, false);
        canvas.Canvas.addEventListener("pointerup", this.OnPointerMove, false);
    }
    OnResize(event) {
        this.EventCache = [];
        this.ShiftPoint.Reset();
        this.DrawCallback();
    }
    OnPointerDown(event) {
        if (event.button != 0)
            return;
        this.EventCache.push(event);
    }
    OnPointerMove(event) {
        if (this.EventCache.length == 1) {
            let ev0 = this.EventCache[0];
            this.ShiftPoint.Set(event.screenX - ev0.screenX, event.screenY - ev0.screenY);
            this.DrawCallback();
            return;
        }
        else if (this.EventCache.length == 2) {
            this.DrawCallback();
            return;
        }
    }
    async OnPointerUp(event) {
        if (event.button != 0)
            return;
        const w = this.CurrentWindow.innerWidth;
        const x = event.screenX;
        const y = event.screenY;
        if (this.EventCache.length == 2) {
            //Zoom finished.
            return;
        }
        else if (this.EventCache.length != 1) {
            return;
        }
        const spx = this.EventCache[0].screenX;
        const spy = this.EventCache[0].screenY;
        let isDoubleClick = (this.LastClickTime != null && (Date.now() - this.LastClickTime) < 300);
        this.LastClickTime = Date.now();
        if (((x - spx) ** 2 + (y - spy) ** 2) < 100) {
            if (x < w / 4) {
                await this.PageShiftCallback(-1);
                isDoubleClick = false;
            }
            else if (x >= w * 3 / 4) {
                await this.PageShiftCallback(-1);
                isDoubleClick = false;
            }
        }
        else {
            if ((x - spx) * 4 > w) {
                await this.PageShiftCallback(1);
                isDoubleClick = false;
            }
            else if ((x - spx) * 4 < -w) {
                await this.PageShiftCallback(-1);
                isDoubleClick = false;
            }
        }
        this.ShiftPoint.Reset();
        if (isDoubleClick) {
            return;
        }
        this.DrawCallback();
        for (var i = 0; i < this.EventCache.length; i++) {
            if (this.EventCache[i].pointerId == event.pointerId) {
                this.EventCache.splice(i, 1);
                break;
            }
        }
    }
}
class Page {
    constructor(image) {
        this.Path = image.folder + "/" + image.name;
        this.Data = image;
        this._IsLoading = false;
        this._IsLoaded = false;
        this.Image = null;
        this._LoadedActions = [];
        this._Size = null;
        this.HasError = false;
    }
    async Load() {
        await new Promise((resolve, reject) => {
            if (this._IsLoaded) {
                resolve();
                return;
            }
            else if (this._IsLoading) {
                this._LoadedActions.push(new function () { resolve(); });
                return;
            }
            this._IsLoading = true;
            const img = new Image();
            img.src = this.Path;
            img.onerror = () => {
                this.HasError = true;
                reject();
            };
            img.onload = () => {
                this.HasError = false;
                this.Image = img;
                this._IsLoaded = true;
                this._IsLoading = false;
                this._LoadedActions.forEach(f => f());
                this._LoadedActions = [];
                this._Size = new Size2d(img.width, img.height);
                resolve();
            };
        });
    }
    get Size() { var _a; return (_a = this._Size) !== null && _a !== void 0 ? _a : new Size2d(); }
    get SizeLoaded() { return this.Size === null; }
    Free() {
        this.Image = null;
        this._IsLoaded = false;
        this._IsLoading = false;
    }
}
class CanvasState {
    constructor(canvas) {
        this.Canvas = canvas;
        this.Context = canvas.getContext("2d");
    }
}
class PageCombinationEntry {
    constructor(page, pageState) {
        this.Page = page;
        this.PageState = pageState;
    }
}
class PageCombination {
    constructor(combinations) {
        //Array of PageCombinationEntry
        this.Combinations = combinations;
    }
    get IsEmpty() {
        return this.Combinations.length === 0;
    }
    get Aspect() {
        let result = 0;
        this.Combinations.forEach(a => {
            if (a.Page.Size.Width == null)
                return;
            result += a.Page.Size.Width / a.Page.Size.Height;
        });
        return result;
    }
    get PageLength() {
        //Length of page simply matches length of Combinations.
        return this.Combinations.length;
    }
}
class PageCombinationSet {
    constructor(combinations, target = -1) {
        this.Combinations = combinations;
        if (target !== -1) {
            this.CurrentPage = target;
        }
        else if (combinations.length >= 3) {
            this.CurrentPage = 1;
        }
        else {
            this.CurrentPage = 0;
        }
    }
}
class Book {
    constructor(info) {
        this.Images = info.entries.filter(a => Helper.IsImage(a.name)).map(a => new Page(a));
    }
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
