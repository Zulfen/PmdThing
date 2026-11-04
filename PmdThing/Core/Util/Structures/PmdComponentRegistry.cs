using System.Collections.Concurrent;
using PmdComponentId = System.Int32;

namespace PmdThing.Core.Util.Structures
{
    public class PmdComponentRegistry
    {

        public struct PmdComponentEntry
        {
            public Type Type;
            public PmdComponentId Id;
        }

        private readonly ConcurrentDictionary<Type, List<PmdComponentId>> _typeMap = new();

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
            _typeMap.Remove(componentType, out _);
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
                    _typeMap.Remove(componentType, out _);
                }
            }

        }

        public List<PmdComponentId> GetAll(Type componentType)
        {
            //return _typeMap.TryGetValue(componentType, out var ints)
            //    ? ints
            //    : throw new ArgumentException($"Component type {componentType.Name} has not been registered");

            if (_typeMap.TryGetValue(componentType, out var ints))
            {
                return ints;
            }

            var subclasses = _typeMap
                .Where(entry => entry.Key.IsSubclassOf(componentType))
                .SelectMany(entry => entry.Value)
                .ToList();

            return subclasses.Count == 0 ? throw new ArgumentException($"Component type {componentType.Name} has not been registered") : subclasses;
        }


        public List<PmdComponentEntry> GetAll()
        {
            List<PmdComponentEntry> entries = [];
            foreach (var mapEntry in _typeMap)
            {
                foreach (var id in mapEntry.Value)
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
