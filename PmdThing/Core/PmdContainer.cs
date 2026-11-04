using PmdThing.Core.Hardware.Framework;
using PmdThing.Core.Util;
using PmdComponentId = System.Int32;

namespace PmdThing.Core
{
    public class PmdContainer
    {

        private readonly PmdGameBase _gameBase = PmdGameBase.GetInstance();
        protected readonly PmdFramework Framework = PmdFrameworkFactory.GetInstance();
        private readonly PmdComponentRegistry _localTypes = new();
        
        public PmdComponent GetComponent(Type componentType)
        {
            var id = _localTypes.GetFirst(componentType);
            return _gameBase.GetRuntimeComponent(id);
        }

        public TComponent GetComponent<TComponent>() where TComponent : PmdComponent
        {
            var id = _localTypes.GetFirst(typeof(TComponent));
            return (TComponent)_gameBase.GetRuntimeComponent(id);
        }

        public TComponent? GetIfExists<TComponent>(bool unique) where TComponent : PmdComponent
        {
            if (!ComponentExists(typeof(TComponent), unique))
            {
                return null;
            }

            var id = unique
                ? _localTypes.GetFirst(typeof(TComponent))
                : _gameBase.GetSharedComponentId(typeof(TComponent));
            return GetOtherComponent<TComponent>(id);
        }

        protected TComponent GetOtherComponent<TComponent>(PmdComponentId id) where TComponent : PmdComponent
        {
            return (TComponent)_gameBase.GetRuntimeComponent(id);
        }

        public List<PmdComponentRegistry.PmdComponentEntry> GetAllComponents()
        {
            return _localTypes.GetAll();
        }

        public PmdComponentId RegisterComponent<TComponent>(bool unique, params object[] arguments) where TComponent : PmdComponent
        {
            var id = _gameBase!.RegisterComponent<TComponent>(unique ? this : null, unique, out _, arguments);
            _localTypes.Add(typeof(TComponent), id);
            return id;
        }

        private bool ComponentExists(Type componentType, bool unique)
        {
            return unique
                ? _localTypes.Contains(componentType)
                : _gameBase.HasComponent(componentType);
        }

        public PmdComponentId RegisterComponent<TComponent>(bool unique,
                                                            out TComponent instance,
                                                            params object[] arguments) where TComponent : PmdComponent
        {
            var id = _gameBase!.RegisterComponent(unique ? this : null, unique, out instance, arguments);
            _localTypes.Add(typeof(TComponent), id);
            return id;
        }

        public PmdComponentId RegisterIfNotExist<TComponent>(bool unique,
                                                             out TComponent instance,
                                                             params object[] arguments) where TComponent : PmdComponent
        {
            PmdComponentId id;
            TComponent instanceOut;
            if (ComponentExists(typeof(TComponent), unique))
            {
                id = _localTypes.GetFirst(typeof(TComponent));
                instanceOut = (TComponent)_gameBase!.GetRuntimeComponent(id);
            }
            else
            {
                id = RegisterComponent(unique, out instanceOut, arguments);
            }

            instance = instanceOut;
            return id;
        }

        public PmdComponentId RegisterIfNotExist<TComponent>(bool unique, params object[] arguments) where TComponent : PmdComponent
        {
            var registered = ComponentExists(typeof(TComponent), unique);

            var id = registered ? _localTypes.GetFirst(typeof(TComponent)) : RegisterComponent<TComponent>(unique, arguments);
            return id;
        }

        public void Deregister<TComponent>(PmdComponentId id, bool unique)
        {
            _localTypes.Remove(typeof(TComponent), id);
            _gameBase.DeregisterComponent(typeof(TComponent), id, unique);
        }

        public void Deregister<TComponent>(bool unique)
        {
            var registered = ComponentExists(typeof(TComponent), unique);
            if (!registered) return;

            var id = unique ? _localTypes.GetFirst(typeof(TComponent)) : _gameBase.GetSharedComponentId(typeof(TComponent));
            Deregister<TComponent>(id, unique);
            
        }

        private void _InternalDestroy()
        {
            _localTypes.GetAll()
                .ForEach(entry => _gameBase!.DeregisterComponent(entry.Type, entry.Id, false));
        }

        ~PmdContainer()
        {
            _InternalDestroy();
        }

    }
}
