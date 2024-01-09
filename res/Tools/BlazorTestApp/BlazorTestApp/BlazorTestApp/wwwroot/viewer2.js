class Vector2d {
    constructor(x = 0, y = 0) {
        this.X = x;
        this.Y = y;
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
class CursorState {
    constructor() {
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
class Book {
}
