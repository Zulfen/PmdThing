using PmdThing.Core.Util;

namespace PmdThing.Core.Network.Rokon
{
    public class PmdServerValue<T>(PmdComponent component, T initialData, ProcessingOrder order = ProcessingOrder.Update, bool raiseChanged = true, bool async = false) : PmdListenableValue<T>(component, initialData, order, raiseChanged, async)
    {
        private readonly PmdComponent _component = component;

        public override void Set(T newData)
        {
            if (_component.IsOnServer())
            {
                base.Set(newData);
            }
        }
    }
}
