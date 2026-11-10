// 货架木纹使用模型局部坐标，模型移动或旋转时纹理随之移动。
shader_type spatial;
varying vec3 local_point;
uniform vec4 tint : source_color = vec4(0.4, 0.25, 0.16, 1);

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

void vertex() {
    local_point = VERTEX;
}

void fragment() {
    vec2 wood_point = local_point.xy;
    float warp = noise(wood_point * vec2(0.9, 0.23));
    float grain = noise(vec2(wood_point.x * 31.0 + warp * 6.0, wood_point.y * 0.7));
    ALBEDO = tint.rgb * (0.71 + 0.17 * grain);
    ROUGHNESS = 0.82;
}
