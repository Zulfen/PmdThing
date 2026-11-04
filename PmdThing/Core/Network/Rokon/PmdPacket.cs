using PmdThing.Core.Util;
using System.Reflection;
using System.Text.Json;

namespace PmdThing.Core.Network.Rokon;

using MessagePack;
using System;

[MessagePackObject]
public struct PmdPacket()
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard;

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
        public string? Tag;
    }

    [Key(0)] public static readonly string Header = "RK";

    [Key(1)] public int ProtocolVersion = 1;

    [Key(2)] public PmdPacketType Type;

    [Key(3)] public required string ComponentType;

    [Key(4)] public int ComponentId;

    [Key(5)] public required string Data;

    [Key(6)] public string? Attribute;

    [Key(7)] public bool Unique = true;

    [Key(8)] public string? DataType;

    [Key(9)] public required PmdPacketSource Source;

    [Key(10)] public int? ParentContainer;

    [Key(11)] public string? Tag;

    // https://stackoverflow.com/a/15020402

    public static byte[] Encode(PacketDto packetDto)
    {
        if (!IsValidDataType(packetDto.Type))
        {
            throw new ArgumentException($"Not allowed: {packetDto.Type}");
        }
        
        var componentName = packetDto.Type.FullName ?? packetDto.Type.Name;
        var dataName = packetDto.DataType?.FullName ?? packetDto.DataType?.Name;

        var packet = new PmdPacket
        {
            Type = packetDto.PmdPacketType,
            ComponentId = packetDto.Id,
            ComponentType = componentName,
            Data = packetDto.DataType != null
                ? JsonSerializer.Serialize(packetDto.Data![0], packetDto.DataType)
                : JsonSerializer.Serialize(packetDto.Data),
            Attribute = packetDto.Attribute,
            Unique = packetDto.Unique,
            DataType = dataName,
            Source = packetDto.Source,
            ParentContainer = packetDto.ParentContainer,
            Tag = packetDto.Tag
        };


        return MessagePackSerializer.Serialize(packet, Options);
    }

    public static bool IsValidDataType(Type type)
    {
        return type.IsPrimitive ||
               type.IsValueType ||
               type.IsDefined(typeof(PmdClientSync)) ||
               type.IsDefined(typeof(PmdServerSync)) ||
               PmdTypeUtils.IsGenericClass(type, typeof(PmdListenableValue<>)) ||
               type.IsDefined(typeof(PmdSerializable)) ||
               type == typeof(string);
    }


    public static async Task<List<PacketDto>> DecodeStreamAsync(byte[] streamBytes, CancellationToken cancellationToken)
    {
        var packetDtos = new List<PacketDto>();
        var rawPackets = new List<PmdPacket>();

        using (var stream = new MemoryStream(streamBytes))
        {
            try
            {
                using var streamReader = new MessagePackStreamReader(stream);
                while (await streamReader.ReadAsync(cancellationToken) is { } msgpack)
                {
                    rawPackets.Add(MessagePackSerializer.Deserialize<PmdPacket>(msgpack, Options, cancellationToken));
                }
            }
            catch (MessagePackSerializationException)
            {
                return packetDtos;
            }
        }

        foreach (var rawPacket in rawPackets)
        {
            var componentType = PmdTypeUtils.FindType(rawPacket.ComponentType);
            if (componentType == null)
                throw new InvalidDataException($"Could not find component type {rawPacket.ComponentType}");

            if (!IsValidDataType(componentType))
            {
                throw new InvalidDataException($"Given component type isn't valid: {rawPacket.ComponentType}");
            }

            Type? dataType = null;
            if (rawPacket.DataType != null)
            {
                dataType = PmdTypeUtils.FindType(rawPacket.DataType);
                if (dataType == null) throw new InvalidDataException($"Could not find data type {rawPacket.DataType}");

                if (!IsValidDataType(dataType))
                {
                    throw new InvalidDataException($"Given data type isn't valid: {rawPacket.DataType}");
                }
            }

            var packetDto = new PacketDto
            {
                Id = rawPacket.ComponentId,
                ParentContainer = rawPacket.ParentContainer,
                Type = componentType,
                PmdPacketType = rawPacket.Type,
                Attribute = rawPacket.Attribute,
                Unique = rawPacket.Unique,
                Source = rawPacket.Source,
                Tag = rawPacket.Tag,
            };

            if (dataType != null)
            {
                var result = JsonSerializer.Deserialize(rawPacket.Data, dataType);
                if (result != null)
                {
                    packetDto.Data = [result];
                }
            }
            else
            {
                var asJson = JsonSerializer.Deserialize<JsonElement[]>(rawPacket.Data);
                packetDto.Data = PmdTypeUtils.ParseJsonElements(asJson);
            }

            packetDtos.Add(packetDto);
        }

        return packetDtos;
    }
}