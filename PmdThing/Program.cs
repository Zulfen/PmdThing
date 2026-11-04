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

        RokonSocketServer? server = null;
        RokonClientConnection? client = null;
        if (headless)
        {
            game.RegisterComponent<RokonSocketServer>(game.BaseContainer, false, out server, 2312);
            var tmxLoader = new TmxTileProvider(@"TileSets\142map.tmx");
            game.RegisterContainer<TiledWorld>(out _,tmxLoader);

            game.RegisterComponent<TileQuery>(game.BaseContainer, true, out var tileQueryHandler);
            game.RegisterComponent<ContainersQuery>(game.BaseContainer, true, out var containersQueryHandler, server);
            game.RegisterComponent<GetComponentsQuery>(game.BaseContainer, true, out var componentsQuery, server);
            server.RegisterHandler(QueryType.Tile, tileQueryHandler);
            server.RegisterHandler(QueryType.ContainerCount, containersQueryHandler);
            server.RegisterHandler(QueryType.SendComponents, componentsQuery);
        }
        else
        {
            game.RegisterComponent<RokonClientConnection>(game.BaseContainer, false, out client, "localhost", 2312);
            game.RegisterComponent<RokonTileProvider>(game.BaseContainer, true, out var networkLoader);
            game.RegisterContainer<TiledWorld>(out var tiledGame, networkLoader);
        }
        

        // Make a generic shared container. Was going to do this in GameBase but decided not to.
        // TODO: Maybe removed the shared components feature and enforce passing around components from a main container like thi

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
        server?.Cleanup();
        client?.Cleanup();

        Console.Write("saturn was here");
    }

}
