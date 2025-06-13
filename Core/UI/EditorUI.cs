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
    public enum HorizontalAnchor
    {
        Left,
        Right,
        Center
    }

    public enum VerticalAnchor
    {
        Top,
        Bottom,
        Center
    }

    public class ImGuiViewportUI
    {
        public static ImGuiViewportUI Current { get; private set; }

        public static float FolderBarSize
        {
            get
            {
                return ScaleByWindowSize(220) + 2;
            }
        }
        public static float ControllerBarSize
        {
            get
            {
                return Math.Clamp(ScaleByWindowSize(120), 0, 120);
            }
        }
        public static float InspectorBarSize
        {
            get
            {
                return ScaleByWindowSize(210);
            }
        }

        public static Action Resize;

        public static event Action UIEvent;
        public List<Entity> SceneEntities = new();

        public Entity SelectedEntity;
        private IInspectorGUI CurrentSelectedGUI;

        private List<IEditorWindow> EditorWindows = new();

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
            Current = this;

            Window.BuildWindow.Resize += BuildWindow_Resize; ;

            AssetDataBase.AssetRefresh += AssetRefresh;
            AssetRefresh();

            EditorWindows.Add(new EditorController());
            EditorWindows.Add(new EditorFolder());
            EditorWindows.Add(new EditorInspector());
            EditorWindows.Add(new EditorHierarchy());
        }

        private void BuildWindow_Resize(ResizeEventArgs obj)
        {
            UIEvent?.Invoke();
        }

        private void AssetRefresh()
        {
            SceneEntities = AssetDataBase.LoadAllSceneEntities();
        }

        public void RenderUI()
        {
            foreach (var window in EditorWindows)
            {
                if (window.WhenToRender == Window.BuildWindow.GameType || window.OtherWhenToRender == Window.BuildWindow.GameType)
                {
                    window.Render();
                }
            }
        }

        private bool MouseIsInsideView()
        {
            return !ImGui.IsAnyItemHovered();
        }
    }

    public interface IEditorUI { public void Render(); }
    public abstract class IEditorWindow
    {
        internal GameWindowType WhenToRender;
        internal GameWindowType OtherWhenToRender;

        public abstract void Render();
    }

    public class EButton : IEditorUI
    {
        private bool pressedLastFrame = false;
        private bool UseTexture = false;
        private nint Texture;

        private HorizontalAnchor HAnchor;
        private VerticalAnchor VAnchor;

        public Vector2 Size { get; private set; }
        public Vector2 Position { get; private set; }
        public string Label { get; private set; }
        public event Action OnClick;

        public EButton(int sizeX, int sizeY, int posX, int posY, string label, VerticalAnchor vanchor, HorizontalAnchor hanchor)
        {
            Size = new Vector2 (sizeX, sizeY);
            Position = new Vector2 (posX, posY);
            Label = label;

            VAnchor = vanchor;
            HAnchor = hanchor;
        }
        public EButton(Vector2 size, Vector2 pos, string label, VerticalAnchor vanchor, HorizontalAnchor hanchor)
        {
            Size = size;
            Position = pos;
            Label = label;

            VAnchor = vanchor;
            HAnchor = hanchor;
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

namespace EngineInternal
{
    public interface IInspectorGUI
    {
        public void DrawInspector();
    }
}