namespace Book4
{
    enum DrawModes {
        Static,
        Animation,
    }

    class CanvasState {
        Canvas: HTMLCanvasElement;
        Context: CanvasRenderingContext2D;

        constructor(canvas: HTMLCanvasElement) {
            this.Canvas = canvas;
            this.Context = canvas.getContext("2d");
        }
    }

    class BookCanvas {
        CurrentMode: DrawModes;
        CanvasStatus: CanvasState;

        Draw() {
            
        }
    }
}