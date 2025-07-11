#version 450

#extension GL_KHR_vulkan_glsl : enable

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;

layout(location = 0) out vec3 v_color;
layout(location = 1) out vec3 v_normal;
layout(location = 2) out vec3 v_pos;
layout(location = 3) out vec3 v_viewPos;

layout(binding = 0) uniform UniformBufferObject {
    mat4 translation;
    mat4 rotation;
    mat4 scale;
    mat4 view;
    mat4 proj;
    vec3 viewPos;
} u_Ubo;

layout(binding = 1) uniform GizmoBufferObject
{
    vec3 color;
} u_GizmoUbo;

void main() {
    mat4 model = u_Ubo.translation * u_Ubo.rotation * u_Ubo.scale;
    gl_Position = u_Ubo.proj * u_Ubo.view * model * vec4(position, 1.0);

    mat3 normalMatrix = transpose(inverse(mat3(model)));

    v_pos = vec3(model * vec4(position, 1));
    v_viewPos = u_Ubo.viewPos;
    v_color = u_GizmoUbo.color;
    v_normal = normalMatrix * normal;
}