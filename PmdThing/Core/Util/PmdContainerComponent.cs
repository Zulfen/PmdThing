namespace PmdThing.Core.Util
{
    public class PmdContainerComponent<T> : PmdComponent where T : PmdContainer
    {

        public T ParentContainer { get; init; }

        public PmdContainerComponent(PmdComponentConfig config) : base(config)
        {
            if (config.Parent == null) throw new ArgumentException("Component must have a parent container");
            ParentContainer = (T) config.Parent!;
        }

        //public PmdContainerId GetChildContainer<TContainer>(out TContainer instance) where TContainer : PmdContainer
        //{
        //    var id = ParentContainer.GetContainer<TContainer>(out var container);
        //    instance = container;
        //    return id;
        //}

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
