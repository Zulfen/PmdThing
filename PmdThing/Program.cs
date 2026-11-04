using PmdThing.Components;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;

public class Program
{

    public static void Main(string[] args)
    {
        var game = PmdGameBase.Create(1280, 720, "ksjdfnsfklm;dsk");

        // Make a generic shared container. Was going to do this in GameBase but decided not to.
        // TODO: Maybe removed the shared components feature and enforce passing around components from a main container like thi

        var tmxLoader = new TmxTileProvider(@"C:\Users\Joshua Passmore\source\repos\PmdThing\PmdThing\TileSets\142map.tmx");
        game.RegisterContainer<TiledWorld>(out var tiledGame, tmxLoader);

        game.RegisterComponent<Camera>(game.BaseContainer,false, out _);

        game.RegisterContainer<TestPokemon>(out _, PmdConstants.Pokemon.Vulpix, true);
        game.RegisterContainer<TestPokemon>(out _, PmdConstants.Pokemon.Eevee, false);

        game.Start();

        Console.Write("saturn was here");


    }

}
