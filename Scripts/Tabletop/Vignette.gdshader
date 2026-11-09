shader_type canvas_item;
render_mode unshaded;
void fragment() {
    float radius = length((UV - vec2(.5,.48)) * vec2(.9,1.05));
    float corners = smoothstep(.28,.72,radius) * .28;
    float top = (1.0 - smoothstep(0.0,.18,UV.y)) * .62;
    float bottom = smoothstep(.77,1.0,UV.y) * .65;
    COLOR = vec4(.035,.031,.025,clamp(corners + top + bottom,0.0,.82));
}
