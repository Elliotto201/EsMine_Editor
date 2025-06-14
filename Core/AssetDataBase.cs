//This file contains two parts of this program. The asset serializer and the AssetDataBase.
//The asset serializer is used to serialize diffrent assets to and from disk and this is for things that are game specific and that might require custom serialization
//The AssetDataBase is for managing files and the Assets for the game. Both in the editor for creating the files and writing them. And at build time to make the Assetpack and
//-write it efficiently as binary to then be unloaded again


using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EngineCore;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using EngineExclude;
using System.Reflection;

namespace EngineInternal
{
    public static class AssetDataBase
    {
        public const string HIEARCHY_ENTITY = ".sEntity";
        public const string ENTITY_METAFILE = ".eMeta";

        public static string AssetDirectory;
        public static string HiddenAssetDirectory;

        public static Action AssetRefresh;

        static AssetDataBase()
        {
            string currentDir = Directory.GetCurrentDirectory();

            if (EditorWindow.BuildWindow.GameType == GameWindowType.Editor || EditorWindow.BuildWindow.GameType == GameWindowType.EditorBuild)
            {

                // Go back two folders
                string twoLevelsUp = Path.GetFullPath(Path.Combine(currentDir, "..", Path.Combine("..", "..")));

                // Then combine with Assets
                //Non hidden Assets
                AssetDirectory = Path.Combine(twoLevelsUp, "Assets");
                Directory.CreateDirectory(AssetDirectory);
                AssetDirectory += Path.DirectorySeparatorChar;

                //Hidden Assets
                HiddenAssetDirectory = Path.Combine(twoLevelsUp, "HiddenAssets");
                Directory.CreateDirectory(HiddenAssetDirectory);
                HiddenAssetDirectory += Path.DirectorySeparatorChar;
            }
        }

        public static void CreateEntityInHiearchy()
        {
            if(EditorWindow.BuildWindow.GameType == GameWindowType.Editor)
            {
                var entity = new Entity("New Entity");

                using (var fs = File.Create(HiddenAssetDirectory + entity.GUID.ToString() + HIEARCHY_ENTITY))
                {
                    var bytes = AssetSerializer.SerializeAsset(entity);
                    fs.Write(bytes);
                }
                using (var metafs = File.Create(HiddenAssetDirectory + entity.GUID.ToString() + ENTITY_METAFILE))
                {
                    var mFile = new EntityMetaFile();

                    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mFile));
                    metafs.Write(bytes);
                }

                AssetRefresh?.Invoke();
            }
        }

        //deletes a Hiearchy Entity from memory and disk
        public static void DeleteEntityHiearch(Guid entityGuid)
        {
            if(EditorWindow.BuildWindow.GameType == GameWindowType.Editor)
            {
                File.Delete(HiddenAssetDirectory + entityGuid.ToString() + HIEARCHY_ENTITY);
                File.Delete(HiddenAssetDirectory + entityGuid.ToString() + ENTITY_METAFILE);

                AssetRefresh?.Invoke();
            }
        }

        //Used for getting all entities that are in the scene hiearchy. Used for well getting all entities
        public static List<Entity> LoadAllSceneEntities()
        {
            List<Entity> entities = new List<Entity>();

            if (EditorWindow.BuildWindow.GameType == GameWindowType.Editor)
            {
                foreach(var file in Directory.GetFiles(HiddenAssetDirectory))
                {
                    if (file.EndsWith(HIEARCHY_ENTITY))
                    {
                        Entity entity = AssetSerializer.DeSerializeAsset(File.ReadAllBytes(file));
                        entities.Add(entity);
                    }
                }
            }

            return entities;
        }

        public static List<ScriptLoad> LoadAllScripts()
        {
            var scriptPaths = Directory.GetFiles(AssetDirectory).Where(t => t.EndsWith(".cs"));
            List<ScriptLoad> scripts = new List<ScriptLoad>(scriptPaths.Count());

            foreach(var scriptPath in scriptPaths)
            {
                var script = new ScriptLoad(scriptPath.Remove(0, AssetDirectory.Length), scriptPath);

                scripts.Add(script);
            }

            return scripts;
        }

        public static string GetCurrentSelectedEntityMetaPath()
        {
            var files = Directory.GetFiles(HiddenAssetDirectory).Where(t => t.EndsWith(ENTITY_METAFILE));

            foreach(var file in files)
            {
                if (file.Contains(ImGuiViewportUI.Current.SelectedEntity.GUID.ToString()))
                {
                    return file;
                }
            }

            throw new Exception("Entity was not found");
        }

        public static void SetEntityMetaFields(Entity entity, string fieldName, object value)
        {
            var file = Directory.GetFiles(HiddenAssetDirectory).Where(t => t.Contains(entity.GUID.ToString())).Where(t => t.EndsWith(ENTITY_METAFILE)).ToList()[0];
            byte[] bytes = File.ReadAllBytes(file);

            var metaFile = JsonConvert.DeserializeObject<EntityMetaFile>(Encoding.UTF8.GetString(bytes));
            metaFile.SerializedFields.Remove(fieldName);
            metaFile.SerializedFields.Add(fieldName, value);

            var newBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(metaFile));
            File.WriteAllBytes(file, newBytes);
        }

        public static object GetEntityFieldValue(Entity entity, string fieldName)
        {
            var file = Directory.GetFiles(HiddenAssetDirectory).Where(t => t.Contains(entity.GUID.ToString())).Where(t => t.EndsWith(ENTITY_METAFILE)).ToList()[0];
            byte[] bytes = File.ReadAllBytes(file);

            var metaFile = JsonConvert.DeserializeObject<EntityMetaFile>(Encoding.UTF8.GetString(bytes));
            if (metaFile.SerializedFields.ContainsKey(fieldName))
            {
                return metaFile.SerializedFields[fieldName];
            }
            else
            {
                return default;
            }
        }
    }

    public static class AssetSerializer
    {
        public static byte[] SerializeAsset(bool value)
        {
            return BitConverter.GetBytes(value);
        }
        public static byte[] SerializeAsset(int value)
        {
            return BitConverter.GetBytes(value);
        }
        public static byte[] SerializeAsset(float value)
        {
            return BitConverter.GetBytes(value);
        }
        public static byte[] SerializeAsset(OpenTK.Mathematics.Vector3 value)
        {
            byte[] bytesX = BitConverter.GetBytes(value.X);
            byte[] bytesY = BitConverter.GetBytes(value.Y);
            byte[] bytesZ = BitConverter.GetBytes(value.Z);

            return CombineMany(bytesX, bytesY, bytesZ);
        }
        public static byte[] SerializeAsset(OpenTK.Mathematics.Vector3i value)
        {
            byte[] bytesX = BitConverter.GetBytes(value.X);
            byte[] bytesY = BitConverter.GetBytes(value.Y);
            byte[] bytesZ = BitConverter.GetBytes(value.Z);

            return CombineMany(bytesX, bytesY, bytesZ);
        }
        public static byte[] SerializeAsset(OpenTK.Mathematics.Vector2 value)
        {
            byte[] bytesX = BitConverter.GetBytes(value.X);
            byte[] bytesY = BitConverter.GetBytes(value.Y);

            return CombineMany(bytesX, bytesY);
        }
        public static byte[] SerializeAsset(OpenTK.Mathematics.Vector2i value)
        {
            byte[] bytesX = BitConverter.GetBytes(value.X);
            byte[] bytesY = BitConverter.GetBytes(value.Y);

            return CombineMany(bytesX, bytesY);
        }
        public static byte[] SerializeAsset(EngineCore.Vector3 value)
        {
            byte[] bytesX = BitConverter.GetBytes(value.X);
            byte[] bytesY = BitConverter.GetBytes(value.Y);
            byte[] bytesZ = BitConverter.GetBytes(value.Z);

            return CombineMany(bytesX, bytesY, bytesZ);
        }
        public static byte[] SerializeAsset(EngineCore.Vector3Int value)
        {
            byte[] bytesX = BitConverter.GetBytes(value.X);
            byte[] bytesY = BitConverter.GetBytes(value.Y);
            byte[] bytesZ = BitConverter.GetBytes(value.Z);

            return CombineMany(bytesX, bytesY, bytesZ);
        }
        public static byte[] SerializeAsset(Entity entity)
        {
            var sEntity = new SerializedEntity(entity);
            var eBytes = new byte[Marshal.SizeOf<SerializedEntity>()];

            MemoryMarshal.Write(eBytes, in sEntity);
            return eBytes.ToArray();
        }

        public static Entity DeSerializeAsset(byte[] bytes)
        {
            if (bytes.Length != Marshal.SizeOf<SerializedEntity>())
            {
                throw new Exception($"Invalid byte array size: {bytes.Length}, expected {Marshal.SizeOf<SerializedEntity>()}.");
            }

            var dsEntity = MemoryMarshal.Read<SerializedEntity>(new Span<byte>(bytes));
            Tags[] tagArray = [dsEntity.Tag0, dsEntity.Tag1, dsEntity.Tag2, dsEntity.Tag3];

            unsafe
            {
                var esBytes = new byte[12];
                for (int i = 0; i < 12; i++)
                {
                    esBytes[i] = dsEntity.NameBytes[i];
                }

                var entity = new Entity(Encoding.UTF8.GetString(esBytes, 0, dsEntity.NameLength), dsEntity.GUID, tagArray);
                List<Behaviour> behaviours = new List<Behaviour>();

                foreach (var script in EditorInspector.LoadedScripts)
                {
                    string className = script.name.Replace(".cs", "");
                    Type foundType = null;

                    var metaFilePath = AssetDataBase.HiddenAssetDirectory + dsEntity.GUID.ToString() + AssetDataBase.ENTITY_METAFILE;
                    if (!File.Exists(metaFilePath))
                    {
                        Console.WriteLine($"Warning: Meta file not found at {metaFilePath}");
                        continue;
                    }

                    var metaFileText = Encoding.UTF8.GetString(File.ReadAllBytes(metaFilePath));
                    var metaFile = JsonConvert.DeserializeObject<EntityMetaFile>(metaFileText);

                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        foundType = assembly.GetTypes().FirstOrDefault(t => t.Name == className);
                        if (foundType != null)
                            break;
                    }

                    if (foundType == null)
                    {
                        Console.WriteLine($"Warning: Type '{className}' not found in loaded assemblies.");
                        continue;
                    }

                    if (metaFile.Scripts.Any(t => t.name.Contains(foundType.Name)))
                    {
                        if (!typeof(Behaviour).IsAssignableFrom(foundType))
                        {
                            Console.WriteLine($"Warning: Type '{foundType.Name}' is not a Behaviour.");
                            continue;
                        }
                        behaviours.Add((Behaviour)Activator.CreateInstance(foundType));
                    }
                }

                if (behaviours.Count > 0)
                {
                    if (entity == null)
                        throw new Exception("Entity is null during deserialization.");

                    var eType = entity.GetType();
                    PropertyInfo info = eType.GetProperty("Behaviours", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (info == null)
                    {
                        throw new Exception($"Field 'Behaviours' not found in type '{eType.Name}'.");
                    }

                    info.SetValue(entity, behaviours);
                }
                Console.WriteLine(behaviours.Count);

                return entity;
            }
        }

        private static T[] CombineMany<T>(params T[][] arrays)
        {
            int totalLength = arrays.Sum(arr => arr.Length);
            T[] result = new T[totalLength];
            int offset = 0;

            foreach (var arr in arrays)
            {
                Array.Copy(arr, 0, result, offset, arr.Length);
                offset += arr.Length;
            }

            return result;
        }
    }

    //A struct representing a serialized unmanaged entity
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SerializedEntity
    {
        public fixed byte NameBytes[12];
        public int NameLength;
        public Guid GUID;

        public Tags Tag0, Tag1, Tag2, Tag3;

        public SerializedEntity(Entity entity)
        {
            Tag0 = entity.Tags[0];
            Tag1 = entity.Tags[1];
            Tag2 = entity.Tags[2];
            Tag3 = entity.Tags[3];

            GUID = entity.GUID;
            string name = entity.Name ?? "";
            byte[] tempBytes = Encoding.UTF8.GetBytes(name);
            NameLength = Math.Min(tempBytes.Length, 12);

            fixed (byte* namePtr = NameBytes)
            {
                for (int i = 0; i < NameLength; i++)
                    namePtr[i] = tempBytes[i];
            }
        }
    }

    public record struct ScriptLoad(string name, string path);
}