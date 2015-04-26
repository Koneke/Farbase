using System;

namespace Farbase
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (fbApplication game = new fbApplication())
            {
                game.Run();
            }
        }
    }
#endif
}

