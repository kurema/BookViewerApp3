//namespace Book4
{
    let DrawModes;
    (function (DrawModes) {
        DrawModes[DrawModes["Static"] = 0] = "Static";
        DrawModes[DrawModes["Animation"] = 1] = "Animation";
    })(DrawModes || (DrawModes = {}));
    class CanvasState {
        constructor(canvas) {
            this.Canvas = canvas;
            this.Context = canvas.getContext("2d");
        }
    }
    class BookCanvas {
        Draw() {
        }
    }
}
