class Program {
    Main() {
        const canvas = document.querySelector("body canvas") as HTMLCanvasElement;
        if (!canvas) throw new Error("canvas not found!");
        const webgl = canvas.getContext("webgl2")
        if (!webgl) throw new Error("webgl2 is not supported.");
        webgl.clearColor(0.95, 0.95, 0.95, 1);
        webgl.clear(webgl.COLOR_BUFFER_BIT);

        this.vertexShader = this.InitShader(webgl, 'VERTEX_SHADER', `
    attribute vec4 a_position;

    void main() {
      gl_Position = a_position;
    }
  `);
        this.fragmentShader = this.InitShader(webgl, 'FRAGMENT_SHADER', `
    void main() {
      gl_FragColor = vec4(0, 0, 0, 1);
    }
    `);
        this.program = webgl.createProgram();
        if (!this.program) throw new Error("Failed to create program");
        webgl.attachShader(this.program, this.vertexShader);
        webgl.attachShader(this.program, this.fragmentShader);
        webgl.linkProgram(this.program);
        if (!webgl.getProgramParameter(this.program, webgl.LINK_STATUS)) throw new Error(`Failed to link shader ${webgl.getProgramInfoLog(this.program)}`);

        webgl.useProgram(this.program);
        const positionBuffer = webgl.createBuffer();
        webgl.bindBuffer(webgl.ARRAY_BUFFER, positionBuffer);

        const positions = [
            -1.0, 1.0, 0.0, 0.0, 1.0,
            1.0, 1.0, 0.0, 1.0, 1.0,
            1.0, -1.0, 0.0, 1.0, 0.0,
            -1.0, -1.0, 0.0, 0.0, 0.0
        ];
        webgl.bufferData(webgl.ARRAY_BUFFER, new Float32Array(positions), webgl.STATIC_DRAW);

        const index = webgl.getAttribLocation(this.program, 'a_position');
        const size = 2;
        const type = webgl.FLOAT;
        const normalized = false;
        const stride = 0;
        const offset = 0;
        webgl.vertexAttribPointer(index, size, type, normalized, stride, offset);
        webgl.enableVertexAttribArray(index);

        webgl.drawArrays(webgl.TRIANGLES, 0, 3);
    }

    private vertexShader: WebGLShader;
    private fragmentShader: WebGLShader;
    private program: WebGLProgram;

    InitShader(gl: WebGL2RenderingContext, type: 'VERTEX_SHADER' | 'FRAGMENT_SHADER', source: string): WebGLShader {
        //https://zenn.dev/ixkaito/articles/webgl-typescript-vercel-logo
        const shader = gl.createShader(gl[type]);

        if (!shader) {
            throw new Error("Failed to create shared.");
        }
        gl.shaderSource(shader, source);
        gl.compileShader(shader);

        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw new Error(`Failed to compile shader: ${gl.getShaderInfoLog(shader)}`);

        return shader;
    }
}