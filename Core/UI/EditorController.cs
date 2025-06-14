using EngineInternal;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace EngineExclude
{
    public class EditorController : BaseSubWindow
    {
        private List<IEditorComponent> ControllerUi = new();

        public EditorController() : base(true, true)
        {
            ControllerUi = new();

            var bPlay = new EButton(new Vector2(60, 60), new Vector2(-70 + ((EditorWindow.BuildWindow.ClientSize.X / 2) - ImGuiViewportUI.InspectorBarSize), 30), "Play", true, false);
            var pPlay = new EButton(new Vector2(60, 60), new Vector2(0 + ((EditorWindow.BuildWindow.ClientSize.X / 2) - ImGuiViewportUI.InspectorBarSize), 30), "Pause", true, false);
            var ePlay = new EButton(new Vector2(60, 60), new Vector2(70 + ((EditorWindow.BuildWindow.ClientSize.X / 2) - ImGuiViewportUI.InspectorBarSize), 30), "Exit", true, false);

            bPlay.OnClick += PlayEditor;
            ePlay.OnClick += ExitEditor;

            ControllerUi.Add(bPlay);
            ControllerUi.Add(pPlay);
            ControllerUi.Add(ePlay);
        }

        public override void RenderUI()
        {
            ImGui.SetNextWindowSize(new Vector2(EditorWindow.BuildWindow.ClientSize.X - ImGuiViewportUI.InspectorBarSize * 2, ImGuiViewportUI.ControllerBarSize), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(ImGuiViewportUI.InspectorBarSize, 0), ImGuiCond.Always);

            ImGui.Begin("Controller",
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus | 
                ImGuiWindowFlags.NoScrollbar);

            foreach (var ui in ControllerUi)
            {
                ui.Render();
            }

            ImGui.End();
        }

        private void PlayEditor()
        {
            if (EditorWindow.BuildWindow.GameType != GameWindowType.Editor) return;

            EditorWindow.BuildWindow.Dispose();

            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new OpenTK.Mathematics.Vector2i(1280, 760),
                Title = "My Compute Shader App",
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new Version(4, 3), // 👈 Must be 4.3 or higher
                Flags = ContextFlags.Debug
            };
            new EditorWindow(1280, 760, nativeWindowSettings, GameWindowType.EditorBuild).Run();

            Console.Write("Play");
        }

        private void ExitEditor()
        {
            if (EditorWindow.BuildWindow.GameType != GameWindowType.EditorBuild) return;

            EditorWindow.BuildWindow.Dispose();

            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new OpenTK.Mathematics.Vector2i(1280, 760),
                Title = "My Compute Shader App",
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new Version(4, 3), // 👈 Must be 4.3 or higher
                Flags = ContextFlags.Debug
            };
            new EditorWindow(1280, 760, nativeWindowSettings, GameWindowType.Editor).Run();

            Console.Write("Exit");
        }
    }
}
