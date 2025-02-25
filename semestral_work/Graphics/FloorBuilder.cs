using OpenTK.Graphics.OpenGL4;

namespace semestral_work.Graphics
{
    internal static class FloorBuilder
    {
        /// <summary>
        /// Создаёт VAO для пола размером (2 * columns) x (2 * rows) в плоскости Y=0.
        /// Возвращает кортеж (vao, indexCount) - индекс массива индексов, чтобы знать, сколько рисовать.
        /// </summary>
        public static (int vao, int indexCount) CreateFloorVAO(int rows, int columns)
        {
            float width = 2 * columns;
            float depth = 2 * rows;

            // Вершины (x, y, z, u, v). Четыре вершины.
            // Левый нижний (0,0,0), правый нижний (width,0,0)
            // Левый верхний (0,0,depth), правый верхний (width,0,depth)
            // Текстурные координаты: u = 0..columns, v = 0..rows
            float[] vertices = new float[]
            {
                //  x     y    z       u      v
                0.0f,    0.0f, 0.0f,   0.0f,      0.0f,          // bottom-left
                width,   0.0f, 0.0f,   columns,   0.0f,          // bottom-right
                width,   0.0f, depth,  columns,   rows,          // top-right
                0.0f,    0.0f, depth,  0.0f,      rows,          // top-left
            };

            // Два треугольника, образующие квадрат
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

            // Настройка атрибутов:
            // location=0 -> (x, y, z)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            // location=1 -> (u, v)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            GL.BindVertexArray(0);

            return (vao, indices.Length);
        }
    }
}
