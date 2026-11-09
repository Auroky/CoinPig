shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;
void fragment() {
    float d=length((UV-0.5)*2.0);
    ALBEDO=vec3(0.10,0.058,0.039);
    ALPHA=(1.0-smoothstep(0.13,1.0,d))*0.44;
}
