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
            LaunchScreen(new NarrowPhase(_framework));
        }

        public void LaunchMainMenu()
        {
            LaunchScreen(new MainMenu(_framework));
        }

        public void LaunchPlayground()
        {
            LaunchScreen(new Playground(_framework));
        }

        private void LaunchScreen(Screen screen)
        {
            // Unload previous screen
            // TODO: unload properly
            _framework.Components.Remove(_currentScreen);
            _currentScreen?.Dispose();

            _currentScreen = screen;
            _currentScreen.Initialize();
            _framework.Components.Add(_currentScreen);

        }
    }
}