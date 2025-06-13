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
                return Math.Clamp(ScaleByWindowSize(120), 0, 120);
            }
        }
        static float InspectorBarSize
        {
            get
            {
                return ScaleByWindowSize(210);
            }
        }

        public static event Action UIEvent;
        private List<IEditorUI> ControllerUi = new();
        private List<IEditorUI> FolderUi = new();
        private List<Entity> SceneEntities = new();

        private Entity SelectedEntity;

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

            AssetDataBase.AssetRefresh += AssetRefresh;
            AssetRefresh();
        }

        private void AssetRefresh()
        {
            SceneEntities = AssetDataBase.LoadAllSceneEntities();
        }

        private void ChangeUI(ResizeEventArgs args = default)
        {
            ControllerUi = new();
            FolderUi = new();

            var bPlay = new EButton(new Vector2(60, 60), new Vector2(-70 + ((Window.BuildWindow.ClientSize.X / 2) - InspectorBarSize), 30), "Play", VerticalAnchor.Top, HorizontalAnchor.Left);
            var pPlay = new EButton(new Vector2(60, 60), new Vector2(0 + ((Window.BuildWindow.ClientSize.X / 2) - InspectorBarSize), 30), "Pause", VerticalAnchor.Top, HorizontalAnchor.Center);
            var ePlay = new EButton(new Vector2(60, 60), new Vector2(70 + ((Window.BuildWindow.ClientSize.X / 2) - InspectorBarSize), 30), "Exit", VerticalAnchor.Top, HorizontalAnchor.Right);

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

                ImGui.SetNextWindowSize(new Vector2(Window.BuildWindow.ClientSize.X - InspectorBarSize * 2, ControllerBarSize), ImGuiCond.Always);
                ImGui.SetNextWindowPos(new Vector2(InspectorBarSize, 0), ImGuiCond.Always);

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

                ImGui.SetNextWindowPos(new Vector2(windowWidth - InspectorBarSize, 0), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(InspectorBarSize, windowHeight - FolderBarSize), ImGuiCond.Always);

                ImGui.Begin("Inspector",
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoNavFocus |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoBringToFrontOnFocus);

                ImGui.End();

                ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(InspectorBarSize, windowHeight - FolderBarSize), ImGuiCond.Always);

                ImGui.Begin("Hierarchy",
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoNavFocus |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoBringToFrontOnFocus);

                bool entityContext = false;

                ImGui.BeginChild("HierarchyScroll", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar);
                foreach (var entity in SceneEntities.ToArray())
                {
                    ImGui.PushID(entity.GUID.ToString());
                    bool isSelected = SelectedEntity?.GUID == entity.GUID;

                    if (ImGui.Selectable($"{entity.Name}", isSelected, ImGuiSelectableFlags.None))
                    {
                        SelectedEntity = entity;
                    }
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        SelectedEntity = entity;
                    }

                    if (Window.BuildWindow.IsMouseButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button2) && ImGui.IsItemHovered())
                    {
                        entityContext = true;

                        ImGui.OpenPopup("Entity Menu");
                    }

                    if (ImGui.BeginPopup("Entity Menu"))
                    {
                        if (ImGui.MenuItem("Delete"))
                        {
                            AssetDataBase.DeleteEntityHiearch(entity.GUID);
                            SceneEntities.Remove(entity);
                        }
                        if (ImGui.MenuItem("Cancel"))
                        {

                        }
                        ImGui.EndPopup();
                    }

                    ImGui.PopID();
                }

                if (Window.BuildWindow.IsMouseButtonReleased(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button2) && ImGui.IsWindowHovered() && !entityContext)
                {
                    ImGui.OpenPopup("Scene Menu");
                }

                if (ImGui.BeginPopup("Scene Menu"))
                {
                    if (ImGui.MenuItem("Create Entity"))
                    {
                        AssetDataBase.CreateEntityHiearchy();
                    }
                    if (ImGui.MenuItem("Cancel"))
                    {

                    }
                    ImGui.EndPopup();
                }

                ImGui.EndChild();

                if (ImGui.BeginPopupContextWindow("SceneMenu"))
                {
                    if (ImGui.MenuItem("Create Entity"))
                    {
                        AssetDataBase.CreateEntityHiearchy();
                    }
                    if (ImGui.MenuItem("Cancel"))
                    {
                        // No action needed
                    }
                    ImGui.EndPopup();
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

        private bool MouseIsInsideView()
        {
            var mousePos = Window.BuildWindow.MousePosition;

            return mousePos.X < Window.BuildWindow.ClientSize.X - InspectorBarSize && 
                mousePos.Y > ControllerBarSize && 
                mousePos.Y < Window.BuildWindow.ClientSize.Y - FolderBarSize && 
                mousePos.X > InspectorBarSize;
        }
    }

    public interface IEditorUI { public void Render(); }

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