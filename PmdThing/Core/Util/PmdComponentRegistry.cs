using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using PmdComponentId = System.Int32;

namespace PmdThing.Core.Util
{
    public class PmdComponentRegistry
    {

        public struct PmdComponentEntry
        {
            public Type Type;
            public PmdComponentId Id;
        }

        private readonly Dictionary<Type, List<PmdComponentId>> _typeMap = new();

        public void Add(Type componentType, PmdComponentId idCounter)
        {

            if (!componentType.IsSubclassOf(typeof(PmdComponent))) throw new ArgumentException(
                "Type passed in is not a component");

            if (_typeMap.TryGetValue(componentType, out var ints))
            {
                _typeMap[componentType].Add(idCounter);
            }
            else
            {
                List<PmdComponentId> componentList = [idCounter];
                _typeMap[componentType] = componentList;
            }

        }

        public PmdComponentId GetFirst(Type componentType)
        {
            return GetAll(componentType)[0];
        }

        public bool Contains(Type componentType)
        {
            return _typeMap.ContainsKey(componentType);
        }

        public void RemoveAll(Type componentType)
        {
            if (!componentType.IsSubclassOf(typeof(PmdComponent))) throw new ArgumentException(
                "Type passed in is not a component");
            _typeMap.Remove(componentType);
        }

        public void Remove(Type componentType, PmdComponentId id)
        {

            if (!componentType.IsSubclassOf(typeof(PmdComponent))) throw new ArgumentException(
                "Type passed in is not a component");

            if (_typeMap.TryGetValue(componentType, out var ints))
            {
                _typeMap[componentType].Remove(id);

                if (_typeMap[componentType].Count == 0)
                {
                    _typeMap.Remove(componentType);
                }
            }

        }

        public List<PmdComponentId> GetAll(Type componentType)
        {
            return _typeMap.TryGetValue(componentType, out var ints)
                ? ints
                : throw new ArgumentException("Component type has not been registered");
        }


        public List<PmdComponentEntry> GetAll()
        {
            List<PmdComponentEntry> entries = [];
            foreach (var mapEntry in _typeMap)
            {
                foreach (PmdComponentId id in mapEntry.Value)
                {
                    PmdComponentEntry componentEntry;
                    componentEntry.Id = id;
                    componentEntry.Type = mapEntry.Key;
                    entries.Add(componentEntry);
                }
            }
            return entries;
        }

        

    } 

}
