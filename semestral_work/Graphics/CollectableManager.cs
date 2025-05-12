using OpenTK.Mathematics;
using semestral_work.Config;
using semestral_work.Map;
using System;
using System.Collections.Generic;

namespace semestral_work.Graphics
{
    internal sealed class CollectableManager : IDisposable
    {
        private readonly AppleModel _apple;

        private readonly List<Item> _items = new();
        private float _time;                     // накопленное время

        // --- настройки анимации ---
        private const float SCALE = 0.005f;   // размер яблока
        private const float BOUNCE_AMPL = 0.08f;    // амплитуда подъёма (метры)
        private const float BOUNCE_FREQ = 2.0f;     // колебаний в секунду
        private const float ROT_SPEED_DEG = 90f;     // °/сек

        private struct Item
        {
            public Vector3 Pos;   // базовая позиция (XZ + базовая высота)
            public float Phase;   // сдвиг фазы, чтобы яблоки не прыгали синхронно
        }

        public CollectableManager(ParsedMap map, Shader appleShader)
        {
            _apple = new AppleModel(AppConfig.GetAppleModelPath(), appleShader);

            int seed = 0;
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    if (map.Cells[r, c] != CellType.Collectable) continue;

                    float x = c * 2f + 1f;
                    float z = r * 2f + 1f;

                    _items.Add(new Item
                    {
                        Pos = new Vector3(x, 0.30f, z),
                        Phase = seed * 0.5f     // разносим фазы
                    });
                    seed++;
                }
        }

        public void Update(float dt) => _time += dt;   // независимое накопление времени

        public void Render(Matrix4 view, Matrix4 proj,
                    Vector3 lightPos, Vector3 lightDir,
                    float cutCos, float range, Vector3 camPos)
        {
            float rotRad = MathHelper.DegreesToRadians(_time * ROT_SPEED_DEG);

            foreach (var it in _items)
            {
                float yOffset = MathF.Sin((_time + it.Phase) * MathF.Tau * BOUNCE_FREQ) * BOUNCE_AMPL;
                Matrix4 model =
                    Matrix4.CreateScale(SCALE) *
                    Matrix4.CreateRotationX(rotRad * 0.5f) *    // вращение по X (вперед-назад)
                    Matrix4.CreateRotationZ(rotRad * 0.3f) *    // вращение по Z (наклон)
                    Matrix4.CreateRotationY(rotRad) *           // основное вращение по Y
                    Matrix4.CreateTranslation(it.Pos + new Vector3(0, yOffset, 0));

                _apple.Render(model, view, proj, lightPos, lightDir, cutCos, range, camPos);
            }
        }


        public void Dispose() => _apple.Dispose();
    }
}
