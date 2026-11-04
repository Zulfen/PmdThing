using System.Text.Json;
using PmdThing.Core.Util;

namespace PmdThing.Core.Network.Rokon;

using MessagePack;
using System;

[MessagePackObject]
public struct PmdPacket()
{

    public record PacketDto
    {
        public required Type Type;
        public required PacketType PacketType;
        public required int Id;
        public bool Unique;
        public string? Field;
        public object[]? Data;
    }

    [Key(0)]
    public string Header = "RK";

    [Key(1)]
    public int ProtocolVersion = 1;

    [Key(2)]
    public PacketType Type;

    [Key(3)]
    public string ComponentType;

    [Key(4)]
    public int ComponentId;

    [Key(5)]
    public string Data;

    [Key(6)] 
    public string? Field;

    [Key(7)]
    public bool Unique;

    // https://stackoverflow.com/a/15020402

    public static bool ValidPacket(PacketDto packet)
    {
        return packet.Data is { Length: > 0 };
    }

    public static byte[] Encode(Type componentType, object[] values, int componentId, PacketType type, bool unique, string? field)
    {
        if (!TypeCheck(componentType, true))
        {
            throw new ArgumentException("Not allowed");
        }

        var className = componentType.FullName ?? componentType.Name;
        var packet = new PmdPacket
        {
            Type = type,
            ComponentId = componentId,
            ComponentType = className,
            Data = JsonSerializer.Serialize(values),
            Field = field,
            Unique = unique
        };

        return MessagePackSerializer.Serialize(packet);
    }

    private static bool TypeCheck(Type type, bool isComponent)
    {
        return type.IsPrimitive || type.IsValueType || (isComponent && type.IsSubclassOf(typeof(PmdListenableComponent<>))) || type == typeof(PmdListenableValue<>);
    }

    public static byte[] Encode(Type componentType, object value, int componentId, PacketType type, bool unique, string? field)
    {
        return Encode(componentType, [value], componentId, type, unique, field);
    }

    public static PacketDto Decode(byte[] packetBytes)
    {
        var options = MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData);

        var packet = MessagePackSerializer.Deserialize<PmdPacket>(packetBytes, options);

        var componentType = System.Type.GetType(packet.ComponentType);
        if (componentType == null) throw new InvalidDataException($"Could not find type {packet.ComponentType}");

        if (!TypeCheck(componentType, true))
        {
            throw new InvalidDataException($"Given component type isn't valid: {packet.ComponentType}");
        }

        return new PacketDto
        {
            Id = packet.ComponentId,
            Type = componentType,
            Data = JsonSerializer.Deserialize<object[]>(packet.Data),
            PacketType = packet.Type,
            Field = packet.Field,
            Unique = packet.Unique
        };
    }
}