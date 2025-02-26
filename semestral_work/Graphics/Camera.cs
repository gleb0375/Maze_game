using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using GLKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace semestral_work.Graphics
{
    internal class Camera
    {
        // Public, чтобы их можно было менять извне (например, при OnResize)
        public float SCREENWIDTH;
        public float SCREENHEIGHT;

        // Скорость бега и чувствительность мыши
        private float SPEED = 1.4f;          // 1.4 м/с по заданию
        private float SENSITIVITY = 30f;

        // Текущая позиция камеры в мире
        public Vector3 position;

        // Ориентация
        private Vector3 up = Vector3.UnitY;
        private Vector3 front = -Vector3.UnitZ;
        private Vector3 right = Vector3.UnitX;

        // Угол для камеры
        private float pitch;
        private float yaw = -90.0f; // Смотрим вперёд вдоль -Z

        // Для мыши
        private bool firstMove = true;
        public Vector2 lastPos;


        public Camera(float width, float height, Vector3 position)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            this.position = position;
        }

        // Матрица вида
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }

        // Матрица проекции
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                SCREENWIDTH / SCREENHEIGHT,
                0.1f,
                100.0f);
        }

        // Пересчитываем front/right/up на основе pitch/yaw
        private void UpdateVectors()
        {
            // Ограничиваем pitch, чтобы не переворачиваться
            if (pitch > 89.0f)
                pitch = 89.0f;
            if (pitch < -89.0f)
                pitch = -89.0f;

            // Расчитываем новый front
            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);

            // Правый вектор: перпендикулярен front и Y (чтобы вращаться вокруг вертикали)
            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));

            // up — перпендикулярен right и front
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        // Обрабатываем нажатия клавиш и движение мыши
        private void InputController(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            // 1) Сначала найдём, куда игрок «хочет» двигаться в плоскости 2D.
            // X = направление влево/вправо, Y = направление вперёд/назад
            Vector2 inputDir = Vector2.Zero;

            if (input.IsKeyDown(GLKeys.W))
            {
                inputDir.Y += 1f;
            }
            if (input.IsKeyDown(GLKeys.S))
            {
                inputDir.Y -= 1f;
            }
            if (input.IsKeyDown(GLKeys.D))
            {
                inputDir.X += 1f;
            }
            if (input.IsKeyDown(GLKeys.A))
            {
                inputDir.X -= 1f;
            }

            // Если есть движение, нормализуем вектор, чтобы длина стала 1
            if (inputDir.LengthSquared > 0.0001f)
            {
                inputDir.Normalize();
            }

            // 2) horizontalFront и horizontalRight оставляем как раньше,
            //    но теперь мы будем умножать их на X/Y из inputDir.
            var horizontalFront = new Vector3(front.X, 0f, front.Z).Normalized();
            var horizontalRight = Vector3.Normalize(Vector3.Cross(horizontalFront, Vector3.UnitY));

            // Составляем итоговый 3D-вектор движения
            Vector3 move = horizontalFront * inputDir.Y + horizontalRight * inputDir.X;

            // 3) Умножаем на скорость и время
            float delta = SPEED * (float)e.Time;
            position += move * delta;

            // 4) Обрабатываем мышь
            if (firstMove)
            {
                lastPos = new Vector2(mouse.X, mouse.Y);
                firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - lastPos.X;
                var deltaY = mouse.Y - lastPos.Y;
                lastPos = new Vector2(mouse.X, mouse.Y);

                yaw += deltaX * SENSITIVITY * (float)e.Time;
                pitch -= deltaY * SENSITIVITY * (float)e.Time;
            }

            // 5) Обновляем ориентацию
            UpdateVectors();

            // 6) Фиксируем высоту камеры
            position.Y = 1.7f;
        }

        // Вызывается из OnUpdateFrame
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }
    }
}
