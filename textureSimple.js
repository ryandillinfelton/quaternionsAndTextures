/*
   Author: R. Felton and M. Olker
   Date: 1/17/17
   Class: CSC3360
*/

var triangleVertexPositionBuffer;

var mouseX;
var mouseY;
var keyChar;
var eye;
var at;
var dtor;
var theta;
var phi;

var locationOfU_time;
var locationOfEye;
var locationOfAt;
var timeLoad;

var gl;
var program;

function mouseMove(event)
{
    mouseX = event.clientX;
    mouseY = event.clientY;
}
function setUp(){
   eye = vec3(0.0,3.0,15.0);
   at = vec3(0.0,0.0,0.0);
   p = normalize(subtract(at,eye));
   dtor = Math.PI / 180;
   phi = Math.acos(p[1]);
   theta = Math.acos(p[2]/(Math.sin(phi)))/dtor;
   phi = phi / dtor;
}
window.addEventListener("keydown",
function keyboard(event)
{
   keyChar = event.keyCode;
   var forward = normalize(subtract(at,eye));
   var orientation = vec3(Math.sin(0.0),Math.cos(0.0), 0.0);
   var right = normalize(cross(forward,orientation));
   var left = normalize(cross(orientation,forward));
   if (keyChar == 83) {        //S
        eye = subtract(eye,forward);
        at = subtract(at,forward);
   } else if (keyChar == 68) { //D
        eye = add(eye,right);
        at = add(at,right);
   } else if (keyChar == 87) {//W
        eye = add(eye,forward);
        at = add(at,forward);
   } else if (keyChar == 65) {//A
        eye = add(eye,left);
        at = add(at,left);
   } else if (keyChar == 40) {//down
        phi++;
        if(Math.abs(phi)<=0)
          phi += 360;
        x = Math.sin(phi*dtor)*Math.sin(theta*dtor);
        y = Math.cos(phi*dtor);
        z = Math.sin(phi*dtor)*Math.cos(theta*dtor);
        p = vec3(x,y,z);
        at = add(eye,p);
   } else if (keyChar == 39) {//right
        theta--;
        if(Math.abs(theta)<=0)
          theta += 360;
        x = Math.sin(phi*dtor)*Math.sin(theta*dtor);
        y = Math.cos(phi*dtor);
        z = Math.sin(phi*dtor)*Math.cos(theta*dtor);
        p = vec3(x,y,z);
        at = add(eye,p);
   } else if (keyChar == 38) {//up
        phi--;
        if(Math.abs(phi)>=360)
          phi -= 360;
        x = Math.sin(phi*dtor)*Math.sin(theta*dtor);
        y = Math.cos(phi*dtor);
        z = Math.sin(phi*dtor)*Math.cos(theta*dtor);
        p = vec3(x,y,z);
        at = add(eye,p);
   } else if (keyChar == 37) {//left
        theta++;
        if(Math.abs(theta)>=360)
          theta -= 360;
        x = Math.sin(phi*dtor)*Math.sin(theta*dtor);
        y = Math.cos(phi*dtor);
        z = Math.sin(phi*dtor)*Math.cos(theta*dtor);
        p = vec3(x,y,z);
        at = add(eye,p);
   }
} );


//===================================================================
//initialzes the buffers and adds the coordinates of the verts of the
//triangle
function initBuffers(gl,program)
{
   var triangle = new Float32Array([-1.0,-1.0,0.0,
                                    1.0,1.0,0.0,
                                    1.0,-1.0,0.0,
                                    -1.0,-1.0,0.0,
                                    1.0,1.0,0.0,
                                    -1.0,1.0,0.0]);

   triangleVertexPositionBuffer = gl.createBuffer();

   gl.bindBuffer(gl.ARRAY_BUFFER, triangleVertexPositionBuffer);
   gl.bufferData(gl.ARRAY_BUFFER,triangle,gl.STATIC_DRAW);
   triangleVertexPositionBuffer.itemSize = 3;
   triangleVertexPositionBuffer.numItems = 6;

   gl.bindBuffer(gl.ARRAY_BUFFER,null);

    var squareCoords = new Float32Array([-1.0,-1.0, 0.0,
                                          -1.0, 1.0, 0.0,
                                          1.0, -1.0, 0.0,
                                          1.0, -1.0, 0.0,
                                          -1.0, 1.0, 0.0,
                                          1.0,1.0, 0.0]);
   squareCoordBuffer = gl.createBuffer();
   gl.bindBuffer(gl.ARRAY_BUFFER, squareCoordBuffer);
   gl.bufferData(gl.ARRAY_BUFFER, squareCoords, gl.STATIC_DRAW);
   squareCoordBuffer.itemSize = 3;
   squareCoordBuffer.numItems = 6;

   var texCoords = new Float32Array([0.0, 0.0,
                                      0.0, 1.0,
                                     1.0, 0.0,
                                     1.0, 0.0,
                                     0.0, 1.0,
                                     1.0, 1.0]);
   texCoordBuffer = gl.createBuffer();
   gl.bindBuffer(gl.ARRAY_BUFFER, texCoordBuffer);
   gl.bufferData(gl.ARRAY_BUFFER, texCoords, gl.STATIC_DRAW);
   texCoordBuffer.itemSize = 2;
   texCoordBuffer.numItems = 6;
   gl.bindBuffer(gl.ARRAY_BUFFER,null);
}

//==================================================================
function initTexture()
{
  hpuTexture = gl.createTexture();
  hpuTexture.image = new Image();
  hpuTexture.image.src = "testImg.gif";
  hpuTexture.image.onload = function() {
    handleLoadedTexture(hpuTexture)
  }
}

function handleLoadedTexture(texture)
{
   gl.bindTexture(gl.TEXTURE_2D, texture);
   gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
   gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, texture.image);
   gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
   gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.REPEAT);
   gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
   gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
   gl.bindTexture(gl.TEXTURE_2D, null);
}
//Function to draw the triangle from the buffer
/*
function render()
{
   gl.clear(gl.COLOR_BUFFER_BIT);

   //update u_time
   var updateTimeVal = (performance.now() - timeLoad)/1000;
   gl.uniform1f(locationOfU_time, updateTimeVal);
   gl.uniform3fv(locationOfEye, eye);
   gl.uniform3fv(locationOfAt, at);



   //Associate our shader variables with our data buffer
   gl.bindBuffer(gl.ARRAY_BUFFER,triangleVertexPositionBuffer);
   var vPosition = gl.getAttribLocation(program, "aVertexPosition");
   gl.vertexAttribPointer(vPosition, triangleVertexPositionBuffer.itemSize,
            gl.FLOAT, false, 0, 0);
   gl.enableVertexAttribArray(vPosition);

   gl.drawArrays(gl.TRIANGLES,0,triangleVertexPositionBuffer.numItems);

   gl.disableVertexAttribArray(vPosition);
   gl.bindBuffer(gl.ARRAY_BUFFER,null);
   //console.log("render func done");
   window.requestAnimationFrame(render);
}
*/

function render()
{
   var vPosition = gl.getAttribLocation( program, "a_Position" );
   var vTexcoord = gl.getAttribLocation( program, "a_Texcoord" );
   gl.clear( gl.COLOR_BUFFER_BIT );
   //update u_time
   var updateTimeVal = (performance.now() - timeLoad)/1000;
   gl.uniform1f(locationOfU_time, updateTimeVal);
   gl.uniform3fv(locationOfEye, eye);
   gl.uniform3fv(locationOfAt, at);
   // Tell WebGL how to pull out the positions from the position
   // buffer into the vertexPosition attribute and texture position attribute
   gl.bindBuffer(gl.ARRAY_BUFFER, squareCoordBuffer);
   gl.vertexAttribPointer(vPosition, squareCoordBuffer.itemSize, gl.FLOAT, false, 0, 0 );
   gl.enableVertexAttribArray( vPosition );
   gl.bindBuffer(gl.ARRAY_BUFFER, texCoordBuffer);
   gl.vertexAttribPointer(vTexcoord, 2, gl.FLOAT, false, 0, 0);
   gl.enableVertexAttribArray( vTexcoord );
   //activate and bind TEXTURE 0
   gl.activeTexture(gl.TEXTURE0);
   gl.bindTexture(gl.TEXTURE_2D, hpuTexture);
   //tell the fragment shader that we have bound to TEXTURE 0
   gl.uniform1i(gl.getUniformLocation(program, "u_Texture"), 0);
   //draw the texture onto the square
   gl.drawArrays( gl.TRIANGLES, 0, squareCoordBuffer.numItems);
   //cleanup
   gl.disableVertexAttribArray(texCoords = new Float32Array(vPosition ));
   gl.disableVertexAttribArray( vTexcoord );
   gl.bindTexture(gl.TEXTURE_2D, null);
   window.requestAnimationFrame(render);
}

//main driving function that runs when the body of the HTML is loaded
function webGLStart()
{
   var canvas = document.getElementById("gl-canvas");



   //
   // Configure WebGL
   //

   gl = initGL(canvas);
   if(gl){

      gl.viewport(0,0,gl.viewportWidth,gl.viewportHeight);
      gl.clearColor(1.0,1.0,1.0,1.0);
      gl.enable(gl.DEPTH_TEST);

      //Load shaders
      program = initShaders(gl);
      gl.useProgram(program);

      setUp();

      //compute u_time
      timeLoad = performance.now();

      //pass values to uniforms
      var locationOfU_resolition = gl.getUniformLocation(program, "u_resolution");
      var locationOfU_mouse = gl.getUniformLocation(program, "u_mouse");
      locationOfU_time = gl.getUniformLocation(program, "u_time");
      locationOfEye = gl.getUniformLocation(program, "eye");
      locationOfAt= gl.getUniformLocation(program, "at");


      gl.uniform2f(locationOfU_resolition, gl.viewportWidth, gl.viewportHeight);
      gl.uniform2f(locationOfU_mouse, mouseX, mouseY);
      //gl.uniform1f(locationOfU_time, 0);


      //load the data into the GPU, i.e. initialize buffers
      initBuffers(gl,program);
      initTexture();

      //render(gl,program);
      window.requestAnimationFrame(render);
   } else {
      alert("Failed to initialize webGL in browser - exiting!");
   }
}