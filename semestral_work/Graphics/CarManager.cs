using OpenTK.Mathematics;
using semestral_work.Config;
using semestral_work.Graphics;
using semestral_work.Map;
using System;
using System.Collections.Generic;

/// <summary>
/// Třída zajišťující správu pozic aut v mapě a jejich vykreslování.
/// </summary>
internal sealed class CarManager : IDisposable
{
    private readonly CarModel _car;
    private readonly List<Vector3> _positions = new();

    private const float SCALE = 0.9f;

    /// <summary>
    /// Načte 3D model auta a uloží pozice z mapy, kde se mají auta vykreslit.
    /// </summary>
    public CarManager(ParsedMap map, Shader carShader)
    {
        _car = new CarModel(AppConfig.GetCarModelPath(), carShader);

        for (int r = 0; r < map.Rows; r++)
            for (int c = 0; c < map.Columns; c++)
                if (map.Cells[r, c] == CellType.Car)
                    _positions.Add(new Vector3(c * 2f + 1f, 0f, r * 2f + 1f));
    }

    /// <summary>
    /// Vykreslí všechna auta na jejich definovaných pozicích.
    /// </summary
    public void Render(Matrix4 view, Matrix4 proj,
                       Vector3 lightPos, Vector3 lightDir,
                       float cutCos, float range, Vector3 camPos)
    {
        foreach (var pos in _positions)
        {
            Vector3 world = pos + new Vector3(0f, _car.BaseShiftY * SCALE, 0f);

            Matrix4 model =
                Matrix4.CreateScale(SCALE) *
                Matrix4.CreateRotationY(_car.DefaultYaw) *
                Matrix4.CreateTranslation(world);

            _car.Render(model, view, proj,
                        lightPos, lightDir,
                        cutCos, range, camPos);
        }
    }

    public void Dispose() => _car.Dispose();
}
