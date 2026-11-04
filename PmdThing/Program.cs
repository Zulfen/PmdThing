using DotTiled;
using PmdThing.Components;
using PmdThing.Components.Networking.Rokon;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

public class Program
{

    public static void Main(string[] args)
    {
        var server = new Thread(() => Server(args));
        server.Start();
    }

    private static void Server(string[] commandLineArgs)
    {
        var game = PmdGameBase.Create(1280, 720, "ksjdfnsfklm;dsk", commandLineArgs);
        game.RegisterComponent<RokonSocketServer>(game.BaseContainer, true, out var server, 2312);

        // Make a generic shared container. Was going to do this in GameBase but decided not to.
        // TODO: Maybe removed the shared components feature and enforce passing around components from a main container like thi

        var tmxLoader = new TmxTileProvider(@"TileSets\142map.tmx");
        game.RegisterContainer<TiledWorld>(out var tiledGame, tmxLoader);

        if (!game.Framework
                .Graphics
                .GetCapabilities()
                .Headless)
        {
            game.RegisterComponent<Camera>(game.BaseContainer, false, out _);
        }
        

        game.RegisterContainer<TestPokemon>(out _, PmdConstants.Pokemon.Vulpix, true);
        game.RegisterContainer<TestPokemon>(out _, PmdConstants.Pokemon.Eevee, false);

        game.Start();
        server.Cleanup();

        Console.Write("saturn was here");
    }

}
