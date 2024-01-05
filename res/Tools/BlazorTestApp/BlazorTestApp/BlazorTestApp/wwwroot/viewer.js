var __classPrivateFieldGet = (this && this.__classPrivateFieldGet) || function (receiver, state, kind, f) {
    if (kind === "a" && !f) throw new TypeError("Private accessor was defined without a getter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot read private member from an object whose class did not declare it");
    return kind === "m" ? f : kind === "a" ? f.call(receiver) : f ? f.value : state.get(receiver);
};
var __classPrivateFieldSet = (this && this.__classPrivateFieldSet) || function (receiver, state, value, kind, f) {
    if (kind === "m") throw new TypeError("Private method is not writable");
    if (kind === "a" && !f) throw new TypeError("Private accessor was defined without a setter");
    if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot write private member to an object whose class did not declare it");
    return (kind === "a" ? f.call(receiver, value) : f ? f.value = value : state.set(receiver, value)), value;
};
var _DrawOptionHorizontal_Delta, _DrawOptionDown_Delta;
class Page {
    constructor(image) {
        this.Path = image.folder + "/" + image.name;
        this.Data = image;
        this.IsLoading = false;
        this.IsLoaded = false;
        this.Image = null;
    }
    async Load() {
        await new Promise((resolve, reject) => {
            if (this.IsLoaded) {
                resolve();
                return;
            }
            const img = new Image();
            img.src = this.Path;
            this.IsLoading = true;
            img.onerror = () => {
                reject();
            };
            img.onload = () => {
                this.Image = img;
                this.IsLoaded = true;
                this.IsLoading = false;
                resolve();
            };
        });
    }
    get Width() { var _a; return (_a = this.Image) === null || _a === void 0 ? void 0 : _a.width; }
    get Height() { var _a; return (_a = this.Image) === null || _a === void 0 ? void 0 : _a.height; }
    Free() {
        this.Image = null;
        this.IsLoaded = false;
        this.IsLoading = false;
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
class PageCombinationEntry {
    constructor(page, pageState) {
        this.Page = page;
        this.PageState = pageState;
    }
}
class Vector2 {
    constructor(x, y) {
        this.X = x;
        this.Y = y;
    }
    Reset() {
        this.X = 0;
        this.Y = 0;
    }
    Set(x, y) {
        this.X = x;
        this.Y = y;
    }
}
class ZoomInformation {
    constructor(centerX, centerY, factor = 1.0) {
        //This vector is relative to height of image.
        this.Center = new Vector2(centerX, centerY);
        this.Factor = factor;
        this.FactorInitial = factor;
    }
}
class DrawOptionHorizontal {
    constructor(direction, delta, widthCanvas, widthImage) {
        _DrawOptionHorizontal_Delta.set(this, void 0);
        this.Direction = direction;
        this.Delta = delta;
        this.WidthCanvas = widthCanvas;
        this.WidthImage = widthImage;
    }
    get Delta() {
        return __classPrivateFieldGet(this, _DrawOptionHorizontal_Delta, "f");
    }
    set Delta(value) {
        __classPrivateFieldSet(this, _DrawOptionHorizontal_Delta, value, "f");
    }
    get Opacity() {
        let p = this.Delta.X;
        if (this.Direction == PageDirections.Right) {
            p = -p;
        }
        if (p <= 0)
            return 1.0;
        return Math.abs(p / this.WidthCanvas * 2.0);
    }
    get Factor() {
        let p = this.Delta.X;
        if (this.Direction == PageDirections.Right) {
            p = -p;
        }
        if (p <= 0)
            return 1.0;
        const minf = 0.5;
        return minf + (1.0 - minf) * Math.abs(p / this.WidthCanvas * 2.0);
    }
    get Shift() {
        return new Vector2(this.Delta.X, 0);
    }
}
_DrawOptionHorizontal_Delta = new WeakMap();
class DrawOptionDown {
    constructor(delta) {
        _DrawOptionDown_Delta.set(this, void 0);
        this.Delta = delta;
    }
    get Delta() {
        return __classPrivateFieldGet(this, _DrawOptionDown_Delta, "f");
    }
    set Delta(value) {
        __classPrivateFieldSet(this, _DrawOptionDown_Delta, value, "f");
    }
    get Opacity() { return 1.0; }
    get Factor() { return 1.0; }
    get Shift() { return new Vector2(0, this.Delta.Y); }
}
_DrawOptionDown_Delta = new WeakMap();
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
            if (a.Page.Width == null)
                return;
            result += a.Page.Width / a.Page.Height;
        });
        return result;
    }
    get PageLength() {
        //Length of page simply matches length of Combinations.
        return this.Combinations.length;
    }
    async Draw(option, ctx) {
        if (this.IsEmpty)
            return;
        const w = window.innerWidth;
        const h = window.innerHeight;
        const asp = this.Aspect;
        const aspcan = w / h;
        ctx.globalAlpha = option.Opacity;
        if (aspcan > asp) {
            for (let i = 0; i < this.Combinations.length; i++) {
                const entry = this.Combinations[i];
                await entry.Page.Load();
                //ctx.drawImage(entry.Page.Image);
            }
        }
        ctx.globalAlpha = 1.0;
    }
}
//const topDiv = document.getElementsByClassName("top")[0];
//const canvas = <HTMLCanvasElement>(document.getElementById("mainCanvas"));
//const ctx = canvas.getContext("2d");
