using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using semestral_work.Map;
using System;
using GLKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace semestral_work.Graphics
{
    /// <summary>
    /// Kamera hráče s podporou jemného setrvačného dosmyku a pohupování (head‑bob)
    /// při chůzi. Logická pozice <see cref="position"/> se používá pro kolize,
    /// zatímco <see cref="_renderPosition"/> je vyhlazená/animovaná pozice, která
    /// se předává do view‑matice (LookAt).
    /// </summary>
    internal class Camera
    {
        public float screenWidth;
        public float screenHeight;

        // Logická (fyzická) pozice hráče – pro kolize a logiku.
        public Vector3 position;

        // Vyhlazená pozice kamery použitá pro renderování.
        private Vector3 _renderPosition;

        private Vector3 up = Vector3.UnitY;
        private Vector3 front = -Vector3.UnitZ;
        private Vector3 right = Vector3.UnitX;

        private float pitch;
        private float yaw = -90.0f;

        private bool firstMove = true;
        public Vector2 lastPos;

        private readonly ParsedMap _map;

        // Poloměr hráče pro kolize.
        private readonly float playerRadius = 0.3f;

        // Parametry pohybu.
        private readonly float movementSpeed;
        private readonly float mouseSensitivity;
        private readonly float lightHeight;
        private readonly float angleOfDepression;

        private readonly float lightCutoffDeg;
        private readonly float lightRange;

        // --- Dynamika / inertia & head‑bob ---
        private bool _isMoving;
        private float _bobTimer;
        private readonly float _bobSpeed = 8.0f;   // Hz
        private readonly float _bobAmount = 0.03f; // metry
        private readonly float _smoothTime = 0.25f; // s (e^-t/τ)
        private readonly float _baseHeight;        // výška očí od podlahy

        public float LightCutoffDeg => lightCutoffDeg;
        public float LightRange => lightRange;
        public float YawDeg => yaw;

        /// <summary>
        /// Konstruktor kamery.
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
            float lightRange)
        {
            screenWidth = width;
            screenHeight = height;
            position = startPosition;
            _renderPosition = startPosition;
            _baseHeight = startPosition.Y;

            _map = map;
            this.movementSpeed = movementSpeed;
            this.mouseSensitivity = mouseSensitivity;
            this.lightHeight = lightHeight;
            this.angleOfDepression = angleOfDepression;
            this.lightCutoffDeg = lightCutoffDeg;
            this.lightRange = lightRange;
        }

        /// <summary>
        /// Hlavní update – zpracuje vstup, vyhodnotí kolize a aplikuje pohupování & setrvačnost.
        /// </summary>
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);

            float dt = (float)e.Time;

            // --- Vyhlazení (setrvačnost) logické → render pozice ---
            float lerpFactor = 1f - MathF.Exp(-dt / _smoothTime);
            _renderPosition = Vector3.Lerp(_renderPosition, position, lerpFactor);

            // --- Head‑bob ---
            if (_isMoving)
            {
                _bobTimer += dt * _bobSpeed;
            }
            else
            {
                _bobTimer = 0f; // reset, ať animace vždy začíná z nulové fáze
            }

            float yOffset = MathF.Sin(_bobTimer) * _bobAmount * (_isMoving ? 1f : 0f);
            _renderPosition.Y = _baseHeight + yOffset;
        }

        /// <summary>
        /// View‑matice používá vyhlazenou a animovanou pozici kamery.
        /// </summary>
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(_renderPosition, _renderPosition + front, up);
        }

        /// <summary>
        /// Projekční matice (45° FOV).
        /// </summary>
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                screenWidth / screenHeight,
                0.1f,
                100.0f);
        }

        // =====================================================================
        //                           Privátní metody
        // =====================================================================

        /// <summary>
        /// Zpracuje pohyb hráče (WASD) + myš a provede kolize.
        /// </summary>
        private void InputController(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            Vector2 inputDir = Vector2.Zero;
            if (input.IsKeyDown(GLKeys.W)) inputDir.Y += 1f;
            if (input.IsKeyDown(GLKeys.S)) inputDir.Y -= 1f;
            if (input.IsKeyDown(GLKeys.D)) inputDir.X += 1f;
            if (input.IsKeyDown(GLKeys.A)) inputDir.X -= 1f;

            _isMoving = inputDir.LengthSquared > 0.0001f;
            if (_isMoving)
                inputDir = inputDir.Normalized();

            Vector3 horizFront = new Vector3(front.X, 0f, front.Z).Normalized();
            Vector3 horizRight = Vector3.Normalize(Vector3.Cross(horizFront, Vector3.UnitY));
            Vector3 move = (horizFront * inputDir.Y + horizRight * inputDir.X) * (movementSpeed * (float)e.Time);

            AttemptSlideMove(move);

            // --- Myš ---
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
        /// Posuv s možností sklouznutí po stěnách.
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
        /// Kolizní test s mapou.
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
        /// Přepočítá směrové vektory (front/right/up) z pitch & yaw.
        /// </summary>
        private void UpdateVectors()
        {
            // Omez extrémní úhly, ať se kamera nepřevrátí.
            if (pitch > 89.0f) pitch = 89.0f;
            if (pitch < -89.0f) pitch = -89.0f;

            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        /// <summary>
        /// Vrací pozici a směr svítilny (se sklonem dolů).
        /// Používá XZ souřadnice renderované kamery pro konzistenci s head‑bobem.
        /// </summary>
        public (Vector3 position, Vector3 direction) GetFlashlightParams()
        {
            Vector3 flashlightPos = new Vector3(_renderPosition.X, lightHeight, _renderPosition.Z);

            float pitchFlashlight = pitch - angleOfDepression;
            float yawFlashlight = yaw;

            Vector3 dir;
            dir.X = MathF.Cos(MathHelper.DegreesToRadians(pitchFlashlight)) * MathF.Cos(MathHelper.DegreesToRadians(yawFlashlight));
            dir.Y = MathF.Sin(MathHelper.DegreesToRadians(pitchFlashlight));
            dir.Z = MathF.Cos(MathHelper.DegreesToRadians(pitchFlashlight)) * MathF.Sin(MathHelper.DegreesToRadians(yawFlashlight));
            dir = Vector3.Normalize(dir);

            return (flashlightPos, dir);
        }
    }
}
