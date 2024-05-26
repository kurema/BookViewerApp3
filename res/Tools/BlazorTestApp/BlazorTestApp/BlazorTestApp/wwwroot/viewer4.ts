namespace Book4 {
    class CanvasState {
        Canvas: HTMLCanvasElement;
        Context: CanvasRenderingContext2D;

        constructor(canvas: HTMLCanvasElement) {
            this.Canvas = canvas;
            this.Context = canvas.getContext("2d");
        }
    }

    interface Animation {

    }

    class BookCanvas {
        CurrentAnnimation: Animation | null;
        CurrentCanvas: CanvasState;

        Draw() {
            
        }
    }
}