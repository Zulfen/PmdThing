using PmdThing.Core;
using PmdThing.Core.Network;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using PmdContainerId = System.Int32;

namespace PmdThing.Components.Networking.QueryHandlers
{
    public class GetComponentsQuery(PmdComponentConfig config) : QueryHandler(config)
    {

        public override dynamic? Handle(dynamic? requestData, PmdContainerId? parentContainerId, IPmdServerConnection connection)
        {
            if (Server == null) return null;
            connection.SignalReady();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator(it looks ugly...)
            foreach (var componentEntry in Server.GetSyncComponents())
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
                        Parent = componentEntry.Value
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
