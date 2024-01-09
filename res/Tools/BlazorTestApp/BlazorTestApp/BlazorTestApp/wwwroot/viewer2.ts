interface JsonImageEntry {
    folder?: string;
    name?: string;
    isFolder?: boolean;
    size?: number;
    updated?: string;
    [k: string]: unknown;
}

class Vector2d {
    X: number;
    Y: number;

    constructor(x: number = 0, y: number = 0) {
        this.X = x;
        this.Y = y;
    }
}

class Size2d {
    Width: number;
    Height: number;

    constructor(width: number = 0, height: number = 0) {
        this.Width = width;
        this.Height = height;
    }
}

enum PageStates {
    Full,
    LeftHalf,
    RightHalf
}

enum PageModes {
    Simple,
    AutoDetect,
    Spread,
    Portrait,
}

enum PageDirections {
    Left,
    Right,
    Down,
}

class CursorState {
    CurrentPosition: Vector2d;

    constructor() {
    }
}

class Page {
    Path: string;
    Data: JsonImageEntry;
    private _IsLoading: boolean;
    private _IsLoaded: boolean;
    private _LoadedActions: Function[];
    private _Size: Size2d | null;
    Image: HTMLImageElement | null;
    HasError: boolean;

    constructor(image: JsonImageEntry) {
        this.Path = image.folder + "/" + image.name;
        this.Data = image;
        this._IsLoading = false;
        this._IsLoaded = false;
        this.Image = null;
        this._LoadedActions = [];
        this._Size = null;
        this.HasError = false;
    }

    async Load(): Promise<void> {
        await new Promise<void>((resolve, reject) => {
            if (this._IsLoaded) {
                resolve();
                return;
            } else if (this._IsLoading) {
                this._LoadedActions.push(new function () { resolve() });
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
            }
        });
    }

    get Size() { return this._Size ?? new Size2d(); }
    get SizeLoaded() { return this.Size === null; }

    Free(): void {
        this.Image = null;
        this._IsLoaded = false;
        this._IsLoading = false;
    }
}
class Book {
}