namespace Farbase
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            using (fbApplication game = new fbApplication())
            {
                game.Run();
            }
        }
    }
#endif
}

