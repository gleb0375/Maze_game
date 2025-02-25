using OpenTK.Graphics.OpenGL4;

namespace semestral_work.Graphics
{
    internal static class BlockBuilder
    {
        /// <summary>
        /// Создаёт VAO для блока размерами 2 x 3 x 2 (ширина x высота x глубина).
        /// Возвращает идентификатор VAO.
        /// </summary>
        public static int CreateBlockVAO()
        {
            float halfWidth = 1.0f;   // 2 / 2
            float halfHeight = 1.5f;  // 3 / 2
            float halfDepth = 1.0f;   // 2 / 2

            // Массив вершин: (x, y, z, u, v) 
            // Примерно назначим UV на каждую грань 0..1
            float[] vertices =
            {
                // Передняя грань (z = +halfDepth)
                -halfWidth,  -halfHeight,  halfDepth,   0.0f, 0.0f,  // нижняя левая
                 halfWidth,  -halfHeight,  halfDepth,   1.0f, 0.0f,  // нижняя правая
                 halfWidth,   halfHeight,  halfDepth,   1.0f, 1.0f,  // верхняя правая
                -halfWidth,   halfHeight,  halfDepth,   0.0f, 1.0f,  // верхняя левая

                // Задняя грань (z = -halfDepth)
                -halfWidth,  -halfHeight, -halfDepth,   1.0f, 0.0f,
                 halfWidth,  -halfHeight, -halfDepth,   0.0f, 0.0f,
                 halfWidth,   halfHeight, -halfDepth,   0.0f, 1.0f,
                -halfWidth,   halfHeight, -halfDepth,   1.0f, 1.0f
            };

            uint[] indices =
            {
                // Передняя грань
                0, 1, 2,
                2, 3, 0,

                // Задняя грань
                5, 4, 7,
                7, 6, 5,

                // Левая грань
                4, 0, 3,
                3, 7, 4,

                // Правая грань
                1, 5, 6,
                6, 2, 1,

                // Нижняя грань
                4, 5, 1,
                1, 0, 4,

                // Верхняя грань
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

            // Настройка атрибутов
            // location 0 -> (x, y, z)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            // location 1 -> (u, v)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            GL.BindVertexArray(0);

            return vao;
        }
    }
}
