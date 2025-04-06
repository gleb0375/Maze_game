using OpenTK.Graphics.OpenGL4;

namespace semestral_work.Graphics
{
    internal static class PlaneBuilder
    {
        /// <summary>
        /// Vytvoří VAO pro podlahu o rozměrech (2 * sloupce) x (2 * řádky).
        /// Vrací dvojici (vao, indexCount), kde 'vao' je identifikátor objektu a 'indexCount' počet indexů.
        /// </summary>
        public static (int vao, int indexCount) CreatePlaneVAO(int rows, int columns)
        {
            float width = 2 * columns;
            float depth = 2 * rows;

            float[] vertices = new float[]
            {
                0.0f,    0.0f, 0.0f,   0.0f,      0.0f,      
                width,   0.0f, 0.0f,   columns,   0.0f,      
                width,   0.0f, depth,  columns,   rows,      
                0.0f,    0.0f, depth,  0.0f,      rows       
            };

            uint[] indices = new uint[]
            {
                0, 1, 2,
                2, 3, 0
            };

            int vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            GL.BindVertexArray(0);

            return (vao, indices.Length);
        }
    }
}
