using EngineCore;
using EngineInternal;
using ImGuiNET;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design.Behavior;

namespace EngineExclude
{
    public class EditorInspector : BaseSubWindow
    {
        private bool OpenScriptsMenu = false;
        private bool RemoveScriptsMenu = false;
        
        private List<IEditorComponent> EntityInspectorUI = new();
        private Entity LastSelectedEntity;
        private List<FieldInfo> LastSelectedEntityFields;
        public static List<ScriptLoad> LoadedScripts = new();

        public EditorInspector() : base(true, false)
        {
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
            EditorHierarchy.OnSelectedEntity += EditInspectorData;
            EditorHierarchy.OnPreSelectedEntity += SaveInspectorFields;

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
            EditInspectorData();
        }

        private void EditInspectorData()
        {
            if (ImGuiViewportUI.Current.SelectedEntity != null)
            {
                LastSelectedEntityFields.Clear();

                LastSelectedEntity = ImGuiViewportUI.Current.SelectedEntity;
                foreach (var behaviour in LastSelectedEntity.Behaviours)
                {
                    foreach (var field in behaviour.GetType().GetFields().Where(f => f.GetCustomAttribute<Export>() != null))
                    {
                        var fieldType = field.FieldType;

                        LastSelectedEntityFields.Add(field);
                        var fieldValue = AssetDataBase.GetEntityFieldValue(LastSelectedEntity, field.Name);
                        object newFieldValue = fieldValue;

                        if (fieldValue != null)
                        {
                            newFieldValue = Convert.ChangeType(fieldValue, fieldType);
                        }

                        field.SetValue(behaviour, newFieldValue);
                    }
                }
            }
        }

        private void SaveInspectorFields()
        {
            if (ImGuiViewportUI.Current.SelectedEntity != null)
            {
                LastSelectedEntityFields.Clear();

                LastSelectedEntity = ImGuiViewportUI.Current.SelectedEntity;
                foreach (var behaviour in LastSelectedEntity.Behaviours)
                {
                    foreach (var field in behaviour.GetType().GetFields().Where(f => f.GetCustomAttribute<Export>() != null))
                    {
                        var fieldValue = field.GetValue(behaviour);
                        AssetDataBase.SetEntityMetaFields(LastSelectedEntity, field.Name, fieldValue);
                    }
                }
            }
        }

        public override void RenderUI()
        {
            float windowWidth = EditorWindow.BuildWindow.ClientSize.X;
            float windowHeight = EditorWindow.BuildWindow.ClientSize.Y;

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
                if (ImGuiViewportUI.Current.SelectedEntity != null)
                {
                    foreach (var ui in EntityInspectorUI)
                    {
                        ui.Render();
                    }
                }

                ImGui.SetCursorPos(new Vector2(0, 0));

                // Disable automatic scrolling to bottom

                foreach (var field in LastSelectedEntityFields)
                {
                    LoadInspectorField(field);
                }

                if (OpenScriptsMenu)
                {
                    ImGui.SetNextWindowSize(new Vector2(300, 400), ImGuiCond.Appearing);
                    if (ImGui.Begin("Add Behaviour", ref OpenScriptsMenu, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking))
                    {
                        AddEntityBehaviour();
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

        private void LoadInspectorField(FieldInfo field)
        {
            if (field.FieldType != typeof(int)) return;

            // Find the Behaviour instance
            Behaviour targetBehaviour = LastSelectedEntity.Behaviours.FirstOrDefault(b => b.GetType() == field.DeclaringType);
            if (targetBehaviour == null)
            {
                Console.WriteLine($"Warning: No Behaviour found with type {field.DeclaringType.Name} for field {field.Name}");
                return;
            }

            InspectorFields.DrawField(field, targetBehaviour);
        }

        private void AddEntityBehaviour()
        {
            // Get the current entity's meta file
            string currentEntityMetaPath = AssetDataBase.GetCurrentSelectedEntityMetaPath();
            var metaFileText = Encoding.UTF8.GetString(File.ReadAllBytes(currentEntityMetaPath));
            var metaFile = JsonConvert.DeserializeObject<EntityMetaFile>(metaFileText);

            if (metaFile.Scripts.Count >= LoadedScripts.Count)
            {
                ImGui.Text("No scripts to add.");
                return;
            }

            foreach (var script in LoadedScripts)
            {
                if (AddScriptToMetaFile(metaFile, script, currentEntityMetaPath))
                {
                    OpenScriptsMenu = false;
                    break;
                }
            }

            AssetDataBase.AssetRefresh?.Invoke();
        }

        //Jag försökte skriva en kommentar men det gick inte
        private bool AddScriptToMetaFile(EntityMetaFile metaFile, ScriptLoad script, string currentSelectedEntityMetaPath)
        {
            if (!(!metaFile.Scripts.Any(s => s.name == script.name) && ImGui.Selectable(script.name))) return false;

            // Add script to meta file
            metaFile.Scripts.Add(script);

            // Serialize back to disk
            var updatedText = JsonConvert.SerializeObject(metaFile, Formatting.Indented);
            File.WriteAllText(currentSelectedEntityMetaPath, updatedText);

            // Add Behaviour in memory
            Type behaviourType = GetTypeFromScript(script);
            var behaviourInstance = (Behaviour)Activator.CreateInstance(behaviourType);
            var behaviours = ImGuiViewportUI.Current.SelectedEntity.Behaviours.ToList();
            behaviours.Add(behaviourInstance);
            ImGuiViewportUI.Current.SelectedEntity.SetBehaviours(behaviours);

            return true;
        }

        private Type GetTypeFromScript(ScriptLoad script)
        {
            Type behaviourType = null;
            string className = script.name.Replace(".cs", "");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                behaviourType = assembly.GetTypes().FirstOrDefault(t => t.Name == className && typeof(Behaviour).IsAssignableFrom(t));
                if (behaviourType != null)
                    break;
            }

            if (behaviourType == null) throw new Exception($"Error: Could not find Behaviour type for script {script.name}");

            return behaviourType;
        }
    }
}

namespace EngineInternal
{
    public struct EntityMetaFile
    {
        public List<ScriptLoad> Scripts;
        public Dictionary<string, object> SerializedFields;

        public EntityMetaFile()
        {
            Scripts = new();
            SerializedFields = new();
        }
    }
}