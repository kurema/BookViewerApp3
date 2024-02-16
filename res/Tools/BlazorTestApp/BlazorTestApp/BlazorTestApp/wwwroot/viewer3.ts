interface JsonImageEntry {
    folder?: string;
    name?: string;
    isFolder?: boolean;
    size?: number;
    updated?: string;
    [k: string]: unknown;
}

interface JsonImage {
    currentDirectory?: string;
    previewFile?: string;
    rootName?: string;
    basePath?: string;
    pageDirection?: "left" | "right" | "down";
    entries?: JsonImageEntry[];
    [k: string]: unknown;
}

class Vector2d {
    X: number;
    Y: number;

    constructor(x: number = 0, y: number = 0) {
        this.X = x;
        this.Y = y;
    }

    Reset() {
        this.X = 0;
        this.Y = 0;
    }

    Set(x: number = 0, y: number = 0) {
        this.X = x;
        this.Y = y;
    }

    Duplicate(): Vector2d {
        return new Vector2d(this.X, this.Y);
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

class Page {
    CurrentPage: number;
    CurrentPageShift: boolean;
}

class Helper {
    static IsImage(f: string): boolean {
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