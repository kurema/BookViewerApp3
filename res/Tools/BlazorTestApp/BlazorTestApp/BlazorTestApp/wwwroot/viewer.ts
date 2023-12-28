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

    async Load() {
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

    Free() {
        this.Image = null;
        this.IsLoaded = false;
        this.IsLoading = false;
    }
}