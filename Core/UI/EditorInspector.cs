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
using System.Windows.Forms.Design.Behavior;

namespace EngineExclude
{
    public class EditorInspector : IEditorWindow
    {
        private bool OpenScriptsMenu = false;
        private bool RemoveScriptsMenu = false;
        
        private List<IEditorUI> EntityInspectorUI = new();
        private Entity LastSelectedEntity;
        private List<FieldInfo> LastSelectedEntityFields;
        public static List<ScriptLoad> LoadedScripts = new();

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

            if (ImGuiViewportUI.Current.SelectedEntity != null)
            {
                LastSelectedEntityFields.Clear();

                LastSelectedEntity = ImGuiViewportUI.Current.SelectedEntity;
                foreach (var behaviour in LastSelectedEntity.Behaviours)
                {
                    foreach (var field in behaviour.GetType().GetFields().Where(f => f.GetCustomAttribute<Export>() != null))
                    {
                        LastSelectedEntityFields.Add(field);
                    }
                }
            }
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
                if(ImGuiViewportUI.Current.SelectedEntity != LastSelectedEntity)
                {
                    LastSelectedEntityFields.Clear();

                    LastSelectedEntity = ImGuiViewportUI.Current.SelectedEntity;
                    foreach (var behaviour in LastSelectedEntity.Behaviours)
                    {
                        foreach (var field in behaviour.GetType().GetFields().Where(f => f.GetCustomAttribute<Export>() != null))
                        {
                            LastSelectedEntityFields.Add(field);
                        }
                    }
                }

                if (ImGuiViewportUI.Current.SelectedEntity != null)
                {
                    foreach (var ui in EntityInspectorUI)
                    {
                        ui.Render();
                    }
                }

                ImGui.SetCursorPos(new Vector2(0, 0));

                // Disable automatic scrolling to bottom
                ImGui.SetScrollY(0);

                foreach (var field in LastSelectedEntityFields)
                {
                    if (field.FieldType == typeof(int))
                    {
                        // Find the Behaviour instance
                        Behaviour targetBehaviour = LastSelectedEntity.Behaviours.FirstOrDefault(b => b.GetType() == field.DeclaringType);
                        if (targetBehaviour == null)
                        {
                            Console.WriteLine($"Warning: No Behaviour found with type {field.DeclaringType.Name} for field {field.Name}");
                            continue;
                        }

                        // Get the current value
                        int output = (int)field.GetValue(targetBehaviour);

                        ImGui.PushID(field.Name);

                        ImGui.Text(field.Name);
                        if (ImGui.InputInt($"##{field.Name}", ref output, 1, 5, ImGuiInputTextFlags.NoUndoRedo | ImGuiInputTextFlags.CharsNoBlank))
                        {
                            field.SetValue(targetBehaviour, output);
                            Console.WriteLine($"Updated {field.Name} to {output} on {targetBehaviour.GetType().Name}");
                        }

                        ImGui.PopID();
                    }
                }

                if (OpenScriptsMenu)
                {
                    ImGui.SetNextWindowSize(new Vector2(300, 400), ImGuiCond.Appearing);
                    if (ImGui.Begin("Add Behaviour", ref OpenScriptsMenu, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking))
                    {
                        // Get the current entity's meta file
                        string currentEntityMeta = AssetDataBase.GetCurrentSelectedEntityMetaPath();
                        var metaFileText = Encoding.UTF8.GetString(File.ReadAllBytes(currentEntityMeta));
                        var metaFile = JsonConvert.DeserializeObject<EntityMetaFile>(metaFileText);

                        if (metaFile.Scripts.Count >= LoadedScripts.Count)
                        {
                            ImGui.Text("No scripts to add.");
                        }
                        else
                        {
                            foreach (var script in LoadedScripts)
                            {
                                if (!metaFile.Scripts.Any(s => s.name == script.name) && ImGui.Selectable(script.name))
                                {
                                    // Add script to meta file
                                    metaFile.Scripts.Add(script);

                                    // Serialize back to disk
                                    var updatedText = JsonConvert.SerializeObject(metaFile, Formatting.Indented);
                                    File.WriteAllText(currentEntityMeta, updatedText);

                                    // Add Behaviour in memory
                                    Type behaviourType = null;
                                    string className = script.name.Replace(".cs", "");
                                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                    {
                                        behaviourType = assembly.GetTypes().FirstOrDefault(t => t.Name == className && typeof(Behaviour).IsAssignableFrom(t));
                                        if (behaviourType != null)
                                            break;
                                    }

                                    if (behaviourType == null)
                                    {
                                        Console.WriteLine($"Error: Could not find Behaviour type for script {className}");
                                        continue;
                                    }

                                    var behaviourInstance = (Behaviour)Activator.CreateInstance(behaviourType);
                                    var behaviours = ImGuiViewportUI.Current.SelectedEntity.Behaviours.ToList();
                                    behaviours.Add(behaviourInstance);
                                    ImGuiViewportUI.Current.SelectedEntity.SetBehaviours(behaviours); // Assumes SetBehaviours exists

                                    OpenScriptsMenu = false;
                                    AssetDataBase.AssetRefresh?.Invoke();
                                }
                            }
                        }
                    }
                    ImGui.End();
                }

                if (RemoveScriptsMenu)
                {
                    ImGui.SetNextWindowSize(new Vector2(300, 400), ImGuiCond.Appearing);
                    if (ImGui.Begin("Remove Behaviour", ref RemoveScriptsMenu, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking))
                    {
                        // Load meta file
                        string currentEntityMeta = AssetDataBase.GetCurrentSelectedEntityMetaPath();
                        var metaFileText = Encoding.UTF8.GetString(File.ReadAllBytes(currentEntityMeta));
                        var metaFile = JsonConvert.DeserializeObject<EntityMetaFile>(metaFileText);

                        if (metaFile.Scripts.Count == 0)
                        {
                            ImGui.Text("No scripts to remove.");
                        }
                        else
                        {
                            foreach (var script in metaFile.Scripts.ToList()) // ToList() for safe modification
                            {
                                if (ImGui.Selectable(script.name))
                                {
                                    // Remove script from meta file
                                    var removed = metaFile.Scripts.RemoveAll(s => s.name == script.name) > 0;
                                    Console.WriteLine($"Removed script {script.name}: {removed}");

                                    // Serialize back to disk
                                    var updatedText = JsonConvert.SerializeObject(metaFile, Formatting.Indented);
                                    File.WriteAllText(currentEntityMeta, updatedText);

                                    // Remove Behaviour in memory
                                    string className = script.name.Replace(".cs", "");
                                    Type behaviourType = null;
                                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                    {
                                        behaviourType = assembly.GetTypes().FirstOrDefault(t => t.Name == className && typeof(Behaviour).IsAssignableFrom(t));
                                        if (behaviourType != null)
                                            break;
                                    }

                                    if (behaviourType != null)
                                    {
                                        var behaviours = ImGuiViewportUI.Current.SelectedEntity.Behaviours.ToList();
                                        var behaviourToRemove = behaviours.FirstOrDefault(b => b.GetType() == behaviourType);
                                        if (behaviourToRemove != null)
                                        {
                                            behaviours.Remove(behaviourToRemove);
                                            ImGuiViewportUI.Current.SelectedEntity.SetBehaviours(behaviours); // Assumes SetBehaviours exists
                                            Console.WriteLine($"Removed Behaviour {className} from entity in memory");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Warning: Behaviour {className} not found in entity Behaviours");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Error: Could not find Behaviour type for script {className}");
                                    }

                                    AssetDataBase.AssetRefresh?.Invoke();
                                    RemoveScriptsMenu = false;
                                    break; // Exit loop after modification
                                }
                            }
                        }
                    }
                    ImGui.End();
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