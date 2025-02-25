using OpenTK.Graphics.OpenGL4;
using Serilog;
using StbImageSharp;
using System;
using System.IO;

namespace semestral_work.Graphics
{
    internal static class TextureLoader
    {
        /// <summary>
        /// Загружает текстуру из указанного файла (jpg, png и т.д.).
        /// Возвращает дескриптор текстуры OpenGL.
        /// </summary>
        public static int LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                Log.Error("Texture file not found: {Path}", path);
                throw new FileNotFoundException($"Texture file not found: {path}");
            }

            // Считываем файл в массив байт
            byte[] fileBytes = File.ReadAllBytes(path);

            // Используем StbImageSharp для чтения пикселей
            var image = ImageResult.FromMemory(fileBytes, ColorComponents.RedGreenBlueAlpha);

            // Генерируем текстуру в OpenGL
            int textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);

            // Загружаем данные
            GL.TexImage2D(TextureTarget.Texture2D,
                level: 0,
                internalformat: PixelInternalFormat.Rgba,
                width: image.Width,
                height: image.Height,
                border: 0,
                format: PixelFormat.Rgba,
                type: PixelType.UnsignedByte,
                pixels: image.Data);

            // Настраиваем фильтрацию и повторение (wrap)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            Log.Information("Texture loaded successfully: {Path}", path);
            return textureHandle;
        }
    }
}
