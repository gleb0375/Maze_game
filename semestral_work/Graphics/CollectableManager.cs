using OpenTK.Mathematics;
using semestral_work.Config;
using semestral_work.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace semestral_work.Graphics
{
    internal sealed class CollectableManager : IDisposable
    {
        private readonly AppleModel _apple;
        private readonly List<Item> _items = new();
        private float _time;

        private const float SCALE = 0.005f;
        private const float BOUNCE_AMPL = 0.08f;
        private const float BOUNCE_FREQ = 2.0f;
        private const float ROT_SPEED_DEG = 90f;

        private const float PICK_RADIUS = 0.9f;

        private struct Item
        {
            public Vector3 Pos;    
            public float Phase;    
            public bool Collected; 
        }

        public int TotalCount => _items.Count;
        public int CollectedCount => _items.Count(i => i.Collected);

        public CollectableManager(ParsedMap map, Shader appleShader)
        {
            _apple = new AppleModel(AppConfig.GetAppleModelPath(), appleShader);

            int seed = 0;
            for (int r = 0; r < map.Rows; r++)
            {
                for (int c = 0; c < map.Columns; c++)
                {
                    if (map.Cells[r, c] != CellType.Collectable) continue;

                    float x = c * 2f + 1f;
                    float z = r * 2f + 1f;

                    _items.Add(new Item
                    {
                        Pos = new Vector3(x, 0.30f, z),
                        Phase = seed * 0.5f,
                        Collected = false
                    });
                    seed++;
                }
            }
        }

        public void Update(float dt) => _time += dt;

        public void TryCollect(Vector3 playerPos)
        {
            Vector2 playerXZ = new Vector2(playerPos.X, playerPos.Z);

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Collected) continue;

                Vector2 itemXZ = new Vector2(_items[i].Pos.X, _items[i].Pos.Z);

                if ((playerXZ - itemXZ).Length < PICK_RADIUS)
                {
                    var item = _items[i];
                    item.Collected = true;
                    _items[i] = item;
                }
            }
        }


        public void Render(Matrix4 view, Matrix4 proj,
                           Vector3 lightPos, Vector3 lightDir,
                           float cutCos, float range, Vector3 camPos)
        {
            float rotRad = MathHelper.DegreesToRadians(_time * ROT_SPEED_DEG);

            foreach (var it in _items)
            {
                if (it.Collected) continue;

                float yOffset = MathF.Sin((_time + it.Phase) * MathF.Tau * BOUNCE_FREQ) * BOUNCE_AMPL;
                Matrix4 model =
                    Matrix4.CreateScale(SCALE) *
                    Matrix4.CreateRotationX(rotRad * 0.5f) *
                    Matrix4.CreateRotationZ(rotRad * 0.3f) *
                    Matrix4.CreateRotationY(rotRad) *
                    Matrix4.CreateTranslation(it.Pos + new Vector3(0, yOffset, 0));

                _apple.Render(model, view, proj, lightPos, lightDir, cutCos, range, camPos);
            }
        }

        public void Dispose() => _apple.Dispose();
    }
}
