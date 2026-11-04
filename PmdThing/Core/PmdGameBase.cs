using System.Collections.Concurrent;
using PmdThing.Components;
using PmdThing.Core.Hardware.Framework;
using PmdThing.Core.Util;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;


namespace PmdThing.Core
{
    public class PmdGameBase
    {


        private enum PmdComponentAction
        {
            Add,
            Remove
        }

        private class PmdQueuedComponent
        {
            public required PmdComponentAction Action { get; init; }
            public required PmdComponent Instance { get; init; }
        }

        private static PmdGameBase? _instance;
        private readonly PmdFramework _framework;

        // TODO: add update / render sorting
        // TODO: maybe add a way to listen for another component to update
        // MovementController?

        private PmdComponentId _componentCounter;
        private PmdContainerId _containerCounter;

        private readonly PmdComponentRegistry _allComponents = new();
        private readonly PmdComponentRegistry _sharedComponents = new();

        private readonly Dictionary<PmdComponentId, PmdComponent> _runtimeComponents = new();

        private readonly ConcurrentQueue<PmdQueuedComponent> _componentActions = [];
        private readonly List<PmdComponent> _processQueue = [];

        private readonly Dictionary<PmdContainerId, PmdContainer> _runtimeContainers = new();

        private PmdContainer? _sharedContainer;

        public PmdGameBase(PmdFramework framework)
        {
            _instance = this;
            _framework = framework;
            Init();
        }

        private void Init()
        {
            RegisterContainer(out _sharedContainer);
            RegisterComponent<TiledWorld>(_sharedContainer, false, out _, 24);
            RegisterComponent<Components.Timer>(_sharedContainer, false, out _, (double)1000 / 30, ProcessingOrder.Update);
        }

        public static PmdGameBase Create(int width, int height, string title)
        {
            var framework = PmdFrameworkFactory.Create(width, height, title);
            return new PmdGameBase(framework);
        }

        public static PmdGameBase GetInstance()
        {
            if (_instance == null) throw new ApplicationException("Could not get GameBase. Is it initialised?");
            return _instance;
        }

        private PmdComponentId RegisterComponent(PmdComponent component, bool unique = true)
        {
            _componentCounter++;

            _allComponents.Add(component.GetType(), _componentCounter);
            if (!unique) _sharedComponents.Add(component.GetType(), _componentCounter);

            var queuedComponent = new PmdQueuedComponent()
            {
                Action = PmdComponentAction.Add,
                Instance = component
            };
            _componentActions.Enqueue(queuedComponent);

            _runtimeComponents.Add(_componentCounter, component);

            component.Priority = _componentCounter;

            return _componentCounter;
        }

        /// <summary>
        /// Registers a component
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <param name="container">Container that owns this component. Can be null if it shouldn't belong to one.</param>
        /// <param name="unique">If false, this registers the component to be shared globally.</param>
        /// <param name="arguments">Any extra arguments to your component</param>
        /// <param name="instance">Returned instance of component, if successful.</param>
        /// <returns>Component Id</returns>
        public PmdComponentId RegisterComponent<TComponent>(PmdContainer? container,
                                                            bool unique,
                                                            out TComponent instance,
                                                            params object[] arguments) where TComponent : PmdComponent
        {
            var runtimeComponentFunc = GetRuntimeComponent;
            var containerComponentFunc = GetContainerComponent;
            var sharedComponentFunc = GetSharedComponent;

            var fullParams = new object[arguments.Length + 1];
            arguments.CopyTo(fullParams, 1);

            var componentConfig = new PmdComponentConfig
            {
                OtherComponent = runtimeComponentFunc,
                ContainerComponent = containerComponentFunc,
                SharedComponent = sharedComponentFunc,
                Parent = container,
            };

            fullParams[0] = componentConfig;

            var componentObject = Activator.CreateInstance(typeof(TComponent), fullParams);
            if (componentObject != null && componentObject.GetType() == typeof(TComponent))
            {
                var component = (TComponent)componentObject;
                instance = component;
                return RegisterComponent(component, unique);
            }

            throw new InvalidCastException($"Could not create valid instance of class");

        }

        public void DeregisterComponent(Type componentType, PmdComponentId id, bool unique = true)
        {
            if (!_runtimeComponents.TryGetValue(id, out var component)) return;
            component.Cleanup();

            _allComponents.Remove(componentType, id);
            _runtimeComponents.Remove(id);

            var queuedComponent = new PmdQueuedComponent()
            {
                Action = PmdComponentAction.Remove,
                Instance = component
            };
            _componentActions.Enqueue(queuedComponent);

            if (!unique) _sharedComponents.Remove(componentType, id);

        }

        public PmdComponent GetRuntimeComponent(PmdComponentId id)
        {
            return _runtimeComponents.TryGetValue(id, out var component) ? component : throw new ArgumentException($"Component with ID {id} has not been registered.");
        }

        public PmdComponent GetSharedComponent(Type componentType)
        {
            var id = _sharedComponents.GetFirst(componentType);
            return _runtimeComponents[id];
        }

        public PmdComponentId GetSharedComponentId(Type componentType)
        {
            return _sharedComponents.GetFirst(componentType);
        }

        public PmdComponent? GetContainerComponent(Type componentType, PmdContainerId id)
        {
            return _runtimeContainers.TryGetValue(id, out var container) ? container.GetComponent(componentType) : null;
        }

        public PmdContainerId RegisterContainer<TContainer>(out TContainer? instance, params object[] arguments) where TContainer : PmdContainer
        {

            var containerInstance = Activator.CreateInstance(typeof(TContainer), arguments);
            if (containerInstance is TContainer container)
            {
                instance = container;
                _runtimeContainers[_containerCounter] = container;
                _containerCounter++;
                return _containerCounter;
            }

            throw new ArgumentException(
                $"Container {typeof(TContainer)} could not be initialised with the following arguments: {arguments}");

        }

        public bool HasComponent(Type componentType)
        {
            return _allComponents.Contains(componentType);
        }

        public void DeregisterContainer(PmdComponentId id)
        {
            if (_runtimeContainers.Remove(id, out var container))
            {
                container.GetAllComponents()
                    .ForEach(entry => DeregisterComponent(entry.Type, entry.Id, false));
                _containerCounter--;
            }
        }

        public List<TComponent> GetAllComponents<TComponent>() where TComponent : PmdComponent
        {
            var componentIds = _allComponents.GetAll(typeof(TComponent));
            return componentIds
                .Select(id => _runtimeComponents[id])
                .OfType<TComponent>()
                .ToList();
        }

        public PmdComponentId GetNextId()
        {
            return _componentCounter;
        }

        public void Shutdown()
        {
            _framework.Running = false;
        }

        public void MainLoop()
        {

            while (_framework.Running)
            {
                _framework.Graphics.PreUpdate();
                _framework.Update();


                // Done to modify the collection of components whilst not iterating over them
                if (_componentActions.TryDequeue(out var queuedComponent))
                {

                    switch (queuedComponent.Action)
                    {
                        case PmdComponentAction.Add:
                            _processQueue.Add(queuedComponent.Instance);
                            break;
                        case PmdComponentAction.Remove:
                            _processQueue.Remove(queuedComponent.Instance);
                            break;
                        default:
                            throw new InvalidOperationException("Not supported");
                    }

                }

                foreach (var component in _processQueue)
                {
                    if (!component.ShouldUpdate) continue;

                    component.GetNextUpdateTask()?.Invoke();
                    component.Update();

                }

                _framework.Graphics.PreRender();

                foreach (var component in _processQueue)
                {
                    if (!component.DoesRendering) continue;

                    component.GetNextRenderTask()?.Invoke();
                    if (component.DoesRendering) component.Render();
                }

                _framework.Graphics.PostRender();

            }


        }

        public void Start()
        {
            MainLoop();
        }


    }
}
