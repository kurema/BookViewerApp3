class CanvasManager {
    Setup(canvas) {
        if (!canvas)
            throw new Error("canvas not found!");
        this.Canvas = canvas;
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
        this.Context = canvas.getContext("webgl2");
        if (!this.Context)
            throw new Error("webgl2 is not supported.");
        this.VertexShaderDefault = this.InitShader('VERTEX_SHADER', `
    attribute vec4 a_position;
    varying vec2 vTexCoord;

    void main() {
      gl_Position = a_position;
      vTexCoord=vec2((a_position.x+1.0)/2.0,(1.0-a_position.y)/2.0);
    }
  `);
        this.FragmentShaderRealisticScroll = this.InitShader('FRAGMENT_SHADER', `
        precision highp float;
        uniform sampler2D uSampler;
        uniform vec2 canvasSize;
        uniform vec2 textureSize;
        varying vec2 vTexCoord;
    void main() {
      vec2 texPoint = vec2(vTexCoord.x*canvasSize.x/canvasSize.y*textureSize.y/textureSize.x, vTexCoord.y);
      gl_FragColor = texPoint.x < 0.0 || texPoint.x > 1.0 || texPoint.y < 0.0 || texPoint.y > 1.0 ? vec4(0.9,0.9,0.9,1.0) : texture2D(uSampler,texPoint);
    }
    `);
        this.SampleTexture = this.LoadTexture("Sample.png");
    }
    Draw() {
        const gl = this.Context;
        const program = this.Context.createProgram();
        if (!program)
            throw new Error("Failed to create program");
        gl.attachShader(program, this.VertexShaderDefault);
        gl.attachShader(program, this.FragmentShaderRealisticScroll);
        gl.linkProgram(program);
        if (!gl.getProgramParameter(program, gl.LINK_STATUS))
            throw new Error(`Failed to link shader ${gl.getProgramInfoLog(program)}`);
        gl.useProgram(program);
        const positionBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, positionBuffer);
        const positions = [
            -1, -1,
            1, -1,
            -1, 1,
            -1, 1,
            1, 1,
            1, -1
        ];
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, this.SampleTexture);
        gl.uniform1i(gl.getUniformLocation(program, "uSampler"), 0);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(positions), gl.STATIC_DRAW);
        const index = gl.getAttribLocation(program, 'a_position');
        gl.vertexAttribPointer(index, 2, gl.FLOAT, false, 0, 0);
        gl.enableVertexAttribArray(index);
        {
            gl.uniform2f(gl.getUniformLocation(program, "canvasSize"), this.Canvas.clientWidth, this.Canvas.clientHeight);
            gl.uniform2f(gl.getUniformLocation(program, "textureSize"), Math.max(1, this.SampleImage.width), Math.max(1, this.SampleImage.height));
        }
        gl.drawArrays(gl.TRIANGLES, 0, 6);
    }
    InitShader(
    //name: string,
    type, source) {
        const gl = this.Context;
        if (!gl)
            throw new Error("webgl2 is not loaded.");
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
    LoadTexture(url) {
        //https://developer.mozilla.org/ja/docs/Web/API/WebGL_API/Tutorial/Using_textures_in_WebGL
        //https://sbfl.net/blog/2016/09/08/webgl2-tutorial-texture/
        const gl = this.Context;
        if (!gl)
            throw new Error("webgl2 is not loaded.");
        const texture = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([200, 200, 200, 255]));
        const image = new Image();
        this.SampleImage = image;
        image.onload = () => {
            gl.bindTexture(gl.TEXTURE_2D, texture);
            // Actual image format is unpredictable but it looks ok.
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGB, gl.RGB, gl.UNSIGNED_BYTE, image);
            //WebGL2 support non power of 2 images.
            //gl.generateMipmap(gl.TEXTURE_2D);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
            //gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        };
        image.src = url;
        return texture;
    }
}
