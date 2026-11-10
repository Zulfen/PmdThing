using Microsoft.Extensions.Logging;
using PmdThing.Components;
using PmdThing.Components.Graphics;
using PmdThing.Components.Graphics.OpenGL;
using PmdThing.Core;
using PmdThing.Core.Hardware.Graphics;
using PmdThing.Core.Network.Rokon;
using static PmdThing.Core.Util.PmdConstants;

namespace PmdThing.Containers
{
    [PmdServerSync]
    public class TestPokemon : PmdContainer
    {

        private readonly SpriteRenderer? _renderer;
        private readonly TiledTransform _tiledTransform;

        private readonly bool _player;
        public Pokemon Type { get; init; }

        public TestPokemon(PmdContainerConfig config, Pokemon type, bool player) : base(config)
        {

            _player = player;
            Type = type;

            RegisterSyncedComponent
            (
                true,
                out _tiledTransform,
                $"{Type}_tile",
                0,
                0
            );

            
            if (!IsOnServer())
            {
                RegisterComponent<SpriteRenderer>(true, out var pokemonRenderer, Type);
                _renderer = pokemonRenderer;
            }

            if (_player)
            {
                GetIfExists<Camera>(false)?
                    .SetTarget(this);

                if (!IsOnServer())
                {
                    RegisterComponent<TiledInput>(true);
                }

                RegisterComponent<PlayerCollision>(false, out var collision, _tiledTransform);
                collision.CollisionCheck += PlayerCollision_Changed;
            }
            else
            {
                var collision = GetIfExists<PlayerCollision>(false);
                collision?.RegisterTransform(_tiledTransform);
            }


            _tiledTransform.Moving.Changed += Moving_Changed;
            _tiledTransform.Direction.Changed += Direction_Changed;

            Framework.Logger.LogInformation("Spawned Pokémon {pokemon}. Player: {player}", type, player);

        }

        private void PlayerCollision_Changed(PmdContainer? sender, PlayerCollision.CollisionEventArgs e)
        {
            if (sender is TestPokemon otherPokemon)
            {
                Framework.Logger.LogDebug("Collision detected with {pokemon} and {otherPokemon} at x={column}, y={row}",
                    Type, otherPokemon.Type, e.Tile.Column, e.Tile.Row);

                var pokemonTransform = otherPokemon.GetComponent<TiledTransform>();
                pokemonTransform.MoveInDirection(AnimationDirection.South);
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
