
#version 330 core

uniform vec2 Scale;
uniform vec2 Position;
uniform mat4 ViewProjectionMatrix;

layout(location = 0) in vec2 InPosition;

out vec4 fColor;
out vec2 fUv;
out vec3 fPos;

void main(void) {
    
    gl_Position = ViewProjectionMatrix * vec4(InPosition * Scale + Position, -10, 1.0);

    fUv = InPosition.xy * 0.5 + 0.5;
//    VColor = InColor;
}
