using OpenTK.Mathematics;
using semestral_work.Config;
using semestral_work.Graphics;
using semestral_work.Map;
using System;
using System.Collections.Generic;

namespace semestral_work.Graphics
{
    /// <summary>Создаёт трансформации для всех клеток T–Z и просто рисует яблоко.</summary>
    internal sealed class CollectableManager : IDisposable
    {
        private readonly AppleModel _apple;
        private readonly List<Matrix4> _transforms = new();

        public CollectableManager(ParsedMap map, Shader appleShader)
        {
            _apple = new AppleModel(AppConfig.GetAppleModelPath(), appleShader);

            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    if (map.Cells[r, c] != CellType.Collectable) continue;

                    float x = c * 2f + 1f;
                    float z = r * 2f + 1f;

                    // маленькое яблоко (25 см) на высоте 0.3 м
                    Matrix4 tr =
                        Matrix4.CreateScale(0.005f) *
                        Matrix4.CreateTranslation(x, 0.30f, z);

                    _transforms.Add(tr);
                }
        }

        public void Render(Matrix4 view, Matrix4 proj)
        {
            foreach (var m in _transforms)
                _apple.Render(m, view, proj);
        }

        public void Dispose() => _apple.Dispose();
    }
}
