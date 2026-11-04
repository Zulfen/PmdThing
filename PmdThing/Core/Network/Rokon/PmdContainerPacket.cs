using System.Text.Json;

namespace PmdThing.Core.Network.Rokon;

using MessagePack;
using System;

[MessagePackObject]
public struct PmdContainerPacket()
{
    public enum PacketType
    {
        Add,
        Remove,
    }

    public record PacketAction
    {
        public required object[]? Data;
        public required Type ContainerType;
        public required PacketType Type;
        public required int ContainerId;
    }

    [Key(0)]
    public string Header = "RK";

    [Key(1)]
    public int ProtocolVersion = 1;

    [Key(2)]
    public PacketType Type;

    [Key(3)]
    public string ContainerType;

    [Key(4)]
    public int ContainerId;

    [Key(5)]
    public string Data;

    // https://stackoverflow.com/a/15020402

    public static byte[] Encode(Type containerType, object[] values, int containerId, PacketType type)
    {
        if (!TypeCheck(containerType, true))
        {
            throw new ArgumentException("Not allowed");
        }

        var className = containerType.FullName ?? containerType.Name;
        var packet = new PmdContainerPacket
        {
            Type = type,
            ContainerId = containerId,
            ContainerType = className,
            Data = JsonSerializer.Serialize(values)
        };

        return MessagePackSerializer.Serialize(packet);
    }

    private static bool TypeCheck(Type type, bool isContainer)
    {
        return type.IsPrimitive || type.IsValueType || (isContainer && type.IsSubclassOf(typeof(PmdContainer)));
    }

    public static byte[] Encode(Type containerType, object value, int containerId, PacketType type)
    {
        return Encode(containerType, [value], containerId, type);
    }

    public static PacketAction Decode(byte[] packetBytes)
    {
        var options = MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData);

        var packet = MessagePackSerializer.Deserialize<PmdContainerPacket>(packetBytes, options);

        var containerType = System.Type.GetType(packet.ContainerType);
        if (containerType == null) throw new InvalidDataException($"Could not find type {packet.ContainerType}");

        if (!TypeCheck(containerType, true))
        {
            throw new InvalidDataException($"Given Container type isn't valid: {packet.ContainerType}");
        }

        return new PacketAction
        {
            ContainerId = packet.ContainerId,
            ContainerType = containerType,
            Data = JsonSerializer.Deserialize<object[]>(packet.Data),
            Type = packet.Type
        };
    }
}