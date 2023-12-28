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
