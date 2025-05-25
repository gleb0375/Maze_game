using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Serilog;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SysMat = System.Numerics.Matrix4x4;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;

namespace semestral_work.Graphics
{
    /// <summary>
    /// Loads and renders a car model.
    /// v2.3 – ground-plane filter refactored + detailed English logs.
    /// </summary>
    internal sealed class CarModel : IDisposable
    {
        private readonly struct Part
        {
            public readonly int Texture;
            public readonly int FirstIndex;
            public readonly int IndexCount;
            public readonly bool Transparent;

            public Part(int tex, int first, int count, bool transp)
            { Texture = tex; FirstIndex = first; IndexCount = count; Transparent = transp; }
        }

        private static readonly string[] GroundKeywords =
            { "plane", "floor", "ground", "mat", "carpet" };

        private static readonly StringComparison Cmp = StringComparison.OrdinalIgnoreCase;

        private readonly int _vao, _vbo, _ebo;
        private readonly Shader _shader;
        private readonly List<Part> _parts = new();

        public float BaseShiftY { get; }
        public float DefaultYaw { get; }

        public CarModel(string glbPath, Shader shader)
        {
            Log.Information("In car Model");
            if (!File.Exists(glbPath)) throw new FileNotFoundException(glbPath);
            _shader = shader ?? throw new ArgumentNullException(nameof(shader));

            var root = ModelRoot.Load(glbPath);
            var scene = root.DefaultScene ?? root.LogicalScenes[0];

            var vertices = new List<float>();
            var indices = new List<uint>();
            uint baseVert = 0;
            var mat2tex = new Dictionary<Material, int>();

            foreach (var node in scene.VisualChildren)
                Collect(node, ref baseVert, vertices, indices, mat2tex);

            // ── GPU buffers ───────────────────────────────────────────
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float),
                          vertices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint),
                          indices.ToArray(), BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);                // position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));// normal
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));// uv
            GL.EnableVertexAttribArray(2);
            GL.BindVertexArray(0);

            // ── bounding-box without ground ──────────────────────────
            Vector3 min = new(float.MaxValue);
            Vector3 max = new(float.MinValue);
            for (int i = 0; i < vertices.Count / 8; i++)
            {
                var v = new Vector3(vertices[i * 8 + 0], vertices[i * 8 + 1], vertices[i * 8 + 2]);
                min = Vector3.ComponentMin(min, v);
                max = Vector3.ComponentMax(max, v);
            }
            BaseShiftY = -min.Y;                                   // place car on floor Y=0
            DefaultYaw = (max.X - min.X) > (max.Z - min.Z) ? MathF.PI * .5f : 0f;

            Log.Information("CarModel → {Materials} materials, {Parts} parts, {Vertices} verts",
                            mat2tex.Count, _parts.Count, vertices.Count / 8);
        }

        // ── ground filter ───────────────────────────────────────────
        private static bool IsGroundPrimitive(Node node, MeshPrimitive prim)
        {
            bool ContainsAny(string? s) =>
                !string.IsNullOrEmpty(s) && GroundKeywords.Any(k => s!.IndexOf(k, Cmp) >= 0);

            bool nameMatch = ContainsAny(node?.Name)
                          || ContainsAny(node?.Mesh?.Name)
                          || ContainsAny(prim.Material?.Name);

            bool geomMatch = false;
            try
            {
                var pos = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var norm = prim.GetVertexAccessor("NORMAL").AsVector3Array();
                geomMatch = pos.Count == 4 &&
                            norm.All(v => Math.Abs(v.X) < 1e-4 && Math.Abs(v.Z) < 1e-4);
            }
            catch { /* missing accessors – ignore */ }

            if (nameMatch || geomMatch)
            {
                Log.Information("Filtered ground primitive: Node='{Node}', Mesh='{Mesh}', Material='{Mat}', " +
                          "NameMatch={NameMatch}, GeomMatch={GeomMatch}",
                          node?.Name, node?.Mesh?.Name, prim.Material?.Name, nameMatch, geomMatch);
            }

            return nameMatch || geomMatch;
        }

        // ── transparent-material detection (any SharpGLTF version) ──
        private static bool IsTransparent(Material? mat)
        {
            if (mat == null) return false;

            bool IsBlend(string? v) =>
                v == "BLEND" || v == "Blend" || v == "MASK" || v == "Mask";

            var p = mat.GetType().GetProperty("AlphaMode", BindingFlags.Public | BindingFlags.Instance)
                 ?? mat.GetType().GetProperty("Alpha", BindingFlags.Public | BindingFlags.Instance);

            string? mode = p?.GetValue(mat)?.ToString();
            if (p == null)
            {
                // extremely old preview: method Alpha()
                var m = mat.GetType().GetMethod("Alpha", BindingFlags.Public | BindingFlags.Instance,
                                                Type.DefaultBinder, Type.EmptyTypes, null);
                mode = m?.Invoke(mat, null)?.ToString();
            }

            bool transparent = IsBlend(mode);
            if (transparent)
                Log.Information("Transparent material detected: '{Mat}' (mode={Mode})", mat.Name, mode);
            return transparent;
        }

        // ── recursive build ─────────────────────────────────────────
        private void Collect(Node node,
                             ref uint baseVertex,
                             List<float> verts,
                             List<uint> idx,
                             Dictionary<Material, int> mat2tex)
        {
            if (node.Mesh != null)
            {
                Matrix4 matWorld = ToMatrix4(node.WorldMatrix);
                Matrix3 normalM = new Matrix3(matWorld).Inverted().Transposed();

                foreach (var prim in node.Mesh.Primitives)
                {
                    if (IsGroundPrimitive(node, prim)) continue;          // skip ground

                    int tex = GetOrLoadTexture(prim.Material, mat2tex);
                    bool isTransp = IsTransparent(prim.Material);

                    int first = idx.Count;
                    uint off = baseVertex;

                    var primIdx = prim.GetIndices().Select(i => (uint)i + off).ToArray();
                    idx.AddRange(primIdx);

                    _parts.Add(new Part(tex, first, primIdx.Length, isTransp));
                    Log.Debug("Added part: Node='{Node}', Mesh='{Mesh}', Material='{Mat}', " +
                              "Indices={Count}, Transparent={Transp}",
                              node.Name, node.Mesh.Name, prim.Material?.Name,
                              primIdx.Length, isTransp);

                    var pos = prim.GetVertexAccessor("POSITION").AsVector3Array();
                    var norm = prim.GetVertexAccessor("NORMAL").AsVector3Array();
                    var uv = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

                    for (int i = 0; i < pos.Count; i++)
                    {
                        Vector3 p = Vector3.TransformPosition(
                                        new(pos[i].X, pos[i].Y, pos[i].Z), matWorld);
                        Vector3 n = Vector3.Normalize(
                                        normalM * new Vector3(norm[i].X, norm[i].Y, norm[i].Z));

                        float tx = uv != null ? uv[i].X : 0f;
                        float ty = uv != null ? uv[i].Y : 0f;

                        verts.AddRange(new[] { p.X, p.Y, p.Z, n.X, n.Y, n.Z, tx, ty });
                    }
                    baseVertex += (uint)pos.Count;
                }
            }
            foreach (var child in node.VisualChildren)
                Collect(child, ref baseVertex, verts, idx, mat2tex);
        }

        // ── texture cache ───────────────────────────────────────────
        private int GetOrLoadTexture(Material? mat, Dictionary<Material, int> cache)
        {
            if (mat != null && cache.TryGetValue(mat, out int cached)) return cached;

            int id;
            var img = mat?.FindChannel("BaseColor")?.Texture?.PrimaryImage;
            if (img == null)
            {
                byte[] px = { 255, 255, 255, 255 };                   // white 1×1
                id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                              1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, px);
            }
            else
            {
                id = TextureLoader.LoadTextureFromMemory(img.Content.Content.ToArray());
            }
            if (mat != null) cache[mat] = id;
            return id;
        }

        private static Matrix4 ToMatrix4(SysMat m) =>
            new(m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);

        // ── render ──────────────────────────────────────────────────
        public void Render(Matrix4 model, Matrix4 view, Matrix4 proj,
                           Vector3 lightPos, Vector3 lightDir,
                           float cutCos, float range, Vector3 camPos)
        {
            _shader.Use();
            int h = _shader.Handle;

            GL.UniformMatrix4(GL.GetUniformLocation(h, "uModel"), false, ref model);
            GL.UniformMatrix4(GL.GetUniformLocation(h, "uView"), false, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(h, "uProjection"), false, ref proj);

            GL.Uniform3(GL.GetUniformLocation(h, "uLightPos"), lightPos);
            GL.Uniform3(GL.GetUniformLocation(h, "uLightDir"), lightDir);
            GL.Uniform1(GL.GetUniformLocation(h, "uSpotCutoff"), cutCos);
            GL.Uniform1(GL.GetUniformLocation(h, "uLightRange"), range);
            GL.Uniform3(GL.GetUniformLocation(h, "uViewPos"), camPos);

            GL.BindVertexArray(_vao);

            // 1) opaque
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            foreach (var p in _parts.Where(p => !p.Transparent))
                DrawPart(p, h);

            // 2) alpha-blended
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false);
            foreach (var p in _parts.Where(p => p.Transparent))
                DrawPart(p, h);

            // restore
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            GL.BindVertexArray(0);
        }

        private static void DrawPart(Part p, int shaderHandle)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, p.Texture);
            GL.Uniform1(GL.GetUniformLocation(shaderHandle, "uBaseColor"), 0);

            IntPtr ofs = (IntPtr)(p.FirstIndex * sizeof(uint));
            GL.DrawElements(PrimitiveType.Triangles, p.IndexCount,
                            DrawElementsType.UnsignedInt, ofs);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            foreach (var tex in _parts.Select(p => p.Texture).Distinct())
                GL.DeleteTexture(tex);
        }
    }
}
