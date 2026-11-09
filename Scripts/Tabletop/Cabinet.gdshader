shader_type spatial;
varying vec3 local_point;
uniform vec4 tint : source_color = vec4(0.4,0.25,0.16,1);
float hash(vec2 p) {return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p) {
    vec2 i=floor(p),f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
void vertex(){local_point=VERTEX;}
void fragment(){
    vec2 p=local_point.xy;
    float warp=noise(p*vec2(0.9,0.23));
    float grain=noise(vec2(p.x*31.0+warp*6.0,p.y*0.7));
    ALBEDO=tint.rgb*(0.71+0.17*grain);
    ROUGHNESS=0.82;
}
