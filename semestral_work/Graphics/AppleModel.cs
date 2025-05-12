using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;

namespace semestral_work.Graphics
{
    /// <summary>Грузит apple.glb: позиции, нормали, UV, базовую текстуру.</summary>
    internal sealed class AppleModel : IDisposable
    {
        private readonly int _vao, _vbo, _ebo, _tex;
        private readonly int _indexCount;
        private readonly Shader _shader;

        public AppleModel(string glbPath, Shader shader)
        {
            _shader = shader ?? throw new ArgumentNullException(nameof(shader));
            if (!File.Exists(glbPath)) throw new FileNotFoundException(glbPath);

            var model = ModelRoot.Load(glbPath);
            var prim = model.LogicalMeshes[0].Primitives[0];

            var pos = prim.GetVertexAccessor("POSITION").AsVector3Array();
            var norm = prim.GetVertexAccessor("NORMAL").AsVector3Array();
            var uv = prim.GetVertexAccessor("TEXCOORD_0").AsVector2Array();

            var verts = new List<float>(pos.Count * 8);
            for (int i = 0; i < pos.Count; i++)
            {
                verts.AddRange(new[] { pos[i].X, pos[i].Y, pos[i].Z,
                                       norm[i].X, norm[i].Y, norm[i].Z,
                                       uv[i].X,  uv[i].Y });
            }

            var idx = prim.GetIndices().ToArray();
            _indexCount = idx.Length;

            // -------- VAO/VBO/EBO --------
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          verts.Count * sizeof(float), verts.ToArray(),
                          BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                          idx.Length * sizeof(uint), idx,
                          BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            GL.BindVertexArray(0);

            // -------- базовая текстура из glTF --------
            var img = prim.Material?
                            .FindChannel("BaseColor")?
                            .Texture?
                            .PrimaryImage;
            if (img == null) throw new Exception("Texture not found in GLB");

            _tex = TextureLoader.LoadTextureFromMemory(img.Content.Content.ToArray());
        }

        public void Render(Matrix4 model, Matrix4 view, Matrix4 proj,
                           Vector3 lightPos, Vector3 lightDir,
                           float cutCos, float range, Vector3 camPos)
        {
            _shader.Use();

            GL.UniformMatrix4(GL.GetUniformLocation(_shader.Handle, "uModel"), false, ref model);
            GL.UniformMatrix4(GL.GetUniformLocation(_shader.Handle, "uView"), false, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(_shader.Handle, "uProjection"), false, ref proj);

            GL.Uniform3(GL.GetUniformLocation(_shader.Handle, "uLightPos"), lightPos);
            GL.Uniform3(GL.GetUniformLocation(_shader.Handle, "uLightDir"), lightDir);
            GL.Uniform1(GL.GetUniformLocation(_shader.Handle, "uSpotCutoff"), cutCos);
            GL.Uniform1(GL.GetUniformLocation(_shader.Handle, "uLightRange"), range);
            GL.Uniform3(GL.GetUniformLocation(_shader.Handle, "uViewPos"), camPos);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _tex);
            GL.Uniform1(GL.GetUniformLocation(_shader.Handle, "uBaseColor"), 0);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteTexture(_tex);
        }
    }
}
