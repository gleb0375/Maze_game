using OpenTK.Graphics.OpenGL4;
using Serilog;
using System;

namespace semestral_work.Graphics
{
    internal class Shader : IDisposable
    {
        public int handle { get; private set; }

        public Shader(string vertexCode, string fragmentCode)
        {
            int vertexShader = CompileShader(ShaderType.VertexShader, vertexCode);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentCode);

            handle = GL.CreateProgram();
            GL.AttachShader(handle, vertexShader);
            GL.AttachShader(handle, fragmentShader);
            GL.LinkProgram(handle);

            GL.GetProgram(handle, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string infoLog = GL.GetProgramInfoLog(handle);
                Log.Error("Program link error: {Error}", infoLog);
                throw new Exception($"Program link error: {infoLog}");
            }

            GL.DetachShader(handle, vertexShader);
            GL.DetachShader(handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private int CompileShader(ShaderType type, string code)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, code);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                Log.Error("Shader compile error ({Type}): {Error}", type, infoLog);
                throw new Exception($"Shader compile error ({type}): {infoLog}");
            }

            return shader;
        }

        public void Use() => GL.UseProgram(handle);

        public void Dispose()
        {
            GL.DeleteProgram(handle);
        }
    }
}
