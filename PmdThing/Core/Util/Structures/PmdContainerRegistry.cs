using PmdContainerId = System.Int32;

namespace PmdThing.Core.Util.Structures
{
    public class PmdContainerRegistry
    {

        public struct PmdContainerEntry
        {
            public Type Type;
            public PmdContainerId Id;
        }

        private readonly Dictionary<Type, List<PmdContainerId>> _typeMap = new();

        public void Add(Type componentType, PmdContainerId idCounter)
        {

            if (!componentType.IsSubclassOf(typeof(PmdContainer)) && componentType != typeof(PmdContainer)) throw new ArgumentException(
                "Type passed in is not a container");

            if (_typeMap.TryGetValue(componentType, out var ints))
            {
                _typeMap[componentType].Add(idCounter);
            }
            else
            {
                List<PmdContainerId> componentList = [idCounter];
                _typeMap[componentType] = componentList;
            }

        }

        public PmdContainerId GetFirst(Type componentType)
        {
            return GetAll(componentType)[0];
        }

        public bool Contains(Type componentType)
        {
            return _typeMap.ContainsKey(componentType);
        }

        public void RemoveAll(Type componentType)
        {
            if (!componentType.IsSubclassOf(typeof(PmdContainer))) throw new ArgumentException(
                "Type passed in is not a container");
            _typeMap.Remove(componentType);
        }

        public void Remove(Type componentType, PmdContainerId id)
        {

            if (!componentType.IsSubclassOf(typeof(PmdContainer))) throw new ArgumentException(
                "Type passed in is not a container");

            if (_typeMap.TryGetValue(componentType, out var ints))
            {
                _typeMap[componentType].Remove(id);

                if (_typeMap[componentType].Count == 0)
                {
                    _typeMap.Remove(componentType);
                }
            }

        }

        public List<PmdContainerId> GetAll(Type componentType)
        {
            return _typeMap.TryGetValue(componentType, out var ints)
                ? ints
                : throw new ArgumentException("Component type has not been registered");
        }


        public List<PmdContainerEntry> GetAll()
        {
            List<PmdContainerEntry> entries = [];
            foreach (var mapEntry in _typeMap)
            {
                foreach (PmdContainerId id in mapEntry.Value)
                {
                    PmdContainerEntry containerEntry;
                    containerEntry.Id = id;
                    containerEntry.Type = mapEntry.Key;
                    entries.Add(containerEntry);
                }
            }
            return entries;
        }

        

    } 

}
