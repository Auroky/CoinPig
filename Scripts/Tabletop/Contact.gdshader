// 猪脚下的柔和接触阴影：只叠加透明颜色，不写深度，避免遮挡模型。
shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;

void fragment() {
    float distance_from_center = length((UV - 0.5) * 2.0);
    ALBEDO = vec3(0.10, 0.058, 0.039);
    ALPHA = (1.0 - smoothstep(0.13, 1.0, distance_from_center)) * 0.44;
}
