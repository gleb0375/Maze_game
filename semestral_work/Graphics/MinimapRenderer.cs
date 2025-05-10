using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using semestral_work.Graphics;
using semestral_work.Map;

internal sealed class MinimapRenderer : IDisposable
{
    private const int MINI_SIZE_PX = 200;   // viewport size
    private const float VIEW_RADIUS = 20;   // метры вокруг игрока
    private const float ARROW_SIZE = 1.8f;  // размер индикатора игрока

    private readonly ParsedMap _map;
    private readonly List<Matrix4> _walls;
    private readonly Shader _shader;
    private readonly int _vaoQuad;
    private readonly int _vaoArrow;

    public MinimapRenderer(ParsedMap map, List<Matrix4> wallMatrices)
    {
        _map = map;
        _walls = wallMatrices;

        const string vs = @"#version 330 core
layout(location = 0) in vec2 aPos;
uniform mat4 uMVP;
void main(){ gl_Position = uMVP * vec4(aPos,0.0,1.0); }";

        const string fs = @"#version 330 core
uniform vec3 uColor;
out vec4 FragColor;
void main(){ FragColor = vec4(uColor,1.0); }";

        _shader = new Shader(vs, fs);
        _vaoQuad = CreateVao(new float[] { 0, 0, 1, 0, 1, 1, 0, 1 });

        // равнобедренный треугольник, «нос» смотрит вверх
        float b = ARROW_SIZE * 0.6f;   // половина основания
        float[] arrow =
        {
             0f,  ARROW_SIZE,   // apex
            -b , -b ,          // left base
             b , -b            // right base
        };
        _vaoArrow = CreateVao(arrow);
    }

    private static int CreateVao(float[] data)
    {
        int vao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float),
                      data, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float,
                               false, 0, 0);
        GL.BindVertexArray(0);
        return vao;
    }

    /// <summary>Draw after the 3-D scene.</summary>
    public void Render(Camera cam, int winW, int winH)
    {
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);

        // ­­­­­­­­­--- отдельный viewport в левом-верхнем углу
        GL.Viewport(0, winH - MINI_SIZE_PX, MINI_SIZE_PX, MINI_SIZE_PX);

        _shader.Use();
        int locMvp = GL.GetUniformLocation(_shader.Handle, "uMVP");
        int locColor = GL.GetUniformLocation(_shader.Handle, "uColor");

        Matrix4 proj = Matrix4.CreateOrthographicOffCenter
                       (-VIEW_RADIUS, VIEW_RADIUS,
                        -VIEW_RADIUS, VIEW_RADIUS,
                        -1f, 1f);

        // перенос → вращение (yaw+90) → отражение по Y
        Matrix4 view =
            Matrix4.CreateTranslation(-cam.position.X, -cam.position.Z, 0) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(-cam.YawDeg - 90f)) *
            Matrix4.CreateScale(1f, -1f, 1f);

        // ---------- walls ----------
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

        // ---------- player arrow (в центре, апекс = направление взгляда) ----------
        GL.BindVertexArray(_vaoArrow);
        GL.Uniform3(locColor, new Vector3(0, 1, 0));

        Matrix4 mvpArrow = proj;                 // view уже помещает игрока в (0,0)
        GL.UniformMatrix4(locMvp, false, ref mvpArrow);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        // ­­­­­­­­­--- вернуть состояние
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
