
namespace PmdThing.Core.Util
{
    /// <summary>
    /// A reference to a texture.
    /// </summary>
    /// <typeparam name="T">Type of platform specific handle</typeparam>
    /// <param name="handle">Given handle</param>
    public record PmdTexture<T>(T handle)
    {
        public T Handle { get; init; } = handle;
    }
}
