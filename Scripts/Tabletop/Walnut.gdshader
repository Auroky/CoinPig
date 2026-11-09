shader_type spatial;

varying vec2 grain_uv;
varying vec2 desk_uv;
uniform vec4 wood_color : source_color = vec4(0.46,0.29,0.19,1.0);
uniform float grain_offset = 0.0;

float hash21(vec2 p) {
    return fract(sin(dot(p,vec2(127.1,311.7))) * 43758.5453);
}
float noise2(vec2 p) {
    vec2 i=floor(p);
    vec2 f=fract(p);
    f=f*f*(3.0-2.0*f);
    return mix(mix(hash21(i),hash21(i+vec2(1,0)),f.x),
               mix(hash21(i+vec2(0,1)),hash21(i+vec2(1,1)),f.x),f.y);
}
void vertex() {
    grain_uv=VERTEX.xz+vec2(grain_offset,grain_offset*0.73);
    desk_uv=(MODEL_MATRIX*vec4(VERTEX,1.0)).xz;
}
void fragment() {
    vec2 p=grain_uv;
    float warp=noise2(p*vec2(0.29,0.88))*0.45 + noise2(p*0.8)*0.07;
    float strand=noise2(vec2(p.x*0.42,(p.y+warp)*27.0));
    float veins=sin((p.y+warp)*79.0 + 2.0*noise2(p*2.1));
    float longgrain=noise2(vec2(p.x*0.19,(p.y+warp)*5.4));
    float light_pool=1.13-0.39*clamp(length(desk_uv/vec2(10.5,7.4)),0.0,1.0);
    ALBEDO=wood_color.rgb*light_pool*(0.83+0.25*longgrain+0.10*strand-0.065*smoothstep(0.56,1.0,veins));
    ROUGHNESS=0.70+0.12*strand;
}
