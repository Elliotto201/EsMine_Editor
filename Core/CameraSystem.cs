using EngineInternal;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace EngineCore;
public static class Camera
{
    public static Vector3 PositionOffset { get; set; }
    public static Vector3 CameraPosition;
    public static Vector2 AzimuthElevation;
    public static Vector3 CameraDirection;
    public static Matrix4 CurrentViewMatrix { get; private set; }

    static Camera()
    {
        EditorWindow.BuildWindow.OnFrame += Update;
    }

    private static void Update()
    {
        GL.Viewport(0, 0, EditorWindow.BuildWindow.ClientSize.X, EditorWindow.BuildWindow.ClientSize.Y);

        CameraDirection = AzimuthElevationToUnitVector(AzimuthElevation);
        CurrentViewMatrix = GetViewMatrix();
    }

    private static Matrix4 GetViewMatrix()
    {
        var eye = CameraPosition;
        var target = CameraPosition + CameraDirection;

        float aspect = EditorWindow.BuildWindow.ClientSize.X / (float)Math.Max(1, EditorWindow.BuildWindow.ClientSize.Y);

        return Matrix4.LookAt(eye, target, Vector3.UnitY) *
               Matrix4.CreatePerspectiveFieldOfView(
                   1.3f,   // ~85 degrees
                   aspect,
                   0.01f,
                   300.0f);
    }

    private static Vector3 AzimuthElevationToUnitVector(Vector2 azimuthElevation)
    {
        float azimuth = azimuthElevation.X;
        float elevation = azimuthElevation.Y;

        float cosElev = (float)Math.Cos(elevation);
        return new Vector3(
            cosElev * (float)Math.Cos(azimuth),
            (float)Math.Sin(elevation),
            cosElev * (float)Math.Sin(azimuth)
        ).Normalized();
    }
}