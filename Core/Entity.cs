using EngineCore;
using EngineInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace EngineCore
{
    public sealed class Entity : IEquatable<Entity>
    {
        private static readonly HashSet<Guid> UsedGUIDs = new();
        public List<Behaviour> Behaviours { get; private set; } = new();

        //Tags are kind of like unity layers. They are a form of identification. You can have up to 4 tags. Theese are/will be configurable in the editor in the future
        internal Tags[] Tags = new Tags[4];

        public string Name { get; set; }
        public Guid GUID { get; }

        //Transformation Data
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

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

        public T GetBehaviour<T>(T value) where T : Behaviour
        {
            var behaviour = Behaviours.Find(t => t.GetType() == typeof(T));

            if (behaviour != null)
            {
                return (T)behaviour;
            }

            throw new Exception($"Entity with name {Name} doesn't have component of type {typeof(T)}");
        }

        public bool HasBehaviour<T>(T value) where T : Behaviour
        {
            return Behaviours.Any(t => t.GetType() == value.GetType());
        }

        public void AddComponent<T>(T value) where T : Behaviour
        {
            if(!Behaviours.Any(t => t.GetType() == typeof(T)))
            {
                Behaviours.Add(value);
            }
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


        //Will remove in future. Only temporary
        public void SetBehaviours(List<Behaviour> behaviours)
        {
            Behaviours = behaviours;
        }
    }
}