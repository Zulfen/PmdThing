using PmdThing.Core.Hardware.Framework;
using PmdThing.Core.Util;
using System.Collections.Concurrent;
using PmdThing.Core.Network;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;

namespace PmdThing.Core
{
    public abstract class PmdComponent(PmdComponentConfig config) : IComparable<PmdComponent>, IDisposable
    {

        // re-add init method
        // ReSharper disable UseSymbolAlias :3

        // can be null
        private readonly PmdContainer? _parent = config.Parent;
        public int Priority { get; set; } = config.Priority;

        #pragma warning restore IDE0044
        public bool ShouldUpdate = true;
        public bool DoesRendering = false;
        public bool HandlesPostRender = false;
        public bool AsyncInit = false;
        private bool _disposedValue;

        private readonly ConcurrentQueue<Action> _updateTasks = new();
        private readonly ConcurrentQueue<Action> _renderTasks = new();

        protected PmdGameBase Base => config.Base;
        protected PmdFramework Framework => config.Framework;

        protected IPmdGameServer? Server => Base.Server;
        protected IPmdGameClient? Client => Base.Client;

        public int CompareTo(PmdComponent? other)
        {
            return other?.Priority.CompareTo(Priority) ?? 0;
        }

        public virtual Task Init()
        {
            return Task.CompletedTask;
        }

        public virtual void Update()
        {
        }

        public virtual void Render()
        {
        }

        public virtual void PostRender()
        {
        }

        public abstract void Cleanup();

        /// <summary>
        /// Submits a task to be run on the main thread.
        /// </summary>
        /// <param name="task">Action to run</param>
        /// <param name="order">Whether it should be processed before updating or drawing.</param>
        public void SubmitTask(Action task, ProcessingOrder order)
        {
            switch (order)
            {
                case ProcessingOrder.Update:
                    _updateTasks.Enqueue(task);
                    break;
                case ProcessingOrder.Render:
                    _renderTasks.Enqueue(task);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        public Action? GetNextUpdateTask()
        {
            _updateTasks.TryDequeue(out var task);
            return task;
        }

        public Action? GetNextRenderTask()
        {
            _renderTasks.TryDequeue(out var task);
            return task;
        }

        public PmdContainer? GetParent()
        {
            return _parent;
        }

        public virtual Type? TickBefore()
        {
            return null;
        }

        // Useful wrappers
        protected TComponent GetParentComponent<TComponent>() where TComponent : PmdComponent
        {
            return _parent!.GetComponent<TComponent>();
        }

        protected TComponent? GetParentIfExists<TComponent>(bool unique = true) where TComponent : PmdComponent
        {
            return _parent!.GetIfExists<TComponent>(unique);
        }

        protected TComponent GetOtherComponent<TComponent>(PmdComponentId id) where TComponent : PmdComponent
        {
            return (TComponent)Base.GetRuntimeComponent(id);
        }

        protected PmdComponent GetOtherComponent(PmdComponentId id)
        {
            return Base.GetRuntimeComponent(id);
        }

        protected TComponent GetBaseContainerComponent<TComponent>(PmdContainerId id) where TComponent : PmdComponent
        {
            return (TComponent)Base.GetContainerComponent(typeof(TComponent), id);
        }

        protected TComponent GetBaseContainerComponent<TContainer, TComponent>() where TComponent : PmdComponent where TContainer : PmdContainer
        {
            return (TComponent)Base.GetContainerComponent(typeof(TContainer), typeof(TComponent));
        }

        protected TContainer GetBaseContainer<TContainer>() where TContainer : PmdContainer
        {
            return Base.GetFirstContainer<TContainer>();
        }

        protected PmdContainer GetContainer(PmdContainerId id)
        {
            return Base.GetContainer(id);
        }

        protected TComponent GetContainerComponent<TComponent>(PmdComponentId id) where TComponent : PmdComponent
        {
            var container = GetContainer(id);
            return container.GetComponent<TComponent>();
        }

        protected TComponent GetSharedComponent<TComponent>() where TComponent : PmdComponent
        {
            return (TComponent)Base.GetSharedComponent(typeof(TComponent));
        }

        /**
         * Note: Requires a parent container to work
         */
        public PmdComponentId RegisterIfNotExist<TComponent>(
            bool unique,
            out TComponent instance,
            params object[] arguments) where TComponent : PmdComponent
        {
            return _parent!.RegisterIfNotExist(unique, out instance, arguments);
        }

        public PmdComponentId RegisterSyncedIfNotExist<TComponent>(
            bool unique,
            string tag,
            out TComponent instance,
            params object[] arguments) where TComponent : PmdComponent
        {
            return _parent!.RegisterSyncedIfNotExist(unique, tag, out instance, arguments);
        }

        /**
         * Note: Requires a parent container to work
         */
        public PmdComponentId Register<TComponent>(
            bool unique,
            out TComponent instance,
            params object[] arguments) where TComponent : PmdComponent
        {
            return _parent!.RegisterComponent(unique, out instance, arguments);
        }

        public PmdComponentId Register(
            PmdComponentId id,
            Type componentType,
            bool unique,
            params object[] arguments)
        {
            return _parent!.RegisterComponent(id, componentType, unique, arguments);
        }

        protected void DeregisterParent<TComponent>(PmdComponentId id, bool unique) where TComponent : PmdComponent
        {
            _parent!.Deregister<TComponent>(id, unique);
        }

        /**
         * Note: Requires a parent container to work
         */
        protected PmdComponentId RegisterIfNotExist<TComponent>(
            bool unique,
            params object[] arguments) where TComponent : PmdComponent
        {
            return _parent!.RegisterIfNotExist<TComponent>(unique, arguments);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                Cleanup();
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~PmdComponent()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
