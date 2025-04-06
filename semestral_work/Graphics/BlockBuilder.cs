using OpenTK.Graphics.OpenGL4;

namespace semestral_work.Graphics
{
    internal static class BlockBuilder
    {
        /// <summary>
        /// Vytvoří VAO pro blok o velikosti 2 x 3 x 2 (šířka x výška x hloubka).
        /// Vrací identifikátor VAO.
        /// </summary>
        public static int CreateBlockVAO()
        {
            float halfWidth = 1.0f;  
            float halfHeight = 1.5f;  
            float halfDepth = 1.0f;   

            float[] vertices =
            {
                -halfWidth,  -halfHeight,  halfDepth,   0.0f, 0.0f,  
                 halfWidth,  -halfHeight,  halfDepth,   1.0f, 0.0f,  
                 halfWidth,   halfHeight,  halfDepth,   1.0f, 1.0f,  
                -halfWidth,   halfHeight,  halfDepth,   0.0f, 1.0f,  

                -halfWidth,  -halfHeight, -halfDepth,   1.0f, 0.0f,
                 halfWidth,  -halfHeight, -halfDepth,   0.0f, 0.0f,
                 halfWidth,   halfHeight, -halfDepth,   0.0f, 1.0f,
                -halfWidth,   halfHeight, -halfDepth,   1.0f, 1.0f
            };

            uint[] indices =
            {
                0, 1, 2,
                2, 3, 0,

                5, 4, 7,
                7, 6, 5,

                4, 0, 3,
                3, 7, 4,

                1, 5, 6,
                6, 2, 1,

                4, 5, 1,
                1, 0, 4,

                3, 2, 6,
                6, 7, 3
            };

            int vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);

            // VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            GL.BindVertexArray(0);

            return vao;
        }
    }
}
