shader_type spatial;
render_mode specular_schlick_ggx;

uniform vec4 wood_color : source_color = vec4(.36, .23, .14, 1.0);
uniform float grain_offset = 0.0;
varying vec3 local_position;

float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
void vertex() { local_position = VERTEX; }
void fragment() {
    vec2 p = local_position.xz + vec2(grain_offset * .31, grain_offset);
    float bend = noise(p * vec2(.23, 1.4)) * 2.0 + sin(p.x * .5) * .16;
    float grain = noise(vec2(p.x * 1.2, p.y * 85.0 + bend));
    float pores = noise(vec2(p.x * 9.0, p.y * 320.0 + bend * 4.0));
    float streak = .5 + .5 * sin(p.y * 90.0 + bend * 6.0);
    ALBEDO = wood_color.rgb * (.82 + grain * .23 + pores * .07 + streak * .05);
    ROUGHNESS = .64 + grain * .16;
    SPECULAR = .28;
}
