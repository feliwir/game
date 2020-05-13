using System;

namespace Viking
{
    class Program
    {
        static void Main(string[] args)
        {
            GameWindow window = new GameWindow("Viking");
            new Game(window);
            window.Run();
        }
    }
}
