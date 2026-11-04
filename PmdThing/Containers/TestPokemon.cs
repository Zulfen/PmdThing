using System.Diagnostics;
using PmdThing.Components;
using PmdThing.Core;
using PmdThing.Core.Util;

namespace PmdThing.Containers
{
    public class TestPokemon : PmdContainer
    {

        public TestPokemon()
        {
            
            var window = Framework.Graphics.GetMainWindow();
            List<int> frameDurations = [40, 12, 20, 12];

            RegisterComponent<Transform>
            (
                true,
                0,
                0
            );

            //RegisterComponent<SpriteSheet>
            //(
            //    true,
            //    @"C:\Users\Joshua Passmore\source\repos\PmdThing\PmdThing\Sprites\Vulpix\Idle-Anim.png",
            //    32, 
            //    32
            //);

            RegisterComponent<Animation>(true, PmdConstants.Pokemon.Vulpix);

            //RegisterComponent<DebugDraw>(true);

            RegisterComponent<TiledTransform>
            (
                true,
                0,
                0
            );

            RegisterComponent<KeyboardInput>(true);


        }

    }
}
