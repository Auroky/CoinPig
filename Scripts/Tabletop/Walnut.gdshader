// 三维桌面木纹：局部坐标提供纹理，世界坐标提供跨木板一致的中央亮区。
shader_type spatial;

varying vec2 grain_uv;
varying vec2 desk_uv;
uniform vec4 wood_color : source_color = vec4(0.46, 0.29, 0.19, 1.0);
uniform float grain_offset = 0.0;

float hash21(vec2 sample_point) {
    return fract(sin(dot(sample_point, vec2(127.1, 311.7))) * 43758.5453);
}

float noise2(vec2 sample_point) {
    vec2 cell = floor(sample_point);
    vec2 fraction = fract(sample_point);
    fraction = fraction * fraction * (3.0 - 2.0 * fraction);
    return mix(
        mix(hash21(cell), hash21(cell + vec2(1, 0)), fraction.x),
        mix(hash21(cell + vec2(0, 1)), hash21(cell + vec2(1, 1)), fraction.x),
        fraction.y
    );
}

void vertex() {
    // 每块木板的偏移打散重复纹理；亮区仍以整张桌子的世界位置计算。
    grain_uv = VERTEX.xz + vec2(grain_offset, grain_offset * 0.73);
    desk_uv = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xz;
}

void fragment() {
    vec2 wood_point = grain_uv;
    float warp = noise2(wood_point * vec2(0.29, 0.88)) * 0.45 + noise2(wood_point * 0.8) * 0.07;
    float strand = noise2(vec2(wood_point.x * 0.42, (wood_point.y + warp) * 27.0));
    float veins = sin((wood_point.y + warp) * 79.0 + 2.0 * noise2(wood_point * 2.1));
    float longgrain = noise2(vec2(wood_point.x * 0.19, (wood_point.y + warp) * 5.4));
    float light_pool = 1.13 - 0.39 * clamp(length(desk_uv / vec2(10.5, 7.4)), 0.0, 1.0);
    ALBEDO = wood_color.rgb * light_pool * (0.83 + 0.25 * longgrain + 0.10 * strand - 0.065 * smoothstep(0.56, 1.0, veins));
    ROUGHNESS = 0.70 + 0.12 * strand;
}
