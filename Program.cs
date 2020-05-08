using System;

namespace lumos
{
    class Program
    {
        static void Main(string[] args)
        {
            GameWindow window = new GameWindow("lumos");
            new Game(window);
            window.Run();
        }
    }
}
