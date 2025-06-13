using EngineExclude;
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
    public class EditorHierarchy : IEditorWindow
    {
        public EditorHierarchy()
        {
            WhenToRender = GameWindowType.Editor;
            OtherWhenToRender = GameWindowType.Editor;
        }

        public override void Render()
        {
            float windowWidth = Window.BuildWindow.ClientSize.X;
            float windowHeight = Window.BuildWindow.ClientSize.Y;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(ImGuiViewportUI.InspectorBarSize, windowHeight - ImGuiViewportUI.FolderBarSize), ImGuiCond.Always);

            ImGui.Begin("Hierarchy",
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            bool entityContext = false;

            ImGui.BeginChild("HierarchyScroll", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar);
            foreach (var entity in ImGuiViewportUI.Current.SceneEntities.ToArray())
            {
                ImGui.PushID(entity.GUID.ToString());
                bool isSelected = ImGuiViewportUI.Current.SelectedEntity?.GUID == entity.GUID;

                if (ImGui.Selectable($"{entity.Name}", isSelected, ImGuiSelectableFlags.None))
                {
                    ImGuiViewportUI.Current.SelectedEntity = entity;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    ImGuiViewportUI.Current.SelectedEntity = entity;
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
                        ImGuiViewportUI.Current.SceneEntities.Remove(entity);
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
    }
}
