using EngineCore;
using EngineInternal;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EngineExclude
{
    public class EditorInspector : IEditorWindow
    {
        private bool OpenScriptsMenu = false;
        private bool RemoveScriptsMenu = false;

        private List<IEditorUI> EntityInspectorUI = new();
        private Entity LastSelectedEntity;
        private List<FieldInfo> LastSelectedEntityFields;
        private List<ScriptLoad> LoadedScripts = new();

        public EditorInspector()
        {
            WhenToRender = GameWindowType.Editor;
            OtherWhenToRender = GameWindowType.Editor;

            var bButton = new EButton(new Vector2(ImGuiViewportUI.InspectorBarSize - 30, 30), 
                new Vector2(ImGuiViewportUI.InspectorBarSize - ImGuiViewportUI.ScaleByWindowSize(190), 430), "Add Behaviour", true, true);

            var dButton = new EButton(new Vector2(ImGuiViewportUI.InspectorBarSize - 30, 30), 
                new Vector2(ImGuiViewportUI.InspectorBarSize - ImGuiViewportUI.ScaleByWindowSize(190), 470), "Remove Behaviour", true, true);


            bButton.OnClick += AddBehaviourOnClick;
            dButton.OnClick += RemoveBehaviourOnClick;

            EntityInspectorUI.Add(bButton);
            EntityInspectorUI.Add(dButton);

            LastSelectedEntityFields = new();
            AssetDataBase.AssetRefresh += AssetRefresh;

            LoadedScripts = AssetDataBase.LoadAllScripts();
        }

        private void AddBehaviourOnClick()
        {
            OpenScriptsMenu = true;
            RemoveScriptsMenu = false;
        }

        private void RemoveBehaviourOnClick()
        {
            RemoveScriptsMenu = true;
            OpenScriptsMenu = false;
        }

        private void AssetRefresh()
        {
            LoadedScripts = AssetDataBase.LoadAllScripts();
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

            

            if(ImGuiViewportUI.Current.SelectedEntity != null)
            {
                if(LastSelectedEntity != ImGuiViewportUI.Current.SelectedEntity)
                {
                    LastSelectedEntityFields.Clear();

                    LastSelectedEntity = ImGuiViewportUI.Current.SelectedEntity;
                    if(Entity.EntityBehaviours.TryGetValue(LastSelectedEntity.GUID, out var behaviours))
                    {
                        foreach(var behaviour in behaviours)
                        {
                            foreach (var field in behaviour.GetType().GetFields().Where(f => f.GetCustomAttribute<Export>() != null))
                            {
                                LastSelectedEntityFields.Add(field);
                            }
                        }
                    }
                }

                if(ImGuiViewportUI.Current.SelectedEntity != null)
                {
                    foreach (var ui in EntityInspectorUI)
                    {
                        ui.Render();
                    }
                }

                foreach (var field in LastSelectedEntityFields)
                {
                    if (field.FieldType == typeof(int))
                    {
                        int Output = 0;
                        ImGui.InputInt(field.Name, ref Output);
                    }
                }


                if (OpenScriptsMenu)
                {
                    ImGui.SetNextWindowSize(new Vector2(300, 400), ImGuiCond.Appearing);
                    if (ImGui.Begin("Add Behaviour", ref OpenScriptsMenu, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking))
                    {
                        //The path to the current selected entities meta file
                        string currentEntityMeta = AssetDataBase.GetCurrentSelectedEntityMetaPath();

                        var metaFileText = Encoding.UTF8.GetString(File.ReadAllBytes(currentEntityMeta));
                        var metaFile = JsonConvert.DeserializeObject<EntityMetaFile>(metaFileText);

                        if (metaFile.Scripts.Count == LoadedScripts.Count)
                        {
                            ImGui.Text("No scripts to add.");
                        }
                        else
                        {
                            foreach (var script in LoadedScripts)
                            {
                                if (!metaFile.Scripts.Any(s => s.name == script.name) && ImGui.Selectable(script.name))
                                {
                                    if (!metaFile.Scripts.Any(s => s.name == script.name))
                                        metaFile.Scripts.Add(script);

                                    // Serialize back to file or wherever you store it
                                    var updatedText = JsonConvert.SerializeObject(metaFile, Formatting.Indented);
                                    File.WriteAllText(currentEntityMeta, updatedText);

                                    OpenScriptsMenu = false;
                                }
                            }
                        }
                    }
                    ImGui.End();
                }
                if (RemoveScriptsMenu)
                {
                    ImGui.SetNextWindowSize(new Vector2(300, 400), ImGuiCond.Appearing);

                    // Use RemoveScriptsMenu here so ImGui can toggle visibility correctly
                    if (ImGui.Begin("Remove Behaviour", ref RemoveScriptsMenu, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking))
                    {
                        // Load metaFile once per frame
                        string currentEntityMeta = AssetDataBase.GetCurrentSelectedEntityMetaPath();
                        var metaFileText = Encoding.UTF8.GetString(File.ReadAllBytes(currentEntityMeta));
                        var metaFile = JsonConvert.DeserializeObject<EntityMetaFile>(metaFileText);

                        if (metaFile.Scripts.Count == 0)
                        {
                            ImGui.Text("No scripts to remove.");
                        }
                        else
                        {
                            // Show only scripts attached to entity
                            foreach (var script in metaFile.Scripts.ToList()) // ToList() to safely modify list inside loop
                            {
                                if (ImGui.Selectable(script.name))
                                {
                                    var removed = metaFile.Scripts.RemoveAll(s => s.name == script.name) > 0;
                                    Console.WriteLine("Could remove item: " + removed);

                                    // Serialize back
                                    var updatedText = JsonConvert.SerializeObject(metaFile, Formatting.Indented);
                                    File.WriteAllText(currentEntityMeta, updatedText);

                                    RemoveScriptsMenu = false;
                                    break; // Exit loop early because list modified
                                }
                            }
                        }

                        ImGui.End();
                    }
                }
            }
            else if(ImGuiViewportUI.Current.CurrentSelectedGUI != null)
            {

            }

            ImGui.End();
        }
    }
}

public struct EntityMetaFile
{
    public List<ScriptLoad> Scripts;

    public EntityMetaFile()
    {
        Scripts = new();
    }
}