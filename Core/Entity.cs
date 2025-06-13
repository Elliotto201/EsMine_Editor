using EngineCore;
using EngineInternal;
using System;
using System.Collections.Generic;

namespace EngineCore
{
    public sealed class Entity : IInspectorGUI, IEquatable<Entity>
    {
        private static readonly HashSet<Guid> UsedGUIDs = new();
        public Tags[] Tags = new Tags[4];

        public string Name { get; set; }
        public Guid GUID { get; }

        public Entity(string name)
        {
            Name = name;
            var guid = Guid.NewGuid();

            while (UsedGUIDs.Contains(guid))
                guid = Guid.NewGuid();


            GUID = guid;
            UsedGUIDs.Add(guid);
            Tags = new Tags[4];
        }

        public Entity(string name, Guid guid, Tags[] tags)
        {
            Name = name;
            GUID = guid;
            Tags = tags;
            UsedGUIDs.Add(guid);
        }

        public T GetBehaviour<T>() where T : Behaviour
        {
            return EntityManager.GetBehaviour<T>(GUID);
        }

        public bool HasBehaviour<T>() where T : Behaviour
        {
            return EntityManager.HasBehaviour<T>(GUID);
        }

        public void DrawInspector()
        {
            
        }

        public bool Equals(Entity other)
        {
            return other.GUID.Equals(GUID);
        }

        public override bool Equals(object obj)
        {
            if(typeof(Entity) == (Type)obj)
            {
                if (((Entity)obj).GUID.Equals(GUID)) return true;
                return false;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return GUID.GetHashCode();
        }
    }
}

namespace EngineInternal
{
    internal static class EntityManager
    {
        private static readonly Dictionary<Guid, List<Behaviour>> EntityBehaviours = new();

        public static T GetBehaviour<T>(Guid guid) where T : Behaviour
        {
            if (!EntityBehaviours.TryGetValue(guid, out var list))
                throw new InvalidOperationException($"Entity with GUID {guid} has no components.");

            foreach (var behaviour in list)
            {
                if (behaviour is T match)
                    return match;
            }

            throw new InvalidOperationException($"Entity does not contain a component of type: {typeof(T)}");
        }

        public static bool HasBehaviour<T>(Guid guid) where T : Behaviour
        {
            if (!EntityBehaviours.TryGetValue(guid, out var list))
                return false;

            foreach (var behaviour in list)
            {
                if (behaviour is T match)
                    return true;
            }

            return false;
        }
    }
}