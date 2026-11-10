using PmdThing.Core;

namespace PmdThing.Components.Graphics.UI.Window
{
    public class WindowUiResources : PmdComponent
    {
        private readonly SpriteSheet _uiSheet;

        public WindowUiResources(PmdComponentConfig config) : base(config)
        {
            Register<SpriteSheet>(true, out var uiSheet, "UI/Window.png", 16, 16);
            _uiSheet = uiSheet;
        }

        public override void Cleanup()
        {
            _uiSheet.Cleanup();
        }

        public void DrawWindow(UiBounds bounds)
        {
            float screenSpaceY = 0;
            float screenSpaceX = 0;

            for (var row = 0; row < bounds.TileDimensions.Height; row++)
            {
                for (var column = 0; column < bounds.TileDimensions.Width; column++)
                {
                    int sheetRow;
                    int sheetColumn;
                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                    if (row == 0 && column == 0)
                    {
                        sheetRow = 3;
                        sheetColumn = 1;
                    }
                    else if (column == bounds.TileDimensions.Width - 1 && row == 0)
                    {
                        sheetRow = 0;
                        sheetColumn = 2;
                    }
                    else if (column == bounds.TileDimensions.Width - 1 && row == bounds.TileDimensions.Height - 1)
                    {
                        sheetRow = 2;
                        sheetColumn = 0;
                    }
                    else if (column == bounds.TileDimensions.Width - 1)
                    {
                        sheetRow = 1;
                        sheetColumn = 1;
                    }
                    else if (row == bounds.TileDimensions.Height - 1 && column == 0)
                    {
                        sheetRow = 1;
                        sheetColumn = 0;
                    }
                    else if (row == bounds.TileDimensions.Height - 1)
                    {
                        sheetRow = 0;
                        sheetColumn = 0;
                    }
                    else if (column == 0)
                    {
                        sheetRow = 3;
                        sheetColumn = 0;
                    }
                    else if (row == 0)
                    {
                        sheetRow = 2;
                        sheetColumn = 1;
                    }
                    else
                    {
                        sheetRow = 0;
                        sheetColumn = 1;
                    }

                    _uiSheet.DrawSprite(bounds.ScreenTopLeft.X + screenSpaceX, bounds.ScreenTopLeft.Y + screenSpaceY, sheetRow, sheetColumn, false);

                    screenSpaceX += _uiSheet.GetScaledWidth();
                }

                screenSpaceX = 0;
                screenSpaceY += _uiSheet.GetScaledHeight();

            }
        }
    }
}
