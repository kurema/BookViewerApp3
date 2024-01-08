namespace Book1 {
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

        constructor(combinations: PageCombinationEntry[]) {
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

    class Book {
        #currentPage: number;
        Images: Array<Page>;
        PageMode: PageModes;
        IsPreviewed: boolean;
        PageCombinations: PageCombination[];
        Direction: PageDirections;

        constructor(info) {
            this.Images = info.entries.filter(a => Helper.IsImage(a.name)).map(a => new Page(a));
            this.#currentPage = this.Images.findIndex(a => a.Path == info.previewFile);
            this.PageMode = PageModes.AutoDetect;
            this.IsPreviewed = false;
            this.PageCombinations = [new PageCombination([]), new PageCombination([]), new PageCombination([])];
            this.Direction = PageDirections.Left;
        }

        get PageCount() { return this.Images.length; }

        get CurrentPage() { return this.Images[this.#currentPage]; }

        async UpdatePageCombination() {
            if (this.#currentPage === 0) {
                this.PageCombinations[0] = new PageCombination([]);
            }
            //ToDo: Implement
        }

        async PageShiftPlus() {
            if (this.PageCombinations[2].PageLength == 0) return;
            this.PageCombinations[0] = this.PageCombinations[1];
            this.PageCombinations[1] = this.PageCombinations[2];
            if (this.IsSingleHalf(this.PageCombinations[1], this.Direction == PageDirections.Right ? PageStates.LeftHalf : PageStates.RightHalf)) {
                this.PageCombinations[2] = new PageCombination([new PageCombinationEntry(this.PageCombinations[0].Combinations[0].Page, PageDirections.Right ? PageStates.RightHalf : PageStates.LeftHalf)])
            } else {
                this.PageCombinations[2] = await this.GetCombination(this.#currentPage + this.PageCombinations[1].PageLength);
            }
            this.#currentPage += this.PageCombinations[0].PageLength;
        }

        async PageShiftMinus() {
            if (this.PageCombinations[0].PageLength == 0) return;
            this.PageCombinations[1] = this.PageCombinations[0];
            this.PageCombinations[2] = this.PageCombinations[1];
            if (this.IsSingleHalf(this.PageCombinations[1], this.Direction == PageDirections.Left ? PageStates.LeftHalf : PageStates.RightHalf)) {
                this.PageCombinations[0] = new PageCombination([new PageCombinationEntry(this.PageCombinations[0].Combinations[0].Page, PageDirections.Left ? PageStates.RightHalf : PageStates.LeftHalf)])
            } else {
                this.PageCombinations[0] = await this.GetCombination(this.#currentPage - this.PageCombinations[1].Combinations.length, -1);
            }
            this.#currentPage += this.PageCombinations[1].PageLength;
        }

        IsSingleHalf(combination, direction) {
            switch (this.PageMode) {
                case PageModes.AutoDetect:
                case PageModes.Portrait:
                    if (combination.Combinations.length != 1) return false;
                    return combination.Combinations[0].PageState == direction;
                default:
                    return false;
            }
        }

        IsPageInRange(page): boolean {
            return page < this.Images.length && page >= 0;
        }

        async GetCombination(pageNumber, shift = +1): Promise<PageCombination> {
            if (!this.IsPageInRange(pageNumber)) return new PageCombination([]);
            const w = window.innerWidth;
            const h = window.innerHeight;

            let currentPageMode = PageModes.Simple;
            switch (this.PageMode) {
                case PageModes.Simple:
                    currentPageMode = PageModes.Simple;
                    break;
                case PageModes.AutoDetect:
                    if (w > h) currentPageMode = PageModes.Spread;
                    else currentPageMode = PageModes.Portrait;
                    break;
                case PageModes.Portrait:
                    currentPageMode = PageModes.Portrait;
                    break;
                case PageModes.Spread:
                    currentPageMode = PageModes.Spread;
                    break;
            }
            switch (currentPageMode) {
                case PageModes.Simple:
                    return new PageCombination([new PageCombinationEntry(this.Images[pageNumber], PageStates.Full)]);
                case PageModes.Portrait:
                    {
                        const img = this.Images[pageNumber];
                        await img.Load();
                        return new PageCombination([new PageCombinationEntry(img, img.Width <= img.Height ? PageStates.Full : (this.Direction == PageDirections.Left ? PageStates.RightHalf : PageStates.LeftHalf))]);
                    }
                    break;
                case PageModes.Spread:
                    {
                        //0: Simple
                        //1: Page 0 - Page 1
                        //2: Page 1 - Page 0
                        let layout = -1;
                        const page0 = shift > 0 ? pageNumber : pageNumber + shift;
                        const page1 = shift > 0 ? pageNumber + shift : pageNumber;
                        if (this.IsPageInRange(pageNumber + shift)) {
                            var imgs = [this.Images[page0], this.Images[page1]];
                            await imgs[0].Load();
                            await imgs[1].Load();
                            if (imgs[0].Width > imgs[0].Height || imgs[1].Width > imgs[1].Height) layout = 0;
                            else if (this.Direction == PageDirections.Left) layout = 2
                            else layout = 1;
                        } else {
                            layout = 0;
                        }
                        switch (layout) {
                            case 0:
                                return new PageCombination([new PageCombinationEntry(this.Images[pageNumber], PageStates.Full)]);
                            case 1:
                                return new PageCombination([new PageCombinationEntry(this.Images[page0], PageStates.Full), new PageCombinationEntry(this.Images[page1], PageStates.Full)]);
                            case 2:
                                return new PageCombination([new PageCombinationEntry(this.Images[page1], PageStates.Full), new PageCombinationEntry(this.Images[page0], PageStates.Full)]);
                        }
                    }
                default:
                    break;
            }
        }

        async #PageShift(shift) {
            const shifted = this.#currentPage + shift;
            if (shifted >= 0 && shifted < this.Images.length) {
                this.#currentPage = shifted;
                //await this.Draw();
            }
        }

        async PageShiftRight() {
            await this.#PageShift(-1);
        }

        async PageShiftLeft() {
            await this.#PageShift(+1);
        }

        //async Draw(canvas: HTMLCanvasElement, ctx: CanvasRenderingContext2D) {
        //    const w = window.innerWidth;
        //    const h = window.innerHeight;
        //    canvas.width = w;
        //    canvas.height = h;

        //    try {
        //        await this.CurrentPage.Load();
        //    } catch {
        //        //読み込み失敗の場合ページを削除する。
        //        return;
        //    }
        //    ctx.beginPath();
        //    ctx.fillStyle = "#DDD";
        //    ctx.fillRect(0, 0, w, h);
        //    const cw = this.CurrentPage.Width;
        //    const ch = this.CurrentPage.Height;

        //    const z = Math.min(w / cw, h / ch);
        //    const iw = cw * z;
        //    const ih = ch * z;
        //    ctx.drawImage(this.CurrentPage.Image, (w - iw) / 2.0 + shiftPoint.X, Math.min(Math.max(0, (h - ih) / 2.0 + shiftPoint.Y), h - ih), iw, ih);
        //}
    }

    const topDiv = document.getElementsByClassName("top")[0];
    const canvas = <HTMLCanvasElement>(document.getElementById("mainCanvas"));
    const ctx = canvas.getContext("2d");

    const info = JSON.parse(document.getElementById("info").innerText);
    const book = new Book(info);

    window.addEventListener('resize', OnResize, false);
    canvas.addEventListener("pointerdown", PointerDown, false)
    canvas.addEventListener("pointermove", PointerMove, false)
    canvas.addEventListener("pointerup", PointerUp, false)

    let lastClickTime = null;
    //https://github.com/mdn/dom-examples/blob/main/pointerevents/Pinch_zoom_gestures.html
    let evCache = new Array();
    let prevDiff = -1;
    let shiftPoint = new Vector2(0, 0);

    //book.Draw();

    function PointerDown(event) {
        if (event.button != 0) return;
        evCache.push(event);
    }

    async function PointerMove(event) {
        if (evCache.length == 1) {
            let evO = evCache[0];
            shiftPoint.Set(event.screenX - evO.screenX, event.screenY - evO.screenY);
            //await book.Draw();
            return;
        } else if (evCache.length == 2) {
            //await book.Draw();
        }
    }

    async function PointerUp(event) {
        if (event.button != 0) return;
        const w = window.innerWidth;
        const x = event.screenX;
        const y = event.screenY;
        if (evCache.length == 2) {
            //Zoom finished.
            return;
        } else if (evCache.length != 1) {
            return;
        }
        const spx = evCache[0].screenX;
        const spy = evCache[0].screenY;
        if (lastClickTime != null && (Date.now() - lastClickTime) < 300) {
            // Double click!
            //    startPoint = null;
            //    return;
        }
        lastClickTime = Date.now();
        if (((x - spx) ** 2 + (y - spy) ** 2) < 100) {
            if (x < w / 4) {
                await book.PageShiftLeft();
            } else if (x >= w * 3 / 4) {
                await book.PageShiftRight();
            }
        } else {
            if ((x - spx) * 4 > w) {
                await book.PageShiftLeft();
            } else if ((x - spx) * 4 < -w) {
                await book.PageShiftRight();
            }
        }
        shiftPoint.Reset();
        //await book.Draw();

        for (var i = 0; i < evCache.length; i++) {
            if (evCache[i].pointerId == event.pointerId) {
                evCache.splice(i, 1);
                break;
            }
        }
    }

    async function OnResize() {
        evCache = new Array();
        shiftPoint.Reset();
        //await book.Draw();
    }

    class Helper {
        static IsImage(f): boolean {
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
}