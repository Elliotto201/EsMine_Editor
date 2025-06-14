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
        public IInspectorGUI CurrentSelectedGUI;

        private List<BaseSubWindow> EditorWindows = new();

        public static float ScaleByWindowSize(float input)
        {
            const float baseWidth = 1280f;
            const float baseHeight = 760f;

            var size = EditorWindow.BuildWindow.ClientSize;
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

            EditorWindow.BuildWindow.Resize += BuildWindow_Resize;

            AssetDataBase.AssetRefresh += AssetRefresh;

            if(EditorWindow.BuildWindow.GameType == GameWindowType.Editor)
            {
                EditorWindows.Add(new EditorFolder());
                EditorWindows.Add(new EditorInspector());
                EditorWindows.Add(new EditorHierarchy());
            }
            EditorWindows.Add(new EditorController());

            //Important that you refresh after windows are loaded as the AsserRrefresh is dependant on the EditorHiearchy which is Reliant on EditorInspector.
            //This is bad but for now im too tired to actually fix.
            AssetRefresh();
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
                window.Render();
            }
        }

        private bool MouseIsInsideView()
        {
            return !ImGui.IsAnyItemHovered();
        }
    }

    public interface IEditorComponent { public void Render(); }
    public abstract class BaseSubWindow
    {
        protected bool RenderEditor;
        protected bool RenderEditorBuild;

        public BaseSubWindow(bool _RenderInEditor, bool _RenderInEditorBuild)
        {
            RenderEditor = _RenderInEditor;
            RenderEditorBuild = _RenderInEditorBuild;
        }

        public virtual void Render()
        {
            if (RenderEditor && EditorWindow.BuildWindow.GameType == GameWindowType.Editor || RenderEditorBuild && EditorWindow.BuildWindow.GameType == GameWindowType.EditorBuild)
            {
                RenderUI();
            }
        }

        public abstract void RenderUI();
    }

    public class EButton : IEditorComponent
    {
        private bool pressedLastFrame = false;
        private bool UseTexture = false;
        private nint Texture;

        private bool ScaleSize;
        private bool ScalePos;

        public Vector2 Size { get; private set; }
        public Vector2 Position { get; private set; }
        public string Label { get; private set; }
        public event Action OnClick;

        public EButton(int sizeX, int sizeY, int posX, int posY, string label, bool scalePos, bool scaleSize)
        {
            Size = new Vector2 (sizeX, sizeY);
            Position = new Vector2 (posX, posY);
            Label = label;

            ScaleSize = scaleSize;
            ScalePos = scalePos;
        }
        public EButton(Vector2 size, Vector2 pos, string label, bool scalePos, bool scaleSize)
        {
            Size = size;
            Position = pos;
            Label = label;

            ScaleSize = scaleSize;
            ScalePos = scalePos;
        }

        public void Render()
        {
            if (ScalePos)
            {
                ImGui.SetCursorPos(ScaleByWindowSize(Position));
            }
            else
            {
                ImGui.SetCursorPos(Position);
            }
            Vector2 size = Size;
            if (ScaleSize)
            {
                size = ScaleByWindowSize(Size);
            }

            if(!UseTexture)
            {
                bool buttonPress = ImGui.Button(Label, size);

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
                bool buttonPress = ImGui.ImageButton(Label, Texture, size);

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

        private static float ScaleByWindowSize(float input)
        {
            const float baseWidth = 1280f;
            const float baseHeight = 760f;

            var size = EditorWindow.BuildWindow.ClientSize;
            float currentWidth = size.X;
            float currentHeight = size.Y;

            float widthScale = currentWidth / baseWidth;
            float heightScale = currentHeight / baseHeight;

            // Use average scaling for general purpose
            float scale = (widthScale + heightScale) / 2f;

            return input * scale;
        }

        private static Vector2 ScaleByWindowSize(Vector2 input)
        {
            const float baseWidth = 1280f;
            const float baseHeight = 760f;

            var size = EditorWindow.BuildWindow.ClientSize;
            float currentWidth = size.X;
            float currentHeight = size.Y;

            float widthScale = currentWidth / baseWidth;
            float heightScale = currentHeight / baseHeight;

            // Use average scaling for general purpose
            float scale = (widthScale + heightScale) / 2f;

            return input * scale;
        }
    }
    public class EColorBox : IEditorComponent
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
        public InspectorDrawType DrawInspector();
    }

    public enum InspectorDrawType
    {

    }
}