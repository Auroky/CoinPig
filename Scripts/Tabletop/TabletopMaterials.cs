using Godot;
using System;
using System.Collections.Generic;

public sealed class TabletopMaterials : IDisposable
{
    private readonly Dictionary<(string, float, float), StandardMaterial3D> _paints = new();
    private readonly List<ShaderMaterial> _woods = new();
    private ShaderMaterial? _contact;

    public StandardMaterial3D Paint(Color color, float roughness = .88f, float metallic = 0) => Paint(color.ToHtml(), roughness, metallic);

    public StandardMaterial3D Paint(string hex, float roughness = .88f, float metallic = 0)
    {
        var key = (hex, roughness, metallic);
        if (!_paints.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D
            {
                AlbedoColor = new Color(hex), Roughness = roughness,
                Metallic = metallic, MetallicSpecular = .5f, CullMode = BaseMaterial3D.CullModeEnum.Disabled
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

    public ShaderMaterial ContactShade()
    {
        if (_contact != null) return _contact;
        _contact = new ShaderMaterial { Shader = GD.Load<Shader>("res://Scripts/Tabletop/Contact.gdshader") };
        _woods.Add(_contact);
        return _contact;
    }

    public ShaderMaterial Cabinet(Color tint)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Scripts/Tabletop/Cabinet.gdshader") };
        material.SetShaderParameter("tint", tint);
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
