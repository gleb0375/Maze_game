using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using semestral_work.Graphics;
using semestral_work.Map;
using Serilog;

internal sealed class MinimapRenderer : IDisposable
{
    private readonly ParsedMap _map;
    private readonly List<Matrix4> _walls;
    private readonly Shader _shader;
    private readonly int _vaoQuad;
    private readonly int _vaoArrow;

    private readonly int _miniSizePx;
    private readonly float _viewRadius;
    private readonly float _arrowSize;

    public MinimapRenderer(
        ParsedMap map,
        List<Matrix4> wallMatrices,
        Shader shader,
        int sizeInPx,
        float viewRadius,
        float arrowSize)
    {
        _map = map;
        _walls = wallMatrices;
        _shader = shader;
        _miniSizePx = sizeInPx;
        _viewRadius = viewRadius;
        _arrowSize = arrowSize;

        _vaoQuad = CreateVao(new float[] { 0, 0, 1, 0, 1, 1, 0, 1 });

        float b = _arrowSize * 0.6f;
        float[] arrow =
        {
             0f, _arrowSize,   // apex
            -b , -b ,          // left base
             b , -b            // right base
        };
        _vaoArrow = CreateVao(arrow);

        Log.Information("MinimapRenderer initialized with Size: {Size}px, ViewRadius: {Radius}, ArrowSize: {Arrow}",
                        _miniSizePx, _viewRadius, _arrowSize);
    }

    private static int CreateVao(float[] data)
    {
        int vao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, 0);
        GL.BindVertexArray(0);
        return vao;
    }

    /// <summary>Draw after the 3-D scene.</summary>
    public void Render(Camera cam, int winW, int winH)
    {
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);

        GL.Viewport(0, winH - _miniSizePx, _miniSizePx, _miniSizePx);

        _shader.Use();
        int locMvp = GL.GetUniformLocation(_shader.Handle, "uMVP");
        int locColor = GL.GetUniformLocation(_shader.Handle, "uColor");

        Matrix4 proj = Matrix4.CreateOrthographicOffCenter(
            -_viewRadius, _viewRadius,
            -_viewRadius, _viewRadius,
            -1f, 1f);

        Matrix4 view =
            Matrix4.CreateTranslation(-cam.position.X, -cam.position.Z, 0) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(-cam.YawDeg - 90f)) *
            Matrix4.CreateScale(1f, -1f, 1f);

        // Walls
        GL.BindVertexArray(_vaoQuad);
        GL.Uniform3(locColor, new Vector3(1, 1, 1));

        foreach (Matrix4 wm in _walls)
        {
            Vector3 p = wm.ExtractTranslation();
            Matrix4 mdl = Matrix4.CreateScale(2, 2, 1) *
                          Matrix4.CreateTranslation(p.X - 1, p.Z - 1, 0);

            Matrix4 mvp = mdl * view * proj;
            GL.UniformMatrix4(locMvp, false, ref mvp);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 4);
        }

        // Player Arrow
        GL.BindVertexArray(_vaoArrow);
        GL.Uniform3(locColor, new Vector3(0, 1, 0));

        Matrix4 mvpArrow = proj;
        GL.UniformMatrix4(locMvp, false, ref mvpArrow);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        // Restore viewport
        GL.Viewport(0, 0, winW, winH);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(_vaoQuad);
        GL.DeleteVertexArray(_vaoArrow);
        _shader.Dispose();
    }
}
