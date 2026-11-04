using System.Collections.Concurrent;
using PmdThing.Core.Hardware.Framework;
using PmdThing.Core.Util;
using PmdComponentId = System.Int32;

namespace PmdThing.Core
{
    public abstract class PmdComponent(PmdComponentConfig config)
    {

        // re-add init method
        // ReSharper disable UseSymbolAlias :3

        private readonly Func<PmdComponentId, PmdComponent>? _otherComponent = config.OtherComponent;
        private readonly Func<Type, PmdComponentId, PmdComponent?>? _containerComponent = config.ContainerComponent;
        private readonly Func<Type, PmdComponent>? _sharedComponent = config.SharedComponent;
        

        // can be null
        public readonly PmdContainer? Parent = config.Parent;

        #pragma warning restore IDE0044
        public bool ShouldUpdate = true;
        public bool DoesRendering = false;
        public int Priority = config.Priority;

        private readonly ConcurrentQueue<Action> _updateTasks = new();
        private readonly ConcurrentQueue<Action> _renderTasks = new();
        protected readonly PmdFramework Framework = PmdFrameworkFactory.GetInstance();

        public virtual void Update()
        {
        }

        public virtual void Render()
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
            return Parent;
        }

        // Useful wrappers
        protected TComponent GetParentComponent<TComponent>() where TComponent : PmdComponent
        {
            return (TComponent)Parent!.GetComponent(typeof(TComponent));
        }

        protected TComponent? GetParentIfExists<TComponent>(bool unique = true) where TComponent : PmdComponent
        {
            return Parent!.GetIfExists<TComponent>(unique);
        }

        protected TComponent GetOtherComponent<TComponent>(PmdComponentId id) where TComponent : PmdComponent
        {
            return (TComponent)_otherComponent!.Invoke(id);
        }

        protected TComponent? GetContainerComponent<TComponent>(PmdComponentId id) where TComponent : PmdComponent
        {
            return _containerComponent!.Invoke(typeof(TComponent), id) as TComponent;
        }

        protected TComponent GetSharedComponent<TComponent>() where TComponent : PmdComponent
        {
            return (TComponent)_sharedComponent!.Invoke(typeof(TComponent));
        }

        /**
         * Note: Requires a parent container to work
         */
        protected PmdComponentId RegisterIfNotExist<TComponent>(
            bool unique,
            out TComponent instance,
            params object[] arguments) where TComponent : PmdComponent
        {
            return Parent!.RegisterIfNotExist(unique, out instance, arguments);
        }

        /**
         * Note: Requires a parent container to work
         */
        protected PmdComponentId Register<TComponent>(
            bool unique,
            out TComponent instance,
            params object[] arguments) where TComponent : PmdComponent
        {
            return Parent!.RegisterComponent(unique, out instance, arguments);
        }

        protected void DeregisterParent<TComponent>(PmdComponentId id, bool unique) where TComponent : PmdComponent
        {
            Parent!.Deregister<TComponent>(id, unique);
        }

        /**
         * Note: Requires a parent container to work
         */
        protected PmdComponentId RegisterIfNotExist<TComponent>(
            bool unique,
            params object[] arguments) where TComponent : PmdComponent
        {
            return Parent!.RegisterIfNotExist<TComponent>(unique, arguments);
        }

    }
}
