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
    public class EditorHierarchy : BaseSubWindow
    {
        public static Action OnSelectedEntity { get; set; }
        public static Action OnPreSelectedEntity { get; set; }

        public EditorHierarchy() : base(true, false)
        {
            
        }

        public override void RenderUI()
        {
            float windowWidth = EditorWindow.BuildWindow.ClientSize.X;
            float windowHeight = EditorWindow.BuildWindow.ClientSize.Y;

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
                    OnPreSelectedEntity?.Invoke();
                    ImGuiViewportUI.Current.SelectedEntity = entity;
                    OnSelectedEntity?.Invoke();
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    OnPreSelectedEntity?.Invoke();
                    ImGuiViewportUI.Current.SelectedEntity = entity;
                    OnSelectedEntity?.Invoke();
                }

                if (EditorWindow.BuildWindow.IsMouseButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button2) && ImGui.IsItemHovered())
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

                        ImGuiViewportUI.Current.SelectedEntity = null;
                    }
                    if (ImGui.MenuItem("Cancel"))
                    {

                    }
                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }

            if (EditorWindow.BuildWindow.IsMouseButtonReleased(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button2) && ImGui.IsWindowHovered() && !entityContext)
            {
                ImGui.OpenPopup("Scene Menu");
            }

            if (ImGui.BeginPopup("Scene Menu"))
            {
                if (ImGui.MenuItem("Create Entity"))
                {
                    AssetDataBase.CreateEntityInHiearchy();
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
                    AssetDataBase.CreateEntityInHiearchy();
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
