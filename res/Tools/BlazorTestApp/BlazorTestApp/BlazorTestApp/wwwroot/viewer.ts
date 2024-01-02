class Page {
    Path: string;
    Data: any;
    IsLoading: boolean;
    IsLoaded: boolean;
    Image: HTMLImageElement | null;

    constructor(image: any) {
        this.Path = image.folder + "/" + image.name;
        this.Data = image;
        this.IsLoading = false;
        this.IsLoaded = false;
        this.Image = null;
    }

    async Load(): Promise<void> {
        await new Promise<void>((resolve, reject) => {
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
            }
        });
    }

    get Width(): number | null { return this.Image?.width; }
    get Height(): number | null { return this.Image?.height; }

    Free(): void{
        this.Image = null;
        this.IsLoaded = false;
        this.IsLoading = false;
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

class PageCombinationEntry {
    Page: Page;
    PageState: PageStates;

    constructor(page: Page, pageState: PageStates) {
        this.Page = page;
        this.PageState = pageState;
    }
}

class Vector2 {
    X: number;
    Y: number;

    constructor(x: number, y: number) {
        this.X = x;
        this.Y = y;
    }

    Reset(): void{
        this.X = 0;
        this.Y = 0;
    }

    Set(x, y): void {
        this.X = x;
        this.Y = y;
    }
}

class ZoomInformation {
    Center: Vector2;
    Factor: number;
    FactorInitial: number;

    constructor(centerX: number, centerY: number, factor = 1.0) {
        //This vector is relative to height of image.
        this.Center = new Vector2(centerX, centerY);
        this.Factor = factor;
        this.FactorInitial = factor;
    }
}

interface IDrawOption {
    get Opacity(): number;
    get Factor(): number;
    get Shift(): Vector2;
}