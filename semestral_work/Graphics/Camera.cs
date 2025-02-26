using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using semestral_work.Map;
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
        private float SPEED = 2.5f;          // 1.4 м/с по заданию
        private float SENSITIVITY = 20f;

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

        private ParsedMap _map;

        private float playerRadius = 0.3f; // Радиус игрока

        public Camera(float width, float height, Vector3 position, ParsedMap map)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            this.position = position;
            _map = map;
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
            Vector2 inputDir = Vector2.Zero;

            if (input.IsKeyDown(GLKeys.W)) inputDir.Y += 1f;
            if (input.IsKeyDown(GLKeys.S)) inputDir.Y -= 1f;
            if (input.IsKeyDown(GLKeys.D)) inputDir.X += 1f;
            if (input.IsKeyDown(GLKeys.A)) inputDir.X -= 1f;

            if (inputDir.LengthSquared > 0.0001f)
                inputDir = Vector2.Normalize(inputDir);

            var horizontalFront = new Vector3(front.X, 0f, front.Z).Normalized();
            var horizontalRight = Vector3.Normalize(Vector3.Cross(horizontalFront, Vector3.UnitY));

            // Итоговый 3D-вектор движения
            Vector3 move = (horizontalFront * inputDir.Y + horizontalRight * inputDir.X) * (SPEED * (float)e.Time);

            // Пытаемся переместиться 
            Vector3 newPosition = position + move;
            TryMove(newPosition);

            // Мышь — всё без изменений
            if (firstMove)
            {
                lastPos = new Vector2(mouse.X, mouse.Y);
                firstMove = false;
            }
            else
            {
                float deltaX = mouse.X - lastPos.X;
                float deltaY = mouse.Y - lastPos.Y;
                lastPos = new Vector2(mouse.X, mouse.Y);

                yaw += deltaX * SENSITIVITY * (float)e.Time;
                pitch -= deltaY * SENSITIVITY * (float)e.Time;
            }

            UpdateVectors();
            position.Y = 1.7f; // фиксируем высоту
        }


        private bool CanWalkAt(Vector2 newPos2D)
        {
            // 1) Сначала проверяем, не выходим ли за границы карты
            int col = (int)MathF.Floor(newPos2D.X / 2f);
            int row = (int)MathF.Floor(newPos2D.Y / 2f);

            // Если мы выходим за пределы массива:
            if (col < 0 || col >= _map.Columns || row < 0 || row >= _map.Rows)
            {
                // Выходить за карту нельзя
                return false;
            }

            // 2) Проверяем, не «врезались» ли мы в стены рядом
            //    Самый простой способ — пробежаться по всем стенам (или вокруг ближайшей клетки).
            //    Здесь для демонстрации — обход всех, но можно оптимизировать.

            for (int r = 0; r < _map.Rows; r++)
            {
                for (int c = 0; c < _map.Columns; c++)
                {
                    if (_map.Cells[r, c] == CellType.Wall)
                    {
                        // Координаты «обычной» стены:
                        float left = c * 2;
                        float right = c * 2 + 2;
                        float top = r * 2 + 2;
                        float bottom = r * 2;

                        // Расширяем на радиус игрока
                        left -= playerRadius;
                        right += playerRadius;
                        bottom -= playerRadius;
                        top += playerRadius;

                        // Если (newPos2D.X, newPos2D.Y) попадает в [left..right] x [bottom..top], 
                        // значит мы пересекаем стену
                        if (newPos2D.X >= left && newPos2D.X <= right &&
                            newPos2D.Y >= bottom && newPos2D.Y <= top)
                        {
                            return false;
                        }
                    }
                }
            }

            // Если до сюда дошли, значит с коллизией всё в порядке
            return true;
        }

        private void TryMove(Vector3 newPos)
        {
            // Мы учитываем только X,Z; Y фиксирована = 1.7.
            Vector2 newPos2D = new Vector2(newPos.X, newPos.Z);

            if (CanWalkAt(newPos2D))
            {
                // Разрешаем
                position = newPos;
            }
            else
            {
                // Запрещаем — ничего не делаем или можно частично откатиться
            }
        }


        // Вызывается из OnUpdateFrame
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }
    }
}
