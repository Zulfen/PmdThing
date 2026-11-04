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
            game.RegisterServer<RokonSocketServer>(2312);

            var tmxLoader = new TmxTileProvider(@"TileSets\142map.tmx");
            game.RegisterContainer<TiledWorld>(out _,tmxLoader);

            game.RegisterComponent<TileQuery>(game.BaseContainer, true, out var tileQueryHandler);
            game.RegisterComponent<ContainersQuery>(game.BaseContainer, true, out var containersQueryHandler);
            game.RegisterComponent<GetComponentsQuery>(game.BaseContainer, true, out var componentsQuery);
            game.RegisterComponent<MovementRequest>(game.BaseContainer, true, out var movementRequestHandler);

            game.Server?.RegisterHandler(QueryType.Tile, tileQueryHandler);
            game.Server?.RegisterHandler(QueryType.ContainerCount, containersQueryHandler);
            game.Server?.RegisterHandler(QueryType.SendComponents, componentsQuery);
            game.Server?.RegisterHandler(QueryType.InputTileMove, movementRequestHandler);
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
        }

        game.Start();
        game.Server?.Cleanup();
        game.Client?.Cleanup();

        Console.Write("saturn was here");
    }

}
