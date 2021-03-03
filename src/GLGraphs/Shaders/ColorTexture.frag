#version 330

uniform vec4 Color;
uniform sampler2D tex;

in vec2 fUv;

out vec4 fColor;

void main()
{
    float alpha = texture(tex, vec2(fUv.x, fUv.y)).r;
    fColor = vec4(Color.rgb, Color.a * alpha);
}
