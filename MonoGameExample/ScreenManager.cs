using Microsoft.Xna.Framework;

namespace MonoGameExample
{
    public class ScreenManager : GameComponent
    {
        public ScreenManager(Framework framework) : base(framework)
        {
            _framework = framework;
        }

        private readonly Framework _framework;
        private Screen _currentScreen;

        public void LaunchNarrowPhase()
        {
            // TODO: Unload screen
            _currentScreen?.Dispose();

            _currentScreen = new NarrowPhase(_framework);
            _currentScreen.Initialize();
            _framework.Components.Add(_currentScreen);
        }
    }
}