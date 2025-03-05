using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using semestral_work.Config;
using semestral_work.Map;
using System;
using GLKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace semestral_work.Graphics
{
    internal class Camera
    {
        // Screen size (for projection)
        public float screenWidth;
        public float screenHeight;

        // Current camera position in world space
        public Vector3 position;

        // Orientation vectors
        private Vector3 up = Vector3.UnitY;
        private Vector3 front = -Vector3.UnitZ;
        private Vector3 right = Vector3.UnitX;

        // Angles for pitch and yaw
        private float pitch;
        private float yaw = -90.0f; // Facing -Z by default

        // Mouse control
        private bool firstMove = true;
        public Vector2 lastPos;

        // Map for collision checks
        private ParsedMap _map;

        // Player radius for bounding-box collision
        private float playerRadius = 0.3f;

        public Camera(float width, float height, Vector3 position, ParsedMap map)
        {
            screenWidth = width;
            screenHeight = height;
            this.position = position;
            _map = map;
        }

        /// <summary>
        /// Returns the view matrix (camera orientation) based on current position and front/up vectors.
        /// </summary>
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }

        /// <summary>
        /// Returns the projection matrix with a fixed 45° FOV, using the current screen width/height.
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
        /// Updates front, right, and up vectors based on pitch and yaw angles.
        /// Ensures pitch stays in [-89°, 89°] to avoid flipping.
        /// </summary>
        private void UpdateVectors()
        {
            if (pitch > 89.0f)
                pitch = 89.0f;
            if (pitch < -89.0f)
                pitch = -89.0f;

            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        /// <summary>
        /// Processes keyboard input for movement, mouse input for orientation, and applies collisions.
        /// </summary>
        private void InputController(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            // Retrieve speed & mouse sensitivity from config
            float speed = AppConfig.GetMovementSpeed();
            float sensitivity = AppConfig.GetMouseSensivity();

            // Determine movement direction based on W, A, S, D
            Vector2 inputDir = Vector2.Zero;
            if (input.IsKeyDown(GLKeys.W)) inputDir.Y += 1f;
            if (input.IsKeyDown(GLKeys.S)) inputDir.Y -= 1f;
            if (input.IsKeyDown(GLKeys.D)) inputDir.X += 1f;
            if (input.IsKeyDown(GLKeys.A)) inputDir.X -= 1f;

            if (inputDir.LengthSquared > 0.0001f)
                inputDir = inputDir.Normalized();

            // Project the front vector onto the XZ plane so we don't fly up or down
            Vector3 horizontalFront = new Vector3(front.X, 0f, front.Z).Normalized();
            Vector3 horizontalRight = Vector3.Normalize(Vector3.Cross(horizontalFront, Vector3.UnitY));

            // Movement vector = direction * speed * deltaTime
            Vector3 move = (horizontalFront * inputDir.Y + horizontalRight * inputDir.X) * (speed * (float)e.Time);

            // Attempt to move and handle collision with "sliding" logic
            AttemptSlideMove(move);

            // Mouse movement for yaw/pitch
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

                yaw += deltaX * sensitivity * (float)e.Time;
                pitch -= deltaY * sensitivity * (float)e.Time;
            }

            UpdateVectors();
        }

        /// <summary>
        /// Performs the attempt to move with "sliding" along walls.
        /// If the full move is blocked, tries partial moves along X or Z to allow sliding.
        /// </summary>
        private void AttemptSlideMove(Vector3 move)
        {
            Vector3 desiredPos = position + move;

            // 1) Check if full movement is valid
            if (CanWalkAt(new Vector2(desiredPos.X, desiredPos.Z)))
            {
                position = desiredPos;
                return;
            }

            // 2) Full move blocked, so we try partial: X or Z axis separately
            //    We'll store the final position in a temporary
            Vector3 tempPos = position;

            // 2a) Try moving along X only
            Vector3 moveX = new Vector3(desiredPos.X, position.Y, position.Z);
            if (CanWalkAt(new Vector2(moveX.X, moveX.Z)))
            {
                tempPos.X = moveX.X;
            }

            // 2b) Then try moving along Z only (with updated tempPos.X)
            Vector3 moveZ = new Vector3(tempPos.X, position.Y, desiredPos.Z);
            if (CanWalkAt(new Vector2(moveZ.X, moveZ.Z)))
            {
                tempPos.Z = moveZ.Z;
            }

            // Assign final position after partial checks
            position = tempPos;
        }

        /// <summary>
        /// Checks if the 2D position (x,z) is free of collisions with the map walls.
        /// Also checks that it's inside map boundaries.
        /// </summary>
        private bool CanWalkAt(Vector2 newPos2D)
        {
            // First, check boundaries
            int col = (int)MathF.Floor(newPos2D.X / 2f);
            int row = (int)MathF.Floor(newPos2D.Y / 2f);

            if (col < 0 || col >= _map.Columns || row < 0 || row >= _map.Rows)
            {
                return false;
            }

            // Check collisions with walls
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

                        // Expand by playerRadius
                        left -= playerRadius;
                        right += playerRadius;
                        bottom -= playerRadius;
                        top += playerRadius;

                        // Check bounding box
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
        /// Returns a tuple (position, direction) for a flashlight held at a certain height
        /// and depressed angle relative to the player's pitch.
        /// </summary>
        public (Vector3 position, Vector3 direction) GetFlashlightParams()
        {
            float lightHeight = AppConfig.GetLightHeight();
            Vector3 flashlightPos = new Vector3(position.X, lightHeight, position.Z);

            float angleOfDepression = AppConfig.GetAngleOfDepression();
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

        /// <summary>
        /// Updates camera by processing input and collisions.
        /// </summary>
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }
    }
}
