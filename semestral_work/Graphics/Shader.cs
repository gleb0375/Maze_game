using OpenTK.Graphics.OpenGL4;
using Serilog;
using System;

namespace semestral_work.Graphics
{
    internal class Shader : IDisposable
    {
        public int Handle { get; private set; }

        /// <summary>
        /// Vytvoří a linkuje vertex a fragment shader do jednoho programu.
        /// </summary>
        public Shader(string vertexCode, string fragmentCode)
        {
            int vertexShader = CompileShader(ShaderType.VertexShader, vertexCode);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentCode);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string infoLog = GL.GetProgramInfoLog(Handle);
                Log.Error("Program link error: {Error}", infoLog);
                throw new Exception($"Program link error: {infoLog}");
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }


        /// <summary>
        /// Zkompiluje shader daného typu.
        /// </summary>
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

        public void Use() => GL.UseProgram(Handle);

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
        }
    }
}
