using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using Serilog;
using StbImageSharp;

namespace semestral_work.Graphics
{
    internal static class TextureLoader
    {
        /// <summary>
        /// Loads a texture from the specified file (e.g., jpg, png) and returns the OpenGL texture handle.
        /// </summary>
        /// <param name="path">Path to the texture file.</param>
        /// <returns>OpenGL texture handle.</returns>
        public static int LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                Log.Error("Texture file not found: {Path}", path);
                throw new FileNotFoundException($"Texture file not found: {path}");
            }

            // Read file into a byte array
            byte[] fileBytes = File.ReadAllBytes(path);
            Log.Information("Read texture file bytes from {Path}", path);

            // Use StbImageSharp to load image data with RGBA components
            var image = ImageResult.FromMemory(fileBytes, ColorComponents.RedGreenBlueAlpha);
            Log.Information("Image loaded: {Width}x{Height}", image.Width, image.Height);

            // Generate an OpenGL texture
            int textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);

            // Upload image data to the texture
            GL.TexImage2D(TextureTarget.Texture2D,
                level: 0,
                internalformat: PixelInternalFormat.Rgba,
                width: image.Width,
                height: image.Height,
                border: 0,
                format: PixelFormat.Rgba,
                type: PixelType.UnsignedByte,
                pixels: image.Data);

            // Set texture filtering parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Set texture wrapping parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // Unbind the texture
            GL.BindTexture(TextureTarget.Texture2D, 0);

            Log.Information("Texture loaded successfully: {Path}", path);
            return textureHandle;
        }
    }
}
