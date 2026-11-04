namespace PmdThing.Core.Util
{
    public class PmdListenableValue<T>
    {

        private readonly PmdComponent _parent;
        private readonly ProcessingOrder _processingOrder;

        private T _data;
        public event Action<T, T>? Changed;
        private bool _hasChanged = true;

        public PmdListenableValue(PmdComponent component, T initialData, ProcessingOrder order = ProcessingOrder.Update)
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
            _hasChanged = true;
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
