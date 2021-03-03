#version 330

uniform vec4 Color;

in vec2 fUv;

out vec4 fColor;

void main()
{
    vec2 dxy = fwidth(fUv);
    float dy = 2*min(dxy.x, dxy.y);
    vec2 centeredUv = fUv * 2.0 - vec2(1);
    float mask = smoothstep(1, 1-dy, length(centeredUv));
    fColor = vec4(Color.rgb, mask);
}
