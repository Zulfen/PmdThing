using PmdThing.Core.Util;
using PmdThing.Core.Util.Structures;
using System.Reflection;
using PmdContainerId = System.Int32;

namespace PmdThing.Core
{
    public class PmdMultiContainer(PmdContainerConfig config) : PmdContainer(config)
    {
        private readonly PmdContainerRegistry _containerRegistry = new();

        private PmdContainerId _containerCounter;
        private readonly Dictionary<PmdContainerId, PmdContainer> _runtimeContainers = new();

        public event Action<PmdContainerId, PmdContainer, object[]?>? ContainerRegistered;


        public PmdContainerId RegisterContainer(PmdContainerId? id, Type containerType, out PmdContainer instance, params object[]? arguments)
        {
            // ReSharper disable once UseSymbolAlias
            var newId = id ?? Random.Shared.Next(1, int.MaxValue);
            var newConfig = new PmdContainerConfig(config.Base, Framework, this, id ?? _containerCounter);

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
                _containerCounter++;

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
            return _runtimeContainers.TryGetValue(id, out var container) ? container : throw new ArgumentException($"Container with ID {id} has not been registered.");
        }

        public List<TContainer> GetAllContainers<TContainer>() where TContainer : PmdContainer
        {
            return _containerRegistry
                .GetAll(typeof(TContainer))
                .Select(GetContainer<TContainer>)
                .ToList();
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
                _containerCounter--;
            }
        }

    }
}
