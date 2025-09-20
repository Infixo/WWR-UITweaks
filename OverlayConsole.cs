using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TestMod;

#pragma warning disable CA1416 // Validate platform compatibility
public static class OverlayConsole
{
    // Access Game object; the game is already running, so we can do it here
    private static Game _game = STM.Main.Main_window;

    private static SpriteBatch? _spriteBatch;
    private static SpriteFont? _font;

    public static void Initialize()
    {
        try
        {
            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);
            // Load the font from your Content Pipeline
            _font = _game.Content.Load<SpriteFont>("Arial");
        }
        catch (Exception ex)
        {
            Log.Write(ex.Message);
        }
        Output("HELLO WORLD!");
    }

    public static void Output(string msg)
    {
        if (_spriteBatch == null)
        {
            Log.Write("Warning. SpriteBatch not initialized.");
            return;
        }
        try
        {
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, msg, new Vector2(10, 10), Color.White);
            _spriteBatch.End();
        }
        catch (Exception ex)
        {
            Log.Write(ex.Message);
        }

    }
}
#pragma warning restore CA1416 // Validate platform compatibility
