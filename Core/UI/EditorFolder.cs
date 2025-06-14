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
    public class EditorFolder : BaseSubWindow
    {
        public EditorFolder() : base(true, false)
        {
            EditorWindow.BuildWindow.FileDrop += BuildWindow_FileDrop;
        }

        private void BuildWindow_FileDrop(OpenTK.Windowing.Common.FileDropEventArgs obj)
        {
            
        }

        public override void RenderUI()
        {
            float windowWidth = EditorWindow.BuildWindow.ClientSize.X;
            float windowHeight = EditorWindow.BuildWindow.ClientSize.Y;
            ImGui.SetNextWindowSize(new Vector2(windowWidth, ImGuiViewportUI.FolderBarSize), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, windowHeight - ImGuiViewportUI.FolderBarSize), ImGuiCond.Always);


            ImGui.Begin("File Explorer",
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            if (EditorWindow.BuildWindow.IsMouseButtonReleased(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button2) && ImGui.IsWindowHovered())
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