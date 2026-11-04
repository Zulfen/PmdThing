using PmdThing.Core.Util;

namespace PmdThing.Core.Network.Rokon
{
    public class PmdServerValue<T>(PmdComponent component, T initialData, ProcessingOrder order = ProcessingOrder.Update, bool raiseChanged = true) : PmdListenableValue<T>(component, initialData, order, raiseChanged)
    {
        public override void Set(T newData)
        {
            if (component.IsOnServer())
            {
                base.Set(newData);
            }
        }
    }
}
