using EngineCore;
using EngineInternal;
using ImGuiNET;
using Microsoft.VisualBasic.Devices;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace EngineExclude
{
    public class ImGuiViewportUI
    {
        static float FolderBarSize
        {
            get
            {
                return ScaleByWindowSize(220) + 2;
            }
        }
        static float ControllerBarSize
        {
            get
            {
                return ScaleByWindowSize(120);
            }
        }

        public static event Action UIEvent;
        private List<IEditorUI> ControllerUi = new();
        private List<IEditorUI> FolderUi = new();

        private static float ScaleByWindowSize(float input)
        {
            const float baseWidth = 1280f;
            const float baseHeight = 760f;

            var size = Window.BuildWindow.ClientSize;
            float currentWidth = size.X;
            float currentHeight = size.Y;

            float widthScale = currentWidth / baseWidth;
            float heightScale = currentHeight / baseHeight;

            // Use average scaling for general purpose
            float scale = (widthScale + heightScale) / 2f;

            return input * scale;
        }

        public ImGuiViewportUI()
        {
            ChangeUI();
            Window.BuildWindow.Resize += ChangeUI;
        }

        private void ChangeUI(ResizeEventArgs args = default)
        {
            ControllerUi = new();
            FolderUi = new();

            var bPlay = new EButton(new Vector2(60, 60), new Vector2(-70 + Window.BuildWindow.ClientSize.X / 2, 30), "Play");
            var pPlay = new EButton(new Vector2(60, 60), new Vector2(0 + Window.BuildWindow.ClientSize.X / 2, 30), "Pause");
            var ePlay = new EButton(new Vector2(60, 60), new Vector2(70 + Window.BuildWindow.ClientSize.X / 2, 30), "Exit");

            bPlay.OnClick += PlayEditor;
            ePlay.OnClick += ExitEditor;

            ControllerUi.Add(bPlay);
            ControllerUi.Add(pPlay);
            ControllerUi.Add(ePlay);

            //FolderUi.Add()
        }

        public void RenderUI()
        {
            if(Window.BuildWindow.GameType == GameWindowType.Editor)
            {
                var clientSize = Window.BuildWindow.ClientSize;
                var viewportSize = clientSize.ToVector2() / 1.5f;
                var center = clientSize.ToVector2() / 2f;

                // Move center up by 50 pixels
                center.Y -= 100;
                var topLeft = center - (viewportSize / 2f);

                ImGui.SetNextWindowSize(new Vector2(Window.BuildWindow.ClientSize.X, ControllerBarSize), ImGuiCond.Always);
                ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);

                ImGui.Begin("Controller",
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoNavFocus |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoBringToFrontOnFocus);

                foreach (var ui in ControllerUi)
                {
                    ui.Render();
                }

                ImGui.End();

                float windowWidth = Window.BuildWindow.ClientSize.X;
                float windowHeight = Window.BuildWindow.ClientSize.Y;
                ImGui.SetNextWindowSize(new Vector2(windowWidth, FolderBarSize), ImGuiCond.Always);
                ImGui.SetNextWindowPos(new Vector2(0, windowHeight - FolderBarSize), ImGuiCond.Always);
                

                ImGui.Begin("File Explorer",
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoNavFocus |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoBringToFrontOnFocus);

                foreach (var ui in FolderUi)
                {
                    ui.Render();
                }

                ImGui.End();
            }
            else if(Window.BuildWindow.GameType == GameWindowType.EditorBuild)
            {
                var clientSize = Window.BuildWindow.ClientSize;
                var viewportSize = clientSize.ToVector2() / 1.5f;
                var center = clientSize.ToVector2() / 2f;

                // Move center up by 50 pixels
                center.Y -= 100;
                var topLeft = center - (viewportSize / 2f);

                ImGui.SetNextWindowSize(new Vector2(Window.BuildWindow.ClientSize.X, ControllerBarSize), ImGuiCond.Always);
                ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);

                ImGui.Begin("Controller",
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoNavFocus |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoBringToFrontOnFocus);

                foreach (var ui in ControllerUi)
                {
                    ui.Render();
                }

                ImGui.End();
            }
        }

        private void PlayEditor()
        {
            if (Window.BuildWindow.GameType != GameWindowType.Editor) return;

            Window.BuildWindow.Dispose();

            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new OpenTK.Mathematics.Vector2i(1280, 760),
                Title = "My Compute Shader App",
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new Version(4, 3), // 👈 Must be 4.3 or higher
                Flags = ContextFlags.Debug
            };
            new Window(1280, 760, nativeWindowSettings, GameWindowType.EditorBuild).Run();

            Console.Write("Play");
        }

        private void ExitEditor()
        {
            if (Window.BuildWindow.GameType != GameWindowType.EditorBuild) return;

            Window.BuildWindow.Dispose();

            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new OpenTK.Mathematics.Vector2i(1280, 760),
                Title = "My Compute Shader App",
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new Version(4, 3), // 👈 Must be 4.3 or higher
                Flags = ContextFlags.Debug
            };
            new Window(1280, 760, nativeWindowSettings, GameWindowType.Editor).Run();

            Console.Write("Exit");
        }
    }

    public interface IEditorUI { public void Render(); }

    public class EButton : IEditorUI
    {
        private bool pressedLastFrame = false;
        private bool UseTexture = false;
        private nint Texture;

        public Vector2 Size { get; private set; }
        public Vector2 Position { get; private set; }
        public string Label { get; private set; }
        public event Action OnClick;

        public EButton(int sizeX, int sizeY, int posX, int posY, string label)
        {
            Size = new Vector2 (sizeX, sizeY);
            Position = new Vector2 (posX, posY);
            Label = label;
        }
        public EButton(Vector2 size, Vector2 pos, string label)
        {
            Size = size;
            Position = pos;
            Label = label;
        }

        public void Render()
        {
            ImGui.SetCursorPos(Position);
            if(!UseTexture)
            {
                bool buttonPress = ImGui.Button(Label, Size);

                if (buttonPress && !pressedLastFrame)
                {
                    OnClick?.Invoke();
                    pressedLastFrame = true;
                }
                else if(!buttonPress && pressedLastFrame)
                {
                    pressedLastFrame = false;
                }
            }
            else
            {
                bool buttonPress = ImGui.ImageButton(Label, Texture, Size);

                if (buttonPress && pressedLastFrame)
                {
                    OnClick?.Invoke();
                }
                else if (!buttonPress && pressedLastFrame)
                {
                    pressedLastFrame = false;
                }
            }
        }

        public void SetTexture(bool useTexture, nint texture = default)
        {
            UseTexture = useTexture;    
            if(UseTexture)
            {
                Texture = texture;
            }
        }
    }

    public class EColorBox : IEditorUI
    {
        public Vector2 Size { get; private set; }
        public Vector2 Position { get; private set; }
        public uint Color { get; private set; }

        public EColorBox(Vector2 size, Vector2 position, Vector4 color)
        {
            Size = size;
            Position = position;
            Color = ImGui.ColorConvertFloat4ToU32(color);
        }

        public void Render()
        {
            var drawList = ImGui.GetWindowDrawList();

            drawList.AddRectFilled(Position - Size, Position + Size, Color);
        }
    }
}