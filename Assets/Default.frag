#version 330 core

out vec4 FragColor;
uniform sampler2D uTexture;
uniform vec4 uColor;
uniform bool uHasTexture;

in vec2 TexCoord;

void main()
{
if(uHasTexture){
    FragColor = texture(uTexture, TexCoord);
}
else{
    FragColor = uColor;
}
}

