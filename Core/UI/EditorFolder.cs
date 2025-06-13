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
        private List<IEditorUI> FolderUi = new();

        public EditorFolder()
        {
            WhenToRender = GameWindowType.Editor;
            OtherWhenToRender = GameWindowType.Editor;
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

            foreach (var ui in FolderUi)
            {
                ui.Render();
            }

            ImGui.End();
        }
    }
}