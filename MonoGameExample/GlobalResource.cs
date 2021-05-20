using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameExample
{
    
    public class GlobalResource
    {
        public GlobalResource(Framework framework)
        {
            _framework = framework;
        }

        private readonly Framework _framework;
        private Dictionary<Font, SpriteFont> _fonts;
        
        public enum Font
        {
            DefaultPixel
        }

        public void LoadContent()
        {
            _fonts = new Dictionary<Font, SpriteFont>();
            _fonts.Add(Font.DefaultPixel, _framework.Content.Load<SpriteFont>("04B09"));
        }


        public SpriteFont GetFont(Font font)
        {
            return _fonts[font];
        }
    }
}
