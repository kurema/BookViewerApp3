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

    Free(): void {
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

    Reset(): void {
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
    get Delta(): Vector2;
    set Delta(value: Vector2);
}

class DrawOptionHorizontal implements IDrawOption {
    Direction: PageDirections;
    WidthCanvas: number;
    WidthImage: number;
    #Delta: Vector2;

    constructor(direction, delta, widthCanvas, widthImage) {
        this.Direction = direction;
        this.Delta = delta;
        this.WidthCanvas = widthCanvas;
        this.WidthImage = widthImage;
    }

    get Delta(): Vector2 {
        return this.#Delta;
    }
    set Delta(value: Vector2) {
        this.#Delta = value;
    }
    get Opacity(): number {
        let p = this.Delta.X;
        if (this.Direction == PageDirections.Right) {
            p = -p;
        }
        if (p <= 0) return 1.0;
        return Math.abs(p / this.WidthCanvas * 2.0);
    }
    get Factor(): number {
        let p = this.Delta.X;
        if (this.Direction == PageDirections.Right) {
            p = -p;
        }
        if (p <= 0) return 1.0;
        const minf = 0.5;
        return minf + (1.0 - minf) * Math.abs(p / this.WidthCanvas * 2.0);
    }
    get Shift(): Vector2 {
        return new Vector2(this.Delta.X, 0);
    }
}

class DrawOptionDown implements IDrawOption {
    #Delta: Vector2;

    constructor(delta) {
        this.Delta = delta;
    }

    get Delta(): Vector2 {
        return this.#Delta;
    }
    set Delta(value: Vector2) {
        this.#Delta = value;
    }

    get Opacity() { return 1.0; }

    get Factor() { return 1.0; }

    get Shift() { return new Vector2(0, this.Delta.Y); }
}

class PageCombination {
    Combinations: PageCombinationEntry[];

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
            if (a.Page.Width == null) return;
            result += a.Page.Width / a.Page.Height;
        });
        return result;
    }

    get PageLength() {
        //Length of page simply matches length of Combinations.
        return this.Combinations.length;
    }

    async Draw(option: IDrawOption, ctx: CanvasRenderingContext2D) {
        if (this.IsEmpty) return;
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
