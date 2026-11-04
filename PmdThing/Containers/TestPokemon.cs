using Microsoft.Extensions.Logging;
using PmdThing.Components;
using PmdThing.Components.Graphics.OpenGL;
using PmdThing.Core;
using PmdThing.Core.Hardware.Framework;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using static PmdThing.Core.Util.PmdConstants;

namespace PmdThing.Containers
{
    [PmdServerSync]
    public class TestPokemon : PmdContainer
    {

        private readonly SpriteRenderer? _renderer;
        private readonly TiledTransform _tiledTransform;

        private readonly bool _player;
        private readonly Pokemon _pokemon;

        public TestPokemon(PmdContainerConfig config, Pokemon pokemon, bool player) : base(config)
        {
            var capabilities = Framework.Graphics.GetCapabilities();

            _player = player && !capabilities.Headless;
            _pokemon = pokemon;
            

            if (capabilities.Api == PmdGraphics.HardwareApi.OpenGl)
            {
                RegisterComponent<GlShader>(true,
                    @"Core\Hardware\Graphics\Shaders\Vertex\shader.vert",
                    @"Core\Hardware\Graphics\Shaders\Fragment\shader.frag",
                    "vertex");
            }

            RegisterSyncedComponent
            (
                true,
                out _tiledTransform,
                $"{_pokemon}_tile",
                0,
                0
            );

            

            if (!capabilities.Headless)
            {
                RegisterComponent<SpriteRenderer>(true, out var pokemonRenderer, _pokemon);
                _renderer = pokemonRenderer;
            }
            else
            {
                RegisterIfNotExist<Collision>(false, out var collision, _tiledTransform);
                collision.RegisterTransform(_tiledTransform);
                collision.CollisionChange += PokemonCollisionChange;
            }

            if (_player)
            {
                GetIfExists<Camera>(false)?
                    .SetTarget(this);
                RegisterComponent<TiledInput>(true);
            }

           
            _tiledTransform.Moving.Changed += Moving_Changed;
            _tiledTransform.Direction.Changed += Direction_Changed;

            Framework.Logger.LogInformation("Spawned Pokémon {pokemon}. Player: {player}", pokemon, player);

        }

        private void PokemonCollisionChange(bool colliding, PmdTileRef tile)
        {
            
            if (!_player && colliding && tile == _tiledTransform.ComponentValue)
            {
                Framework.Logger.LogDebug("Collision detected with {pokemon} at x={column}, y={row}", _pokemon, tile.Column, tile.Row);
                _tiledTransform.MoveInDirection(AnimationDirection.South);
            }
            else
            {
                _tiledTransform.CanMove.Set(!colliding);
            }
            
        }

        private void Direction_Changed(AnimationDirection newDirection, AnimationDirection prevDirection)
        {
            _renderer?.SetAnimationDirection(newDirection);
        }

        private void Moving_Changed(bool currentValue, bool previousValue)
        {
            _renderer?.CurrentAnimation = currentValue ? AnimationType.Walk : AnimationType.Idle;
        }

    }
}
