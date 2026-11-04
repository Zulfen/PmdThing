using PmdThing.Components.Graphics;
using PmdThing.Components.Graphics.OpenGL;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;

namespace PmdThing.Containers
{

    [PmdServerSync]
    public class DrawableTextHandler : PmdMultiContainer
    {

        private record CharData(int Row, int Column);

        private readonly SpriteSheet? _fontSheet;

        // TODO: xml
        private readonly Dictionary<char, CharData> _charMap = new()
        {
            ['a'] = new CharData(0, 0),
            ['b'] = new CharData(0, 1),
            ['c'] = new CharData(0, 2),
            ['d'] = new CharData(0, 3),
            ['e'] = new CharData(0, 4),
            ['f'] = new CharData(0, 5),
            ['g'] = new CharData(0, 6),
            ['h'] = new CharData(0, 7),
            ['i'] = new CharData(0, 8),
            ['j'] = new CharData(0, 9),
            ['k'] = new CharData(0, 10),
            ['l'] = new CharData(0, 11),
            ['m'] = new CharData(0, 12),
            ['n'] = new CharData(1, 0),
            ['o'] = new CharData(1, 1),
            ['p'] = new CharData(1, 2),
            ['q'] = new CharData(1, 3),
            ['r'] = new CharData(1, 4),
            ['s'] = new CharData(1, 5),
            ['t'] = new CharData(1, 6),
            ['u'] = new CharData(1, 7),
            ['v'] = new CharData(1, 8),
            ['w'] = new CharData(1, 9),
            ['x'] = new CharData(1, 10),
            ['y'] = new CharData(1, 11),
            ['z'] = new CharData(1, 12)
        };

        public DrawableTextHandler(PmdContainerConfig config) : base(config)
        {
            if (!Framework.Graphics
                .GetCapabilities()
                .Headless)
            {
                // TODO: basic shared shader?
                RegisterComponent<GlShader>(true,
                    @"Core\Hardware\Graphics\Shaders\Vertex\shader.vert",
                    @"Core\Hardware\Graphics\Shaders\Fragment\shader.frag",
                    "vertex");

                RegisterComponent<SpriteSheet>(true, out var sheet, "Fonts/Default.png", 15, 13);
                _fontSheet = sheet;
            }
        }

        public int AddLabel(string text, int x, int y)
        {
            return RegisterContainer<TextContainer>(text, x, y);
        }

        public void DrawString(string text, int x, int y, int spacing = 1)
        {
            var currentX = x;
            foreach (var c in text)
            {
                if (_charMap.TryGetValue(c, out var charData))
                {
                    _fontSheet?.DrawSprite(currentX, y, charData.Row, charData.Column, false);
                }
                currentX += _fontSheet?.GetSpriteWidth() ?? 0 + spacing;
            }
        }
    }
}
