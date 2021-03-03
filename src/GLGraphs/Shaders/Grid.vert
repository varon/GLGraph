#version 330 core

uniform float VerticalSize;
uniform float Aspect;
uniform vec2 Position;
uniform mat4 InvViewProjectionMatrix;

layout(location = 0) in vec2 InPosition;

out vec2 fUv;

void main(void) {

    gl_Position = vec4(InPosition, 0, 1);
    vec2 uv = (InvViewProjectionMatrix * vec4(InPosition,0,1)).xy;
    fUv = uv;
//    fUv.x = fUv.x * Aspect;
//    fUv = fUv;// * VerticalSize;
    //    VColor = InColor;
}
