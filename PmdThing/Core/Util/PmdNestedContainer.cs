namespace PmdThing.Core.Util
{
    public class PmdNestedContainer<T> : PmdContainer where T : PmdMultiContainer
    {

        public T ParentContainer { get; init; }

        public PmdNestedContainer(PmdContainerConfig config) : base(config)
        {
            if (config.Parent == null) throw new ArgumentException("Container must have a parent container");
            ParentContainer = (T) config.Parent!;
        }

    }
}
