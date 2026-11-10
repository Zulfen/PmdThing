namespace PmdThing.Core.Network.Rokon;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Property)]
public class PmdServerSync : Attribute
{
    public bool UpdateValue { get; set; } = true;
}