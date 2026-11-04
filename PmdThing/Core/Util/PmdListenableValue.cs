namespace PmdThing.Core.Util
{
    public class PmdListenableValue<T>
    {

        public class ValueEventArgs(T newValue, T oldValue) : EventArgs
        {
            public T NewValue { get; } = newValue;
            public T OldValue { get; } = oldValue;
        }

        private readonly PmdComponent _parent;
        private readonly ProcessingOrder _processingOrder;

        private T _data;
        public event Action<T, T>? Changed;

        private bool _hasChanged;

        public PmdListenableValue(PmdComponent component, T initialData, string? tag = null, ProcessingOrder order = ProcessingOrder.Update)
        {
            _parent = component;
            _processingOrder = order;
            _data = initialData;
            SetChanged(true, initialData);
        }

        public void Set(T newData)
        {
            if (!Equals(_data, newData)) SetChanged(true, newData);
        }

        public void SetChanged(bool changed, T newValue)
        {
            _hasChanged = changed;
            _parent.SubmitTask(() => Changed?.Invoke(newValue, _data), _processingOrder);
            _data = newValue;
        }

        public bool GetChanged()
        {
            var result = _hasChanged;
            _hasChanged = false;
            return result;
        }

        public T Get()
        {
            return _data;
        }

    }

    public enum ProcessingOrder
    {
        Render,
        Update
    }

}
