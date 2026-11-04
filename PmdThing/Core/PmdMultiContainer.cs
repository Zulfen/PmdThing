using PmdThing.Core.Util;
using PmdThing.Core.Util.Structures;
using System.Reflection;
using PmdContainerId = System.Int32;

namespace PmdThing.Core
{
    public class PmdMultiContainer(PmdContainerConfig config) : PmdContainer(config)
    {
        private readonly PmdContainerConfig _config = config;
        private readonly PmdContainerRegistry _containerRegistry = new();

        private readonly Dictionary<PmdContainerId, PmdContainer> _runtimeContainers = new();

        public event Action<PmdContainerId, PmdContainer, object[]?>? ContainerRegistered;


        public PmdContainerId RegisterContainer(PmdContainerId? id, Type containerType, out PmdContainer instance, params object[]? arguments)
        {
            // ReSharper disable once UseSymbolAlias
            var newId = id ?? Random.Shared.Next(2, int.MaxValue);
            var newConfig = new PmdContainerConfig(_config.Base, Framework, this, newId);

            object[] completeArgs;
            if (arguments != null)
            {
                completeArgs = new object[arguments.Length + 1];

                completeArgs[0] = newConfig;
                arguments.CopyTo(completeArgs, 1);
            }
            else
            {
                completeArgs = [newConfig];
            }

            // This big for loop makes sure serialized enums are handled for constructors if needed (they get sent as ints)
            ConstructorInfo? containerConstructor = null;
            foreach (var constructor in containerType.GetConstructors())
            {
                var paramTypes = constructor.GetParameters()
                    .Select(p => p.ParameterType)
                    .ToArray();

                if (paramTypes.Length == completeArgs.Length)
                {
                    var matches = true;

                    for (var i = 0; i < paramTypes.Length; i++)
                    {
                        if (!paramTypes[i].IsInstanceOfType(completeArgs[i]))
                        {
                            if (paramTypes[i].IsEnum)
                            {
                                var argumentToConvert = completeArgs[i];
                                // ReSharper disable once UseSymbolAlias
                                if (argumentToConvert.IsNumericType())
                                {
                                    try
                                    {
                                        completeArgs[i] = Enum.ToObject(paramTypes[i], Convert.ToInt32(argumentToConvert));
                                    }
                                    catch (ArgumentException)
                                    {
                                        matches = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                matches = false;
                                break;
                            }
                        }
                    }

                    if (matches) containerConstructor = constructor;
                }
            }

            if (containerConstructor == null)
            {
                throw new ArgumentException(
                    $"Container {containerType} does not have a constructor that matches the provided arguments: {arguments}");
            }

            var containerInstance = containerConstructor.Invoke(completeArgs);
            if (containerInstance is PmdContainer container)
            {
                instance = container;

                if (container.AsyncInit)
                {
                    Task.Run(container.Init);
                }

                _containerRegistry.Add(containerType, newId);
                _runtimeContainers.Add(newId, instance);

                ContainerRegistered?.Invoke(newId, instance, arguments);

                return newId;
            }

            throw new ArgumentException(
                $"Container {containerType} could not be initialised with the following arguments: {arguments}");

        }

        public PmdContainerId RegisterEmptyContainer(out PmdContainer instance)
        {
            var id = RegisterContainer(null, typeof(PmdContainer), out instance);
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
            if (_runtimeContainers.TryGetValue(id, out var normalContainer))
            {
                return normalContainer;
            }

            foreach (var container in _runtimeContainers.Values)
            {
                if (container is PmdMultiContainer multiContainer)
                {
                    return multiContainer.GetContainer(id);
                }
            }

            throw new ArgumentException($"Container with ID {id} has not been registered.");
        }

        public List<PmdContainerId> GetAllContainers()
        {
            return [.. _containerRegistry
                .GetAll()
                .Select(entry => entry.Id)];
        }

        public void DeregisterContainer(PmdContainer instance, PmdContainerId id)
        {
            _containerRegistry.Remove(instance.GetType(), id);

            if (_runtimeContainers.Remove(id, out var container))
            {
                container.GetAllComponents()
                    .ForEach(entry => Base.DeregisterComponent(entry.Type, entry.Id, false));
            }
        }

    }
}
