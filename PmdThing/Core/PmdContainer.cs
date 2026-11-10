using PmdThing.Components.Networking.Rokon;
using PmdThing.Core.Hardware.Framework;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using PmdThing.Core.Util.Structures;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;

namespace PmdThing.Core
{
    public class PmdContainer(PmdContainerConfig config)
    {

        protected readonly PmdGameBase Base = config.Base;
        protected readonly PmdFramework Framework = config.Framework;
        public readonly PmdMultiContainer? Parent = config.Parent;
        public readonly PmdContainerId Id = config.Id; 

        private readonly PmdComponentRegistry _localTypes = new();

        private readonly Dictionary<Type, PmdContainerId> _properties = new();

        public bool AsyncInit = false;

        public virtual Task Init()
        {
            return Task.CompletedTask;
        }

        public PmdComponent GetComponent(Type componentType)
        {
            var id = _localTypes.GetFirst(componentType);
            return Base.GetRuntimeComponent(id);
        }

        public PmdComponentId GetFirstId(Type componentType)
        {
            return _localTypes.GetFirst(componentType);
        }

        public TComponent GetComponent<TComponent>() where TComponent : PmdComponent
        {
            var id = _localTypes.GetFirst(typeof(TComponent));
            return (TComponent)Base.GetRuntimeComponent(id);
        }

        public TComponent? GetIfExists<TComponent>(bool unique) where TComponent : PmdComponent
        {
            if (!ComponentExists(typeof(TComponent), unique))
            {
                return null;
            }

            var id = unique
                ? _localTypes.GetFirst(typeof(TComponent))
                : Base.GetSharedComponentId(typeof(TComponent));
            return GetOtherComponent<TComponent>(id);
        }

        protected TComponent GetOtherComponent<TComponent>(PmdComponentId id) where TComponent : PmdComponent
        {
            return (TComponent)Base.GetRuntimeComponent(id);
        }

        protected PmdComponent GetOtherComponent(PmdComponentId id)
        {
            return Base.GetRuntimeComponent(id);
        }

        public List<PmdComponentRegistry.PmdComponentEntry> GetAllComponents()
        {
            return _localTypes.GetAll();
        }

        public PmdComponentId RegisterComponent<TComponent>(bool unique, params object[] arguments) where TComponent : PmdComponent
        {
            return RegisterComponent(null, typeof(TComponent), unique, arguments);
        }

        public PmdComponentId RegisterComponent(PmdComponentId? id, Type componentType, bool unique, params object[] arguments)
        {
            var returnedId = Base.RegisterComponent(id, componentType, this, unique, out _, arguments);
            _localTypes.Add(componentType, returnedId);
            return returnedId;
        }

        public bool ComponentExists(Type componentType, bool unique)
        {
            return unique
                ? _localTypes.Contains(componentType)
                : Base.ComponentExists(componentType);
        }

        public PmdComponentId RegisterComponent<TComponent>(bool unique,
                                                            out TComponent instance,
                                                            params object[] arguments) where TComponent : PmdComponent
        {
            var id = Base.RegisterComponent(unique ? this : null, unique, out instance, arguments);
            _localTypes.Add(typeof(TComponent), id);
            return id;
        }

        public PmdComponentId RegisterSyncedComponent<TComponent>(bool unique,
            out TComponent instance, 
            string tag,
            params object[] arguments) where TComponent : PmdComponent
        {
            var id = RegisterComponent<TComponent>(unique, out var component, arguments);
            instance = component;

            PmdNetworkHandler.Instance?.RegisterSyncedComponent(id, component, unique, this, tag);
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
                id = unique ? _localTypes.GetFirst(typeof(TComponent)) : Base.GetSharedComponentId(typeof(TComponent));
                instanceOut = (TComponent)Base.GetRuntimeComponent(id);
            }
            else
            {
                id = RegisterComponent(unique, out instanceOut, arguments);
            }

            instance = instanceOut;
            return id;
        }

        public PmdComponentId RegisterSyncedIfNotExist<TComponent>(bool unique,
            string tag,
            out TComponent instance,
            params object[] arguments) where TComponent : PmdComponent
        {
            var id = RegisterIfNotExist<TComponent>(unique, out var component, arguments);
            instance = component;

            PmdNetworkHandler.Instance?.RegisterSyncedComponent(id, component, unique, this, tag);
            return id;
        }


        public PmdComponentId RegisterIfNotExist<TComponent>(bool unique, params object[] arguments) where TComponent : PmdComponent
        {
            var registered = ComponentExists(typeof(TComponent), unique);

            var id = registered ? _localTypes.GetFirst(typeof(TComponent)) : RegisterComponent<TComponent>(unique, arguments);
            return id;
        }

        /// <summary>
        /// Shorthand for creating listenable components programatically.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="unique"></param>
        /// <returns></returns>
        public PmdComponentId RegisterProperty<T>(T value, bool unique = true)
        {
            // Probably a nicer way to do this.
            var config = new PmdComponentConfig
            {
                Framework = Framework,
                Base = Base,
                Parent = this,
            };

            var listenableComponent = new PmdListenableComponent<T>(config, value);
            var id = Base.RegisterComponent(null, listenableComponent, unique);

            _properties.Add(typeof(T), id);
            _localTypes.Add(listenableComponent.GetType(), id);
            return id;
        }

        public PmdComponentId RegisterSyncedProperty<T>(T value, string tag, bool unique = true)
        {
            // Probably a nicer way to do this.
            var config = new PmdComponentConfig
            {
                Framework = Framework,
                Base = Base,
                Parent = this,
            };

            var listenableComponent = new PmdListenableComponent<T>(config, value);
            var id = Base.RegisterComponent(null, listenableComponent, unique);
            PmdNetworkHandler.Instance?.RegisterSyncedComponent(id, listenableComponent, unique, this, tag);

            _properties.Add(typeof(T), id);
            _localTypes.Add(listenableComponent.GetType(), id);
            return id;
        }

        public T GetProperty<T>(bool unique = true)
        {
            if (_properties.TryGetValue(typeof(T), out var id))
            {
                var listenableComponent = (PmdListenableComponent<T>)Base.GetRuntimeComponent(id);
                return listenableComponent.ComponentValue;
            }

            throw new ArgumentException($"Property of type {typeof(T)} was not registered");
        }

        public bool HasProperty<T>()
        {
            return _properties.ContainsKey(typeof(T));
        }

        public void Deregister<TComponent>(PmdComponentId id, bool unique)
        {
            _localTypes.Remove(typeof(TComponent), id);
            Base.DeregisterComponent(typeof(TComponent), id, unique);
        }

        private void _InternalDestroy()
        {
            _localTypes.GetAll()
                .ForEach(entry => Base.DeregisterComponent(entry.Type, entry.Id, false));
        }

        public bool IsOnServer()
        {
            return Base.Server != null;
        }

        ~PmdContainer()
        {
            _InternalDestroy();
        }

    }
}
