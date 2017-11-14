#ifdef GL_ES
precision mediump float;
#endif
attribute vec3 a_Position;
attribute vec2 a_Texcoord;
varying vec2 v_Texcoord;

void main()
{
	gl_Position = vec4(a_Position,1.0);
	v_Texcoord = a_Texcoord;
}



