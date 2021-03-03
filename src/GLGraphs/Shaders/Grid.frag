#version 330

uniform vec4 Color;
uniform float minorXGridSpacing;
uniform float majorXGridSpacing;
uniform float minorYGridSpacing;
uniform float majorYGridSpacing;

uniform float majorXAlpha;
uniform float minorXAlpha;
uniform float majorYAlpha;
uniform float minorYAlpha;

uniform float originXAlpha;
uniform float originYAlpha;

in vec2 fUv;

out vec4 FragColor;


float screenSpaceLines(vec2 spacing, float thickness, vec2 fPos, vec2 alpha) {
    fPos /= spacing;
    vec2 width = (thickness * fwidth(fPos));
    vec2 grid = vec2(1) - round(abs(fract(fPos - 0.5) - 0.5) / width);
    grid *= alpha;
    float line = max(grid.x, grid.y);
    return line;
}

float axisLines(float thickness, vec2 fPos, vec2 alpha) {
    vec2 thinWidth = (thickness * fwidth(fPos));
    vec2 grid = vec2(1) - round(abs(fPos) / thinWidth);
    grid *= alpha;
    float line = max(grid.x, grid.y);
    return line;
}

void main()
{
    vec2 minorSpacing = vec2(minorXGridSpacing, minorYGridSpacing);
    vec2 minorAlpha = vec2(minorXAlpha, minorYAlpha);
    vec2 majorSpacing = vec2(majorXGridSpacing, majorYGridSpacing);
    vec2 majorAlpha = vec2(majorXAlpha, majorYAlpha);
    float thinLines = screenSpaceLines(minorSpacing, 1.0, fUv, minorAlpha)*0.25;
    float thickLines = screenSpaceLines(majorSpacing, 3, fUv, majorAlpha)*0.325;
    
    vec2 axisAlpha = vec2(originXAlpha, originYAlpha);
    float axisLines = axisLines(3.0, fUv, axisAlpha);
    float mask = max(thinLines, thickLines);
    mask = max(mask,axisLines);
    FragColor = vec4(Color.rgb, Color.a*mask);
}
