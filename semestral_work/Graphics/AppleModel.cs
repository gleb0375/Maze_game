using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;

namespace semestral_work.Graphics
{
    /// <summary>Загружает .glb и рендерит один меш (позиции+нормали).</summary>
    internal sealed class AppleModel : IDisposable
    {
        private readonly int _vao;
        private readonly int _vbo;
        private readonly int _ebo;
        private readonly int _indexCount;
        private readonly Shader _shader;

        public AppleModel(string glbPath, Shader shader)
        {
            _shader = shader ?? throw new ArgumentNullException(nameof(shader));
            if (!File.Exists(glbPath))
                throw new FileNotFoundException("GLB model not found", glbPath);

            var model = ModelRoot.Load(glbPath);
            var prim = model.LogicalMeshes[0].Primitives[0];

            var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
            var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();

            // ---- собираем массив float [pos.xyz, norm.xyz] ----
            var verts = new List<float>(positions.Count * 6);
            for (int i = 0; i < positions.Count; i++)
            {
                var p = positions[i];
                var n = normals != null && i < normals.Count ? normals[i] : System.Numerics.Vector3.UnitY;
                verts.AddRange(new[] { p.X, p.Y, p.Z, n.X, n.Y, n.Z });
            }

            var idx = prim.GetIndices().ToArray();
            _indexCount = idx.Length;

            // ---- в OpenGL ----
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Count * sizeof(float), verts.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, idx.Length * sizeof(uint), idx, BufferUsageHint.StaticDraw);

            // layout (0) = position, (1) = normal
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        public void Render(Matrix4 model, Matrix4 view, Matrix4 proj)
        {
            _shader.Use();

            GL.UniformMatrix4(GL.GetUniformLocation(_shader.Handle, "uModel"), false, ref model);
            GL.UniformMatrix4(GL.GetUniformLocation(_shader.Handle, "uView"), false, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(_shader.Handle, "uProjection"), false, ref proj);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
        }
    }
}
