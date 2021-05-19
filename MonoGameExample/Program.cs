using System;

namespace MonoGameExample
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Framework())
                game.Run();
        }
    }
}
