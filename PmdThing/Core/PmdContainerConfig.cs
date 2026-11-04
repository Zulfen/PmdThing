using PmdThing.Core.Hardware.Framework;
using PmdContainerId = System.Int32;

namespace PmdThing.Core
{
    public record PmdContainerConfig(PmdGameBase Base, PmdFramework Framework, PmdMultiContainer? Parent, PmdContainerId Id);
}
