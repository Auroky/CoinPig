// 二维桌面使用 UV 合成木纹、木板接缝和边缘压暗，不需要额外纹理贴图。
shader_type canvas_item;
render_mode unshaded;

float hash(vec2 sample_point) {
    return fract(sin(dot(sample_point, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 sample_point) {
    vec2 cell = floor(sample_point);
    vec2 fraction = fract(sample_point);
    fraction = fraction * fraction * (3.0 - 2.0 * fraction);
    return mix(
        mix(hash(cell), hash(cell + vec2(1, 0)), fraction.x),
        mix(hash(cell + vec2(0, 1)), hash(cell + vec2(1, 1)), fraction.x),
        fraction.y
    );
}

void fragment() {
    vec2 uv = UV;
    float plank = floor(uv.y * 6.0);

    // 多层噪声分别形成粗木纹、细纹和节疤，随后按木板编号轻微改变明暗。
    float warp = noise(uv * vec2(3.0, 6.0)) * 4.0 + sin(uv.x * 9.0) * .3;
    float grain = noise(vec2(uv.x * 12.0, uv.y * 340.0 + warp));
    float fine = noise(vec2(uv.x * 50.0, uv.y * 1400.0 + warp * 3.0));
    float bands = .5 + .5 * sin((uv.y * 95.0 + warp) * 6.283);
    vec2 knot = (uv - vec2(.76, .30)) * vec2(2.2, 14.0);
    float ring = sin(length(knot) * 34.0) * exp(-length(knot) * 2.4);
    vec3 wood = vec3(.265, .179, .112) + vec3(.045, .029, .015) * (grain - .5);
    wood += vec3(.018, .011, .005) * (fine + bands * .4 + ring * .7 - .5);
    wood *= .94 + hash(vec2(plank, 3.0)) * .12;
    float seam = smoothstep(0.0, .012, fract(uv.y * 6.0));
    wood *= .53 + .47 * seam;

    float light = 1.22 - .75 * smoothstep(.1, .7, length((uv - vec2(.50, .45)) * vec2(1.05, 1.05)));
    float edge = smoothstep(0.0, .075, uv.x) * smoothstep(0.0, .075, 1.0 - uv.x);
    edge *= smoothstep(0.0, .13, uv.y) * smoothstep(0.0, .13, 1.0 - uv.y);
    COLOR = vec4(wood * light * (.5 + edge * .5), 1.0);
}
