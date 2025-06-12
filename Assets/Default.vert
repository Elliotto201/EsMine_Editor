#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

uniform mat4 uModel;
uniform mat3 normalMatrix;
uniform mat4 uViewProjection;

out vec3 vNormal;
out vec2 TexCoord;

void main()
{
    gl_Position = uViewProjection * uModel * vec4(aPosition, 1.0);
    vNormal = normalize(normalMatrix * aNormal);
    TexCoord = aTexCoord;
}
