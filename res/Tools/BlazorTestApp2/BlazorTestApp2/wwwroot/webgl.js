class Program {
    Main() {
        const canvas = document.querySelector("body canvas");
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
        if (!canvas)
            throw new Error("canvas not found!");
        const webgl = canvas.getContext("webgl2");
        if (!webgl)
            throw new Error("webgl2 is not supported.");
        webgl.clearColor(0.95, 0.95, 0.95, 1);
        webgl.clear(webgl.COLOR_BUFFER_BIT);
        this.vertexShader = this.InitShader(webgl, 'VERTEX_SHADER', `
    attribute vec4 a_position;
    //attribute vec2 a_texCoord;
    //varying vec2 vTexCoord;

    void main() {
      gl_Position = a_position;
      //vTexCoord=vec2(0.5,0.5);
    }
  `);
        this.fragmentShader = this.InitShader(webgl, 'FRAGMENT_SHADER', `
        //varying vec2 vTexCoord;
    void main() {
      gl_FragColor = vec4(0.05, 0, 0, 1);
    }

    `);
        this.program = webgl.createProgram();
        if (!this.program)
            throw new Error("Failed to create program");
        webgl.attachShader(this.program, this.vertexShader);
        webgl.attachShader(this.program, this.fragmentShader);
        webgl.linkProgram(this.program);
        if (!webgl.getProgramParameter(this.program, webgl.LINK_STATUS))
            throw new Error(`Failed to link shader ${webgl.getProgramInfoLog(this.program)}`);
        webgl.useProgram(this.program);
        const positionBuffer = webgl.createBuffer();
        webgl.bindBuffer(webgl.ARRAY_BUFFER, positionBuffer);
        const positions = [
            -1, -1,
            1, -1,
            -1, 1,
            -1, 1,
            1, 1,
            1, -1
        ];
        webgl.bufferData(webgl.ARRAY_BUFFER, new Float32Array(positions), webgl.STATIC_DRAW);
        const index = webgl.getAttribLocation(this.program, 'a_position');
        //const texCoord = webgl.getAttribLocation(this.program, 'a_texCoord');
        webgl.vertexAttribPointer(index, 2, webgl.FLOAT, false, 0, 0);
        webgl.enableVertexAttribArray(index);
        //webgl.vertexAttribPointer(texCoord, 2, webgl.FLOAT, false, 20, 12);
        //webgl.enableVertexAttribArray(texCoord);
        webgl.drawArrays(webgl.TRIANGLE_FAN, 0, 6);
    }
    InitShader(gl, type, source) {
        //https://zenn.dev/ixkaito/articles/webgl-typescript-vercel-logo
        const shader = gl.createShader(gl[type]);
        if (!shader) {
            throw new Error("Failed to create shared.");
        }
        gl.shaderSource(shader, source);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS))
            throw new Error(`Failed to compile shader: ${gl.getShaderInfoLog(shader)}`);
        return shader;
    }
}
