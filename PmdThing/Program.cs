using PmdThing.Containers;
using PmdThing.Core;

public class Program
{

    private static TestPokemon? _vulpix;

    public static void Main(string[] args)
    {
        var game = PmdGameBase
            .Create(800, 600, "ksjdfnsfklm;dsk");

        
        game.RegisterContainer<TestPokemon>(out _vulpix);
        game.Start();




    }

}
