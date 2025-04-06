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
        public float screenWidth;
        public float screenHeight;

        // Pozice kamery
        public Vector3 position;

        private Vector3 up = Vector3.UnitY;
        private Vector3 front = -Vector3.UnitZ;
        private Vector3 right = Vector3.UnitX;

        private float pitch;
        private float yaw = -90.0f;

        private bool firstMove = true;
        public Vector2 lastPos;

        private ParsedMap _map;

        // Poloměr hráče pro kolize
        private float playerRadius = 0.3f;

        // Parametry pohybu
        private float movementSpeed;
        private float mouseSensitivity;
        private float lightHeight;
        private float angleOfDepression;

        private float lightCutoffDeg;
        private float lightRange;

        public float LightCutoffDeg => lightCutoffDeg;
        public float LightRange => lightRange;

        /// <summary>
        /// Konstruktor kamery s rozšířenými parametry pro světlo.
        /// </summary>
        public Camera(
            float width,
            float height,
            Vector3 startPosition,
            ParsedMap map,
            float movementSpeed,
            float mouseSensitivity,
            float lightHeight,
            float angleOfDepression,
            float lightCutoffDeg,    
            float lightRange
        )
        {
            screenWidth = width;
            screenHeight = height;
            position = startPosition;
            _map = map;

            this.movementSpeed = movementSpeed;
            this.mouseSensitivity = mouseSensitivity;
            this.lightHeight = lightHeight;
            this.angleOfDepression = angleOfDepression;
            this.lightCutoffDeg = lightCutoffDeg; 
            this.lightRange = lightRange;         
        }

        /// <summary>
        /// Aktualizace kamery (pohyb, myš).
        /// </summary>
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }

        /// <summary>
        /// Vrací view matici (pohled kamery).
        /// </summary>
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }

        /// <summary>
        /// Vrací projekční matici (45° FOV).
        /// </summary>
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                screenWidth / screenHeight,
                0.1f,
                100.0f);
        }

        /// <summary>
        /// Zpracování vstupu (W, A, S, D) a pohybu kamery s kolizemi.
        /// </summary>
        private void InputController(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            Vector2 inputDir = Vector2.Zero;
            if (input.IsKeyDown(GLKeys.W)) inputDir.Y += 1f;
            if (input.IsKeyDown(GLKeys.S)) inputDir.Y -= 1f;
            if (input.IsKeyDown(GLKeys.D)) inputDir.X += 1f;
            if (input.IsKeyDown(GLKeys.A)) inputDir.X -= 1f;

            if (inputDir.LengthSquared > 0.0001f)
                inputDir = inputDir.Normalized();

            Vector3 horizFront = new Vector3(front.X, 0f, front.Z).Normalized();
            Vector3 horizRight = Vector3.Normalize(Vector3.Cross(horizFront, Vector3.UnitY));

            Vector3 move = (horizFront * inputDir.Y + horizRight * inputDir.X)
                         * (movementSpeed * (float)e.Time);

            AttemptSlideMove(move);

            // Pohyb myší
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

                yaw += deltaX * mouseSensitivity * (float)e.Time;
                pitch -= deltaY * mouseSensitivity * (float)e.Time;
            }

            UpdateVectors();
        }

        /// <summary>
        /// Posuv s možností sklouznutí po zdi.
        /// </summary>
        private void AttemptSlideMove(Vector3 move)
        {
            Vector3 desiredPos = position + move;

            if (CanWalkAt(new Vector2(desiredPos.X, desiredPos.Z)))
            {
                position = desiredPos;
                return;
            }

            Vector3 tempPos = position;

            Vector3 moveX = new Vector3(desiredPos.X, position.Y, position.Z);
            if (CanWalkAt(new Vector2(moveX.X, moveX.Z)))
            {
                tempPos.X = moveX.X;
            }

            Vector3 moveZ = new Vector3(tempPos.X, position.Y, desiredPos.Z);
            if (CanWalkAt(new Vector2(moveZ.X, moveZ.Z)))
            {
                tempPos.Z = moveZ.Z;
            }

            position = tempPos;
        }

        /// <summary>
        /// Kontrola kolizí s mapou a zdmi.
        /// </summary>
        private bool CanWalkAt(Vector2 newPos2D)
        {
            int col = (int)MathF.Floor(newPos2D.X / 2f);
            int row = (int)MathF.Floor(newPos2D.Y / 2f);

            if (col < 0 || col >= _map.Columns || row < 0 || row >= _map.Rows)
            {
                return false;
            }

            for (int r = 0; r < _map.Rows; r++)
            {
                for (int c = 0; c < _map.Columns; c++)
                {
                    if (_map.Cells[r, c] == CellType.Wall)
                    {
                        float left = c * 2;
                        float right = c * 2 + 2;
                        float bottom = r * 2;
                        float top = r * 2 + 2;

                        left -= playerRadius;
                        right += playerRadius;
                        bottom -= playerRadius;
                        top += playerRadius;

                        if (newPos2D.X >= left && newPos2D.X <= right &&
                            newPos2D.Y >= bottom && newPos2D.Y <= top)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Aktualizuje směrové vektory podle pitch/yaw.
        /// </summary>
        private void UpdateVectors()
        {
            // Zabraňme extrémním úhlům (±90°), aby se kamera neotočila vzhůru nohama
            if (pitch > 89.0f) pitch = 89.0f;
            if (pitch < -89.0f) pitch = -89.0f;

            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch))
                    * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch))
                    * MathF.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        /// <summary>
        /// Vrací pozici a směr svítilny (se sklonem dolů).
        /// </summary>
        public (Vector3 position, Vector3 direction) GetFlashlightParams()
        {
            // Umístění baterky = XZ z kamery, Y = lightHeight
            Vector3 flashlightPos = new Vector3(position.X, lightHeight, position.Z);

            // Použijeme menší pitch, abychom se dívali trochu dolů (angleOfDepression)
            float pitchFlashlight = pitch - angleOfDepression;
            float yawFlashlight = yaw;

            Vector3 dir;
            dir.X = MathF.Cos(MathHelper.DegreesToRadians(pitchFlashlight))
                  * MathF.Cos(MathHelper.DegreesToRadians(yawFlashlight));
            dir.Y = MathF.Sin(MathHelper.DegreesToRadians(pitchFlashlight));
            dir.Z = MathF.Cos(MathHelper.DegreesToRadians(pitchFlashlight))
                  * MathF.Sin(MathHelper.DegreesToRadians(yawFlashlight));
            dir = Vector3.Normalize(dir);

            return (flashlightPos, dir);
        }
    }
}
