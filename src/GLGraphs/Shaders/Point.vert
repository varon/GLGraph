
#version 330 core

uniform vec2 Scale;
uniform vec2 Position;
uniform mat4 ViewProjectionMatrix;
uniform float Aspect;
uniform float PointSize;

layout(location = 0) in vec2 InPosition;
layout(location = 1) in vec2 InOffset;

out vec4 fColor;
out vec2 fUv;
out vec3 fPos;

void main(void) {
    
    vec2 offset = vec2(0);
    offset.x = InOffset.x / Aspect;
    offset.y = InOffset.y;
    offset *= PointSize;
    
    gl_Position = ViewProjectionMatrix * vec4(InPosition * Scale + Position, -10, 1.0) + vec4(offset, 0, 0);

    fUv = (InOffset + vec2(1)) / vec2(2);
    fUv.y = 1-fUv.y;
}
