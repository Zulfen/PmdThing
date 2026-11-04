using PmdThing.Core;
using PmdThing.Core.Util;
using System.Xml.Serialization;
using static PmdThing.Core.Util.PmdConstants;

namespace PmdThing.Components
{
    public class Animation : PmdComponent
    {

        private int _frameCounter;
        private int _columnIndex;
        private int _rowIndex;

        private SpriteSheet? _spriteSheet;

        private readonly Transform _transform;

        private readonly Pokemon _currentPokemon;
        private AnimationType _currentAnimation;

        public AnimationType CurrentAnimation
        {
            get => _currentAnimation;
            set
            {
                _currentAnimation = value;

                if (_animations.TryGetValue(_currentAnimation, out var animation))
                {
                    _spriteSheet = animation.SpriteSheet;
                }
            }
        }

        private readonly Dictionary<AnimationType, PmdAnimation> _animations;

        public Animation(PmdComponentConfig config, Pokemon pokemon) : base(config)
        {

            DoesRendering = true;
            _currentPokemon = pokemon;
            _transform = GetParentComponent<Transform>();

            var timer = GetSharedComponent<Timer>();
            timer.RegisterTask(Animation_FixedUpdate);

            _animations = LoadAllAnimations();
            CurrentAnimation = AnimationType.Idle;

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
                    if (Enum.TryParse<AnimationType>(xmlAnimation.Name, true, out var type))
                    {

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
                        };

                        animations.Add(type, pmdAnimation);
                    }
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

            if (_columnIndex == _spriteSheet?.GetColumns() - 1)
            {
                _columnIndex = 0;
            }

            var spritePosition = _transform.GetPosition();
            // draw row zero for now as just want to see if this works at all
            _spriteSheet?.DrawSprite(
                (int)spritePosition.x,
                (int)spritePosition.y,
                _rowIndex,
                _columnIndex, false);

        }

        public void Animation_FixedUpdate()
        {

            _frameCounter++;

            if (_frameCounter > 
                    _animations[_currentAnimation]
                    .Durations
                    .Max())
            {
                _frameCounter = 0;
            }

            if (_frameCounter == _animations[_currentAnimation].Durations[_columnIndex])
            {
                _columnIndex++;
            }

        }

        public void ChangeDirection(AnimationDirection direction)
        {
            _rowIndex = (int)direction;
        }

        public override void Cleanup()
        {
            //_spriteSheet?.Cleanup();
        }

    }
}
