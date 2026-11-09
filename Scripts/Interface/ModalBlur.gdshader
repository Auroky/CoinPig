shader_type canvas_item;
render_mode unshaded;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;
uniform float blur_pixels = 2.0;

void fragment() {
    vec2 step_uv=SCREEN_PIXEL_SIZE*blur_pixels;
    vec3 blurred=vec3(0.0);
    float total=0.0;
    for(int x=-1;x<=1;x++){
        for(int y=-1;y<=1;y++){
            float weight=exp(-float(x*x+y*y)*0.48);
            blurred+=textureLod(screen_texture,SCREEN_UV+vec2(float(x),float(y))*step_uv,2.8).rgb*weight;
            total+=weight;
        }
    }
    blurred/=total;
    float edge=smoothstep(0.16,0.72,length((SCREEN_UV-vec2(0.5))*vec2(1.0,0.85)));
    vec3 warm=blurred*vec3(0.53,0.45,0.38);
    COLOR=vec4(mix(warm,vec3(0.025,0.020,0.016),edge*0.62),1.0);
}
