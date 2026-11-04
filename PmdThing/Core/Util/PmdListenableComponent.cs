namespace PmdThing.Core.Util
{
    public class PmdListenableComponent<T> : PmdComponent
    {
        private readonly PmdListenableValue<T> _value;

        public T Value
        {
            get => _value.Get();
            set => _value.Set(value);
        }

        public event Action<T, T>? Changed;

        public PmdListenableComponent(PmdComponentConfig config, T value) : base(config)
        {
            _value = new PmdListenableValue<T>(this, value);
            _value.Changed += OnChanged;
        }

        private void OnChanged(T newValue, T oldValue)
        {
            Changed?.Invoke(newValue, oldValue);
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
