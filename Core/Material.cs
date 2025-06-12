using Microsoft.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using EngineInternal;

namespace EngineCore;

public class Material
{
    private int Handle;
    private Color4 Color;
    private bool HasTexture = false;

    public Material(string texturePath, TextureWrapMode wrapMode)
    {
        SetTexture(texturePath, wrapMode);
        HasTexture = true;
        CheckError();
    }

    public Material(Color4 color)
    {
        HasTexture = false;
        Color = color;
    }

    public void ApplyFrame()
    {
        int hasLocation = GL.GetUniformLocation(Window.BuildWindow.shaderProgram, "uHasTexture");
        GL.Uniform1(hasLocation, HasTexture ? 1 : 0);

        if (HasTexture)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }
        else
        {
            int colLocation = GL.GetUniformLocation(Window.BuildWindow.shaderProgram, "uColor");
            GL.Uniform4(colLocation, Color);
        }

        CheckError();
    }

    private static void CheckError()
    {
        ErrorCode errorCode = GL.GetError();
        if (errorCode != ErrorCode.NoError)
        {
            throw new Exception("Error code: " + errorCode);
        }
    }

    public void SetTexture(string texturePath, TextureWrapMode wrapMode)
    {
        HasTexture = true;

        // Check if texture is already loaded
        if (MaterialManager.LoadedTextures.TryGetValue(texturePath, out int cachedHandle))
        {
            Handle = cachedHandle;
            return; // If the texture is cached, no need to reload
        }

        // If not cached, load and store the texture
        int handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);

        using var stream = File.Open("../../../Textures/" + texturePath, FileMode.Open);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        int width = image.Width;
        int height = image.Height;
        int channels = 4; // RGBA
        byte[] flipped = new byte[image.Data.Length];

        for (int y = 0; y < height; y++)
        {
            Array.Copy(
                image.Data,                         // source array
                y * width * channels,
                flipped,
                (height - 1 - y) * width * channels,
                width * channels
            );
        }

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            image.Width, image.Height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, flipped);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapMode);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        // Cache the texture handle by the texture path
        MaterialManager.LoadedTextures[texturePath] = handle;

        Handle = handle;
    }

    public void SetColor(Color4 color)
    {
        HasTexture = false;
        Color = color;
    }

    private static class MaterialManager
    {
        public static Dictionary<string, int> LoadedTextures = new();
    }
}