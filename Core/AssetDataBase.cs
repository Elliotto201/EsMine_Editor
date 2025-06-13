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

namespace EngineInternal
{
    public static class AssetDataBase
    {
        const string HIEARCHY_ENTITY = ".sEntity";

        private static string AssetDirectory;
        public static Action AssetRefresh;

        static AssetDataBase()
        {
            AssetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            Directory.CreateDirectory(AssetDirectory);

            AssetDirectory += "/";
        }

        public static void CreateEntityHiearchy()
        {
            if(Window.BuildWindow.GameType == GameWindowType.Editor)
            {
                var entity = new Entity("New Entity");

                using (var fs = File.Create(AssetDirectory + entity.GUID.ToString() + HIEARCHY_ENTITY))
                {
                    var bytes = AssetSerializer.SerializeAsset(entity);
                    fs.Write(bytes);
                }

                AssetRefresh?.Invoke();
            }
        }

        //deletes a Hiearchy Entity from memory and disk
        public static void DeleteEntityHiearch(Guid entityGuid)
        {
            if(Window.BuildWindow.GameType == GameWindowType.Editor)
            {
                File.Delete(AssetDirectory + entityGuid.ToString() + HIEARCHY_ENTITY);

                AssetRefresh?.Invoke();
            }
        }

        //Used for getting all entities that are in the scene hiearchy. Used for well getting all entities
        public static List<Entity> LoadAllSceneEntities()
        {
            List<Entity> entities = new List<Entity>();

            if (Window.BuildWindow.GameType == GameWindowType.Editor)
            {
                foreach(var file in Directory.GetFiles(AssetDirectory))
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
            var dsEntity = MemoryMarshal.Read<SerializedEntity>(new Span<byte>(bytes));

            Tags[] tagArray = [dsEntity.Tag0, dsEntity.Tag1, dsEntity.Tag2, dsEntity.Tag3];

            unsafe
            {
                //unsafe to safe
                var esBytes = new byte[12];

                for(int i = 0; i < 12; i++)
                {
                    esBytes[i] = dsEntity.NameBytes[i];
                }

                return new Entity(Encoding.UTF8.GetString(esBytes, 0, dsEntity.NameLength), dsEntity.GUID, tagArray);
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