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
        public PmdPacketType PmdPacketType;
        public int Id;
        public bool Unique;
        public string? Attribute;
        public Type? DataType;
        public object[]? Data;
        public required PmdPacketSource Source;
    }

    [Key(0)]
    public string Header = "RK";

    [Key(1)]
    public int ProtocolVersion = 1;

    [Key(2)]
    public PmdPacketType Type;

    [Key(3)]
    public required string ComponentType;

    [Key(4)]
    public int ComponentId;

    [Key(5)]
    public required string Data;

    [Key(6)] 
    public string? Attribute;

    [Key(7)]
    public bool Unique = true;

    [Key(8)] 
    public string? DataType;

    [Key(9)]
    public required PmdPacketSource Source;

    // https://stackoverflow.com/a/15020402

    public static bool ValidPacket(PacketDto packet)
    {
        return packet.Data is { Length: > 0 };
    }

    public static byte[] Encode(Type componentType, object[] values, int componentId, PmdPacketType type, bool unique, string? field, Type? dataType, PmdPacketSource source)
    {
        if (!TypeCheck(componentType, true))
        {
            throw new ArgumentException("Not allowed");
        }

        if (dataType != null)
        {
            if (!TypeCheck(dataType, true))
            {
                throw new ArgumentException("Not allowed");
            }
        }

        var componentName = componentType.FullName ?? componentType.Name;
        var dataName = dataType?.FullName ?? dataType?.Name;

        var packet = new PmdPacket
        {
            Type = type,
            ComponentId = componentId,
            ComponentType = componentName,
            Data = JsonSerializer.Serialize(values),
            Attribute = field,
            Unique = unique,
            DataType = dataName,
            Source = source
        };

        return MessagePackSerializer.Serialize(packet);
    }

    private static bool TypeCheck(Type type, bool isComponent)
    {
        return type.IsPrimitive || type.IsValueType || (isComponent && type.IsSubclassOf(typeof(PmdListenableComponent<>))) || type == typeof(PmdListenableValue<>);
    }

    public static byte[] Encode(Type componentType, object value, int componentId, PmdPacketType type, bool unique, string? field, Type? dataType, PmdPacketSource source)
    {
        return Encode(componentType, [value], componentId, type, unique, field, dataType, source);
    }

    public static PacketDto Decode(byte[] packetBytes)
    {
        var options = MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData);

        var packet = MessagePackSerializer.Deserialize<PmdPacket>(packetBytes, options);

        var componentType = System.Type.GetType(packet.ComponentType);
        if (componentType == null) throw new InvalidDataException($"Could not find component type {packet.ComponentType}");

        if (!TypeCheck(componentType, true))
        {
            throw new InvalidDataException($"Given component type isn't valid: {packet.ComponentType}");
        }

        Type? dataType = null;
        if (packet.DataType != null)
        {
            dataType = System.Type.GetType(packet.DataType);
            if (dataType == null) throw new InvalidDataException($"Could not find data type {packet.ComponentType}");

            if (!TypeCheck(dataType, true))
            {
                throw new InvalidDataException($"Given data type isn't valid: {packet.DataType}");
            }
        }
        
        var packetDto = new PacketDto
        {
            Id = packet.ComponentId,
            Type = componentType,
            PmdPacketType = packet.Type,
            Attribute = packet.Attribute,
            Unique = packet.Unique,
            Source = packet.Source
        };

        if (dataType != null)
        {
            var result = JsonSerializer.Deserialize(packet.Data, dataType);
            if (result != null)
            {
                packetDto.Data = [result];
            }
        }
        else
        {
            packetDto.Data = JsonSerializer.Deserialize<object[]>(packet.Data);
        }

        return packetDto;
    }
}