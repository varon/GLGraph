#version 330

uniform vec4 Color;
uniform sampler2D tex;


in vec2 fUv;

out vec4 FragColor;

void main()
{
    vec4 texCol = texture(tex, vec2(fUv.x, 1-fUv.y));
    float alpha = 1-texCol.r;
    FragColor = vec4(Color.rgb, Color.a * alpha);
}
