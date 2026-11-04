namespace PmdThing.Core.Util
{
    public class PmdListenableComponent<T> : PmdComponent
    {
        private readonly PmdListenableValue<T> _value;

        public T ComponentValue
        {
            get => _value.Get();
            set => SetValue(value);
        }

        public event Action<T, T>? Changed;

        public PmdListenableComponent(PmdComponentConfig config, T value) : base(config)
        {
            _value = new PmdListenableValue<T>(this, value);
            _value.Changed += OnChanged;
        }

        public void SetChanged(bool changed)
        {
            _value.SetChanged(changed, ComponentValue);
        }

        private void OnChanged(T newValue, T oldValue)
        {
            Changed?.Invoke(newValue, oldValue);
        }

        protected virtual void SetValue(T newData)
        {
            _value.Set(newData);
        }

        public override void Cleanup()
        {
            // nothing
        }
    }
}
