using Godot;

public static class SceneShapes
{
    public static MeshInstance3D Mesh(Node3D parent, Mesh mesh, Vector3 position, Material material)
    {
        var instance = new MeshInstance3D { Mesh = mesh, Position = position, MaterialOverride = material };
        parent.AddChild(instance);
        return instance;
    }

    public static MeshInstance3D Box(Node3D parent, Vector3 position, Vector3 size, Material material) =>
        Mesh(parent, new BoxMesh { Size = size }, position, material);

    public static MeshInstance3D Ellipsoid(Node3D parent, Vector3 position, Vector3 radius, Material material)
    {
        var instance = Mesh(parent, new SphereMesh { Radius = 1, Height = 2, RadialSegments = 32, Rings = 16 }, position, material);
        instance.Scale = radius;
        return instance;
    }

    public static MeshInstance3D Cylinder(Node3D parent, Vector3 position, float bottom, float top, float height, Material material, int sides = 32) =>
        Mesh(parent, new CylinderMesh { BottomRadius = bottom, TopRadius = top, Height = height, RadialSegments = sides, Rings = 1 }, position, material);

    public static MeshInstance3D Rod(Node3D parent, Vector3 from, Vector3 to, float radius, Material material)
    {
        var instance = Cylinder(parent, (from + to) / 2, radius, radius, from.DistanceTo(to), material, 16);
        instance.Quaternion = new Quaternion(Vector3.Up, (to - from).Normalized());
        return instance;
    }
}
