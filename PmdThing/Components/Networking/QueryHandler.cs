using DotTiled;
using PmdThing.Core;

namespace PmdThing.Components.Networking
{
    public abstract class QueryHandler(PmdComponentConfig config) : PmdComponent(config)
    {
        public abstract dynamic? Handle(dynamic requestData);

        public abstract Type GetDataType();

    }
}
