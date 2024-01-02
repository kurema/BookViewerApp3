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
