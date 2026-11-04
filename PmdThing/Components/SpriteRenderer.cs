using GlmSharp;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Util;
using System.Xml.Serialization;
using static PmdThing.Core.Util.PmdConstants;

namespace PmdThing.Components
{
    public class SpriteRenderer : PmdComponent
    {

        private int _frameCounter;
        private int _columnIndex;
        private int _rowIndex;

        private SpriteSheet? _spriteSheet;
        private readonly Transform _transform;
        private readonly TiledWorldRenderer _worldRenderer;

        private readonly Pokemon _currentPokemon;
        private AnimationType _currentAnimation;

        private vec2 _spriteCenter = vec2.Zero;

        public AnimationType CurrentAnimation
        {
            get => _currentAnimation;
            set
            {
                _currentAnimation = value;

                if (!_animations.TryGetValue(_currentAnimation, out var animation))
                {
                    return;
                }

                _spriteSheet = animation.SpriteSheet;
                _columnIndex = 0;
                _frameCounter = 0;

                if (_spriteSheet != null)
                {
                    _spriteCenter = new vec2(_spriteSheet.GetSpriteWidth() / 2f, _spriteSheet.GetSpriteHeight() / 2f);
                }
            }
        }

        private readonly Dictionary<AnimationType, PmdAnimation> _animations;

        public SpriteRenderer(PmdComponentConfig config, Pokemon pokemon) : base(config)
        {
            DoesRendering = true;
            Priority = 1;

            _currentPokemon = pokemon;
            _transform = GetParentComponent<Transform>();

            GetSharedComponent<Timer>()
                .RegisterTask(Animation_FixedUpdate);

            _animations = LoadAllAnimations();
            CurrentAnimation = AnimationType.Idle;

            _worldRenderer = GetBaseContainerComponent<TiledWorld, TiledWorldRenderer>();
        }

        private string GetDataPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Sprites", _currentPokemon.ToString());
        }

        public string GetImagePath(AnimationType type)
        {
            return Path.Combine(GetDataPath(), $"{type}-Anim.png");
        }

        private Dictionary<AnimationType, PmdAnimation> LoadAllAnimations()
        {
            var xmlPath = Path.Combine(GetDataPath(), "AnimData.xml");
            Dictionary<AnimationType, PmdAnimation> animations = new();

            try
            {
                var serializer = new XmlSerializer(typeof(PmdXmlSprite));

                using var reader = new StreamReader(xmlPath);
                var xmlSprite = (PmdXmlSprite?)serializer.Deserialize(reader);

                if (xmlSprite == null) return animations;

                foreach (var xmlAnimation in xmlSprite.Animations)
                {
                    if (!Enum.TryParse<AnimationType>(xmlAnimation.Name, true, out var type))
                    {
                        continue;
                    }

                    if (xmlAnimation.CopyOf.HasValue)
                    {
                        if (animations.TryGetValue(xmlAnimation.CopyOf.Value, out var existingAnimation))
                        {
                            animations.Add(type, existingAnimation);
                            continue;
                        }
                    }

                    Register<SpriteSheet>(
                        true,
                        out var spriteSheet,
                        GetImagePath(type),
                        xmlAnimation.Width,
                        xmlAnimation.Height
                    );

                    var pmdAnimation = new PmdAnimation
                    {
                        Durations = xmlAnimation.Durations,
                        SpriteSheet = spriteSheet,
                        OffsetX = xmlAnimation.OffsetX ?? 0,
                        OffsetY = xmlAnimation.OffsetY ?? 0,
                    };

                    animations.Add(type, pmdAnimation);
                }

            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not deserialize animation data: " + ex);
            }

            return animations;

        }

        public override void Render()
        {
            vec2 spritePosition;
            if (_transform.IsTarget)
            {
                spritePosition = _worldRenderer.ScreenCenter - _spriteCenter;
            }
            else
            {
                spritePosition = _worldRenderer.ScreenCenter - _spriteCenter - _worldRenderer.Offset + _transform.ToVec2();
            }

            _spriteSheet?.DrawSprite(
                spritePosition.x + _animations[_currentAnimation].OffsetX,
                spritePosition.y + _animations[_currentAnimation].OffsetY,
                _rowIndex,
                _columnIndex,
                false
                );
        }

        public void Animation_FixedUpdate()
        {
            var durations = _animations[_currentAnimation].Durations;

            if (_frameCounter > durations.Max())
            {
                _frameCounter = 0;
            }
            else
            {
                _frameCounter++;
            }

            if (_frameCounter != durations[_columnIndex]) return;


            if (_columnIndex < durations.Length - 1)
            {
                _columnIndex++;
            }
            else if (_columnIndex == durations.Length - 1)
            {
                _columnIndex = 0;
            }
        }

        public void SetAnimationDirection(AnimationDirection direction)
        {
            _rowIndex = (int)direction;
        }

        public override void Cleanup()
        {
            //_spriteSheet?.Cleanup();
        }

    }
}
