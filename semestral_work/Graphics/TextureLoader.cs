using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using Serilog;
using StbImageSharp;

namespace semestral_work.Graphics
{
    internal static class TextureLoader
    {
        // Anisotropic filtering constants (EXT_texture_filter_anisotropic)
        private const int GL_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FE;
        private const int GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FF;

        public static int LoadTexture(string path)
        {
            ValidateFileExists(path);
            byte[] fileBytes = ReadFileBytes(path);
            var image = LoadImageData(fileBytes);
            int textureHandle = CreateAndBindTexture();
            UploadImage(image);
            SetTextureParameters();
            GenerateMipmaps();
            ApplyAnisotropicFiltering(); // !!!
            UnbindTexture();

            Log.Information("Texture loaded successfully: {Path}", path);
            return textureHandle;
        }

        private static void ValidateFileExists(string path)
        {
            if (!File.Exists(path))
            {
                Log.Error("Texture file not found: {Path}", path);
                throw new FileNotFoundException($"Texture file not found: {path}");
            }
        }

        private static byte[] ReadFileBytes(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Log.Information("Read texture file bytes from {Path}", path);
            return bytes;
        }

        private static ImageResult LoadImageData(byte[] fileBytes)
        {
            var image = ImageResult.FromMemory(fileBytes, ColorComponents.RedGreenBlueAlpha);
            Log.Information("Image loaded: {Width}x{Height}", image.Width, image.Height);
            return image;
        }

        private static int CreateAndBindTexture()
        {
            int textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);
            return textureHandle;
        }

        private static void UploadImage(ImageResult image)
        {
            GL.TexImage2D(TextureTarget.Texture2D,
                level: 0,
                internalformat: PixelInternalFormat.Rgba,
                width: image.Width,
                height: image.Height,
                border: 0,
                format: PixelFormat.Rgba,
                type: PixelType.UnsignedByte,
                pixels: image.Data);
        }

        private static void SetTextureParameters()
        {
            // Set texture filtering parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Set texture wrapping parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        }

        private static void GenerateMipmaps()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        private static void ApplyAnisotropicFiltering()
        {
            GL.GetFloat((GetPName)GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, out float maxAniso);
            float desiredAniso = maxAniso; // mozne zmenit MathF.Min(8.0f, maxAniso)
            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)GL_TEXTURE_MAX_ANISOTROPY_EXT, desiredAniso);
            Log.Information("Anisotropic filtering applied: {Level}", desiredAniso);
        }

        private static void UnbindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
