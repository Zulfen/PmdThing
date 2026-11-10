using DotTiled;
using PmdThing.Components;
using PmdThing.Components.Networking;
using PmdThing.Components.Networking.QueryHandlers;
using PmdThing.Components.Networking.Rokon;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

public class Program
{

    public static void Main(string[] args)
    {
        var game = PmdGameBase.Create(1280, 720, "ksjdfnsfklm;dsk", args);

        var headless = game.Framework
            .Graphics
            .GetCapabilities()
            .Headless;

        if (headless)
        {
            var server = game.RegisterServer<RokonSocketServer>("localhost", 2312);

            var tmxLoader = new TmxTileProvider(@"TileSets\142map.tmx");
            game.RegisterContainer<TiledWorld>(out _,tmxLoader);

            game.RegisterComponent<TileQuery>(game.BaseContainer, true, out var tileQueryHandler);
            game.RegisterComponent<ContainersQuery>(game.BaseContainer, true, out var containersQueryHandler);
            game.RegisterComponent<GetComponentsQuery>(game.BaseContainer, true, out var componentsQuery);
            game.RegisterComponent<MovementRequest>(game.BaseContainer, true, out var movementRequestHandler);

            server.RegisterHandler(QueryType.Tile, tileQueryHandler);
            server.RegisterHandler(QueryType.ContainerCount, containersQueryHandler);
            server.RegisterHandler(QueryType.SendComponents, componentsQuery);
            server.RegisterHandler(QueryType.InputTileMove, movementRequestHandler);
        }
        else
        {
            game.RegisterClient<RokonGameClient>("localhost", 2312);
            game.RegisterComponent<RokonTileProvider>(game.BaseContainer, true, out var networkLoader);
            game.RegisterContainer<TiledWorld>(out var tiledGame, networkLoader);
        }

        if (!headless)
        {
            game.RegisterComponent<Camera>(game.BaseContainer, false, out _);
        }
        else
        {
            game.RegisterContainer<TestPokemon>(out _, PmdConstants.Pokemon.Vulpix, true);
            //game.RegisterContainer<TestPokemon>(out _, PmdConstants.Pokemon.Eevee, false);
            game.RegisterContainer<Window>(out var window, 50, 50, 10);
            window.RegisterContainer<TextContainer>("hello this is text", 20, 20, -5);
        }

        game.Start();
        game.Server?.Cleanup();
        game.Client?.Cleanup();

        Console.Write("saturn was here");
    }

}
