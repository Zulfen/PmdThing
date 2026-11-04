using PmdContainerId = System.Int32;
using PmdThing.Core.Util.Structures;
using PmdThing.Core.Hardware.Framework;

namespace PmdThing.Core
{
    public class PmdMultiContainer(PmdGameBase game, PmdFramework framework) : PmdContainer(game, framework)
    {

        private readonly PmdContainerRegistry _containerRegistry = new();

        private PmdContainerId _containerCounter;
        private readonly Dictionary<PmdContainerId, PmdContainer> _runtimeContainers = new();

        public void AddContainer<TContainer>(PmdContainerId id, TContainer instance) where TContainer : PmdContainer
        {
            _containerRegistry.Add(typeof(TContainer), id);
            _runtimeContainers.Add(id, instance);
        }

        public PmdContainerId RegisterContainer<TContainer>(out TContainer instance, params object[]? arguments) where TContainer : PmdContainer
        {
            object[] completeArgs;
            if (arguments != null)
            {
                completeArgs = new object[arguments.Length + 2];
                completeArgs[0] = Base;
                completeArgs[1] = framework;

                arguments.CopyTo(completeArgs, 2);
            }
            else
            {
                completeArgs = [Base, framework];
            }
            
            var containerInstance = Activator.CreateInstance(typeof(TContainer), completeArgs);
            if (containerInstance is TContainer container)
            {
                instance = container;
                _containerCounter++;
                AddContainer(_containerCounter, instance);
                return _containerCounter;
            }

            throw new ArgumentException(
                $"Container {typeof(TContainer)} could not be initialised with the following arguments: {arguments}");

        }

        public PmdContainerId RegisterEmptyContainer(out PmdContainer instance)
        {
            var id = RegisterContainer(out instance);
            return id;
        }

        public PmdContainerId GetContainer<TContainer>(out TContainer instance) where TContainer : PmdContainer
        {
            var id = _containerRegistry.GetFirst(typeof(TContainer));

            if (_runtimeContainers.TryGetValue(id, out var container))
            {
                instance = (TContainer)container;
            }
            else
            {
                throw new ArgumentException($"Container with ID {id} has not been registered.");
            }

            return id;
        }

        public TContainer GetContainer<TContainer>(PmdContainerId id) where TContainer : PmdContainer
        {
            return (TContainer)GetContainer(id);
        }

        public PmdContainer GetContainer(Type containerType)
        {
            var id = _containerRegistry.GetFirst(containerType);

            return _runtimeContainers.TryGetValue(id, out var container) ? container : throw new ArgumentException($"Container with ID {id} has not been registered.");
        }

        public PmdContainer GetContainer(PmdContainerId id)
        {
            return _runtimeContainers.TryGetValue(id, out var container) ? container : throw new ArgumentException($"Container with ID {id} has not been registered.");
        }

        public List<TContainer> GetAllContainers<TContainer>() where TContainer : PmdContainer
        {
            return _containerRegistry
                .GetAll(typeof(TContainer))
                .Select(GetContainer<TContainer>)
                .ToList();
        }

        public void DeregisterContainer(PmdContainer instance, PmdContainerId id)
        {
            _containerRegistry.Remove(instance.GetType(), id);

            if (_runtimeContainers.Remove(id, out var container))
            {
                container.GetAllComponents()
                    .ForEach(entry => Base.DeregisterComponent(entry.Type, entry.Id, false));
                _containerCounter--;
            }
        }

    }
}
