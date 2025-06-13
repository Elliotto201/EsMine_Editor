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
    public class EditorInspector : IEditorWindow
    {
        public EditorInspector()
        {
            WhenToRender = GameWindowType.Editor;
            OtherWhenToRender = GameWindowType.Editor;
        }

        public override void Render()
        {
            float windowWidth = Window.BuildWindow.ClientSize.X;
            float windowHeight = Window.BuildWindow.ClientSize.Y;

            ImGui.SetNextWindowPos(new Vector2(windowWidth - ImGuiViewportUI.InspectorBarSize, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(ImGuiViewportUI.InspectorBarSize, windowHeight - ImGuiViewportUI.FolderBarSize), ImGuiCond.Always);

            ImGui.Begin("Inspector",
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            ImGui.End();
        }
    }
}