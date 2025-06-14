using EngineInternal;
using OpenTK.Graphics.OpenGL4;
using EngineCore;
using OpenTK.Mathematics;
using System;

namespace EngineExclude
{
    public static class EditorCamera
    {
        public static EngineCore.Vector3 CameraPosition;
        public static Vector2 AzimuthElevation;
        public static EngineCore.Vector3 CameraDirection;
        public static Matrix4 CurrentViewMatrix { get; private set; }

        private static Vector2 lastMousePos;
        private static bool firstMouse = true;
        private const float mouseSensitivity = 0.0025f;
        private const float moveSpeed = 0.1f;

        static EditorCamera()
        {
            EditorWindow.BuildWindow.OnFrame += Update;
        }

        private static void Update()
        {
            if (EditorWindow.BuildWindow.GameType != GameWindowType.EditorBuild) return;

            GL.Viewport(0, 0, EditorWindow.BuildWindow.ClientSize.X, EditorWindow.BuildWindow.ClientSize.Y);

            float moveX = 0f;
            float moveZ = 0f;

            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A)) moveX -= 1f;
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D)) moveX += 1f;
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W)) moveZ += 1f;
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S)) moveZ -= 1f;

            var moveVector = new EngineCore.Vector3(moveX, 0f, moveZ);
            if (moveVector.X != 0f || moveVector.Z != 0f)
                moveVector = moveVector.Normalized();

            Vector2 currentMousePos = EditorWindow.BuildWindow.MousePosition;
            if (firstMouse)
            {
                lastMousePos = currentMousePos;
                firstMouse = false;
            }
            Vector2 mouseDelta = currentMousePos - lastMousePos;
            lastMousePos = currentMousePos;

            AzimuthElevation.X += mouseDelta.X * mouseSensitivity;
            AzimuthElevation.Y -= mouseDelta.Y * mouseSensitivity;

            if (AzimuthElevation.Y > MathF.PI / 2f) AzimuthElevation.Y = MathF.PI / 2f;
            if (AzimuthElevation.Y < -MathF.PI / 2f) AzimuthElevation.Y = -MathF.PI / 2f;

            CameraDirection = AzimuthElevationToUnitVector(AzimuthElevation);

            EngineCore.Vector3 forward = new EngineCore.Vector3(CameraDirection.X, 0f, CameraDirection.Z).Normalized();
            EngineCore.Vector3 right = new EngineCore.Vector3(forward.Z, 0f, -forward.X);

            CameraPosition += forward * moveVector.Z * moveSpeed;
            CameraPosition += right * moveVector.X * moveSpeed;

            CurrentViewMatrix = GetViewMatrix();
        }

        private static Matrix4 GetViewMatrix()
        {
            if (EditorWindow.BuildWindow.GameType != GameWindowType.EditorBuild) return Matrix4.Zero;

            var eye = CameraPosition;
            var target = CameraPosition + CameraDirection;

            float aspect = EditorWindow.BuildWindow.ClientSize.X / (float)Math.Max(1, EditorWindow.BuildWindow.ClientSize.Y);

            return Matrix4.LookAt(eye, target, EngineCore.Vector3.UnitY) *
                   Matrix4.CreatePerspectiveFieldOfView(
                       1.3f,
                       aspect,
                       0.01f,
                       300.0f);
        }

        private static EngineCore.Vector3 AzimuthElevationToUnitVector(Vector2 azimuthElevation)
        {
            if (EditorWindow.BuildWindow.GameType != GameWindowType.EditorBuild) return EngineCore.Vector3.UnitY;

            float azimuth = azimuthElevation.X;
            float elevation = azimuthElevation.Y;

            float cosElev = MathF.Cos(elevation);
            return new EngineCore.Vector3(
                cosElev * MathF.Cos(azimuth),
                MathF.Sin(elevation),
                cosElev * MathF.Sin(azimuth)
            ).Normalized();
        }
    }
}
