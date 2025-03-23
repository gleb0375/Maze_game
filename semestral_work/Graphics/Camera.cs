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
        // Screen dimensions for projection
        public float screenWidth;
        public float screenHeight;

        // Camera position
        public Vector3 position;

        // Orientation
        private Vector3 up = Vector3.UnitY;
        private Vector3 front = -Vector3.UnitZ;
        private Vector3 right = Vector3.UnitX;

        // Angles
        private float pitch;
        private float yaw = -90.0f;

        // Mouse
        private bool firstMove = true;
        public Vector2 lastPos;

        // Map for collisions
        private ParsedMap _map;

        // Player collision radius
        private float playerRadius = 0.3f;

        // Configuration values passed via constructor (once)
        private float movementSpeed;
        private float mouseSensitivity;
        private float lightHeight;
        private float angleOfDepression;

        public Camera(
            float width,
            float height,
            Vector3 startPosition,
            ParsedMap map,
            float movementSpeed,
            float mouseSensitivity,
            float lightHeight,
            float angleOfDepression)
        {
            screenWidth = width;
            screenHeight = height;
            position = startPosition;
            _map = map;

            // Save config values to private fields
            this.movementSpeed = movementSpeed;
            this.mouseSensitivity = mouseSensitivity;
            this.lightHeight = lightHeight;
            this.angleOfDepression = angleOfDepression;
        }

        /// <summary>
        /// Updates the camera each frame.
        /// </summary>
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }

        /// <summary>
        /// Returns the view matrix.
        /// </summary>
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }

        /// <summary>
        /// Returns the projection matrix for a 45° FOV using current screen size.
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
        /// Processes keyboard/mouse input and applies collisions (with sliding).
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

            // Movement in XZ plane
            Vector3 horizFront = new Vector3(front.X, 0f, front.Z).Normalized();
            Vector3 horizRight = Vector3.Normalize(Vector3.Cross(horizFront, Vector3.UnitY));

            Vector3 move = (horizFront * inputDir.Y + horizRight * inputDir.X)
                         * (movementSpeed * (float)e.Time);

            AttemptSlideMove(move);

            // Mouse
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
        /// Allows sliding along walls: if full movement is blocked,
        /// attempts partial X or Z.
        /// </summary>
        private void AttemptSlideMove(Vector3 move)
        {
            Vector3 desiredPos = position + move;

            // Check full
            if (CanWalkAt(new Vector2(desiredPos.X, desiredPos.Z)))
            {
                position = desiredPos;
                return;
            }

            // Partial
            Vector3 tempPos = position;

            // Check X only
            Vector3 moveX = new Vector3(desiredPos.X, position.Y, position.Z);
            if (CanWalkAt(new Vector2(moveX.X, moveX.Z)))
            {
                tempPos.X = moveX.X;
            }

            // Check Z only
            Vector3 moveZ = new Vector3(tempPos.X, position.Y, desiredPos.Z);
            if (CanWalkAt(new Vector2(moveZ.X, moveZ.Z)))
            {
                tempPos.Z = moveZ.Z;
            }

            position = tempPos;
        }

        /// <summary>
        /// Checks collisions in the map (including walls and boundaries).
        /// </summary>
        private bool CanWalkAt(Vector2 newPos2D)
        {
            int col = (int)MathF.Floor(newPos2D.X / 2f);
            int row = (int)MathF.Floor(newPos2D.Y / 2f);

            // Check map boundaries
            if (col < 0 || col >= _map.Columns || row < 0 || row >= _map.Rows)
            {
                return false;
            }

            // Check collisions
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
        /// Updates orientation vectors (front, right, up) based on pitch/yaw.
        /// </summary>
        private void UpdateVectors()
        {
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
        /// Returns (position, direction) for the player's flashlight.
        /// Depresses pitch by angleOfDepression to tilt downward.
        /// </summary>
        public (Vector3 position, Vector3 direction) GetFlashlightParams()
        {
            // The flashlight is at "lightHeight" above the player's X,Z
            Vector3 flashlightPos = new Vector3(position.X, lightHeight, position.Z);

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
