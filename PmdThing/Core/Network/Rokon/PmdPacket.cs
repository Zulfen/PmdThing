using System.Reflection;
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
        public int? ParentContainer;
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

    [Key(10)]
    public int? ParentContainer;

    // https://stackoverflow.com/a/15020402

    public static bool ValidPacket(PacketDto packet)
    {
        return packet.Data is { Length: > 0 };
    }

    public static byte[] Encode(PacketDto packetDto)
    {
        if (!TypeCheck(packetDto.Type, true))
        {
            throw new ArgumentException("Not allowed");
        }

        if (packetDto.DataType != null)
        {
            if (!TypeCheck(packetDto.DataType, true))
            {
                throw new ArgumentException("Not allowed");
            }
        }

        var componentName = packetDto.Type.FullName ?? packetDto.Type.Name;
        var dataName = packetDto.DataType?.FullName ?? packetDto.DataType?.Name;

        var packet = new PmdPacket
        {
            Type = packetDto.PmdPacketType,
            ComponentId = packetDto.Id,
            ComponentType = componentName,
            Data = packetDto.DataType != null ? JsonSerializer.Serialize(packetDto.Data![0], packetDto.DataType) : JsonSerializer.Serialize(packetDto.Data),
            Attribute = packetDto.Attribute,
            Unique = packetDto.Unique,
            DataType = dataName,
            Source = packetDto.Source,
            ParentContainer = packetDto.ParentContainer
        };

       
        return MessagePackSerializer.Serialize(packet);
        
    }

    private static bool TypeCheck(Type type, bool isComponent)
    {
        return type.IsPrimitive ||
               type.IsValueType ||
               type.IsDefined(typeof(PmdClientSync)) ||
               type.IsDefined(typeof(PmdServerSync)) ||
               type == typeof(PmdListenableValue<>) ||
               type.IsDefined(typeof(PmdSerializable)) ||
               type == typeof(string);
    }

    private static Type? FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == fullName);
    }


    public static PacketDto? Decode(byte[] packetBytes)
    {
        if (packetBytes[0] == 0)
        {
            return null;
        }

        //var options = MessagePackSerializerOptions.Standard
            //.WithSecurity(MessagePackSecurity.UntrustedData);

        var packet = MessagePackSerializer.Deserialize<PmdPacket>(packetBytes);

        var componentType = FindType(packet.ComponentType);
        if (componentType == null) throw new InvalidDataException($"Could not find component type {packet.ComponentType}");

        if (!TypeCheck(componentType, true))
        {
            throw new InvalidDataException($"Given component type isn't valid: {packet.ComponentType}");
        }

        Type? dataType = null;
        if (packet.DataType != null)
        {
            dataType = FindType(packet.DataType);
            if (dataType == null) throw new InvalidDataException($"Could not find data type {packet.DataType}");

            if (!TypeCheck(dataType, true))
            {
                throw new InvalidDataException($"Given data type isn't valid: {packet.DataType}");
            }
        }
        
        var packetDto = new PacketDto
        {
            Id = packet.ComponentId,
            ParentContainer = packet.ParentContainer,
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
            var asJson = JsonSerializer.Deserialize<JsonElement[]>(packet.Data);
            packetDto.Data = PmdTypeUtils.ParseJsonElements(asJson);
        }

        return packetDto;
    }
}