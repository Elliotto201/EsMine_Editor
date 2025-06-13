using EngineInternal;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EngineExclude
{
    public class EditorFolder : IEditorWindow
    {
        public EditorFolder()
        {
            WhenToRender = GameWindowType.Editor;
            OtherWhenToRender = GameWindowType.Editor;

            Window.BuildWindow.FileDrop += BuildWindow_FileDrop;
        }

        private void BuildWindow_FileDrop(OpenTK.Windowing.Common.FileDropEventArgs obj)
        {
            
        }

        public override void Render()
        {
            float windowWidth = Window.BuildWindow.ClientSize.X;
            float windowHeight = Window.BuildWindow.ClientSize.Y;
            ImGui.SetNextWindowSize(new Vector2(windowWidth, ImGuiViewportUI.FolderBarSize), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, windowHeight - ImGuiViewportUI.FolderBarSize), ImGuiCond.Always);


            ImGui.Begin("File Explorer",
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            if (Window.BuildWindow.IsMouseButtonReleased(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button2) && ImGui.IsWindowHovered())
            {
                ImGui.OpenPopup("File Menu");
            }

            if (ImGui.BeginPopup("File Menu"))
            {
                if (ImGui.MenuItem("Create PrefabEntity"))
                {
                    
                }
                if (ImGui.MenuItem("Cancel"))
                {

                }
                ImGui.EndPopup();
            }

            ImGui.End();
        }
    }
}