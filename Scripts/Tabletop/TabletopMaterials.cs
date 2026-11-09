using Godot;
using System;
using System.Collections.Generic;

public sealed class TabletopMaterials : IDisposable
{
    private readonly Dictionary<(string, float, float), StandardMaterial3D> _paints = new();
    private readonly List<ShaderMaterial> _woods = new();

    public StandardMaterial3D Paint(string hex, float roughness = .65f, float metallic = 0)
    {
        var key = (hex, roughness, metallic);
        if (!_paints.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D
            {
                AlbedoColor = new Color(hex), Roughness = roughness,
                Metallic = metallic, MetallicSpecular = .5f
            };
            _paints.Add(key, material);
        }
        return material;
    }

    public ShaderMaterial Wood(string hex, float offset)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Scripts/Tabletop/Walnut.gdshader") };
        material.SetShaderParameter("wood_color", new Color(hex));
        material.SetShaderParameter("grain_offset", offset);
        _woods.Add(material);
        return material;
    }

    public void Dispose()
    {
        foreach (var material in _paints.Values) material.Dispose();
        foreach (var material in _woods) material.Dispose();
        _paints.Clear();
        _woods.Clear();
    }
}
