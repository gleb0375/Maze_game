using OpenTK.Graphics.OpenGL4;

namespace semestral_work.Graphics
{
    internal static class FloorBuilder
    {
        /// <summary>
        /// Creates a VAO for the floor with dimensions (2 * columns) x (2 * rows) on the Y=0 plane.
        /// Returns a tuple (vao, indexCount) where 'vao' is the vertex array object handle
        /// and 'indexCount' is the number of indices to draw.
        /// </summary>
        public static (int vao, int indexCount) CreateFloorVAO(int rows, int columns)
        {
            float width = 2 * columns;
            float depth = 2 * rows;

            // Vertices (x, y, z, u, v) for four corners.
            // Bottom-left: (0, 0, 0), Bottom-right: (width, 0, 0)
            // Top-right: (width, 0, depth), Top-left: (0, 0, depth)
            // Texture coordinates: u from 0 to columns, v from 0 to rows.
            float[] vertices = new float[]
            {
                //    x       y    z       u         v
                0.0f,    0.0f, 0.0f,   0.0f,      0.0f,      // bottom-left
                width,   0.0f, 0.0f,   columns,   0.0f,      // bottom-right
                width,   0.0f, depth,  columns,   rows,      // top-right
                0.0f,    0.0f, depth,  0.0f,      rows       // top-left
            };

            // Indices for two triangles forming the floor quad.
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

            // Setup vertex attributes:
            // Location 0: position (x, y, z)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            // Location 1: texture coordinates (u, v)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            GL.BindVertexArray(0);

            return (vao, indices.Length);
        }
    }
}
