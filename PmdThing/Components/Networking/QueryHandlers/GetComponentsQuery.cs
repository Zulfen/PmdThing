using PmdThing.Core;
using PmdThing.Core.Network;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Components.Networking.QueryHandlers
{
    public class GetComponentsQuery(PmdComponentConfig config, IPmdGameServer server) : QueryHandler(config)
    {

        public override dynamic? Handle(dynamic? requestData, IPmdServerConnection connection)
        {
            connection.SignalReady();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator(it looks ugly...)
            foreach (var componentEntry in server.GetSyncComponents())
            {
                if (PmdTypeUtils.IsGenericClass(componentEntry.Value.GetType(), typeof(PmdListenableComponent<>)))
                {
                    var data = componentEntry.Value
                        .GetType()
                        .GetProperty("ComponentValue")?
                        .GetValue(componentEntry.Value);

                    if (data == null) continue;

                    var packet = new PmdPacket.PacketDto
                    {
                        Data = [data],
                        Type = componentEntry.Value.GetType(),
                        DataType = data.GetType(),
                        PmdPacketType = PmdPacketType.UpdateComponent,
                        Id = componentEntry.Key,
                        ParentContainer = componentEntry.Value
                            .GetParent()?
                            .Id,
                        Unique = true,
                        Source = PmdPacketSource.Server,
                        Tag = PmdNetworkHandler.Instance?.GetTagForId(componentEntry.Key)
                    };

                    connection.SendPacket(packet);
                }
            }

            return null;
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

    }
}
