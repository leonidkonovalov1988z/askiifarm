using System.Runtime.InteropServices.JavaScript;

namespace askiifarm
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Random rnd = new Random();
            using var console = new DoubleBufferedConsole(
            width: 80,
            height: 25,
            defaultForeground: ConsoleColor.Gray,
            defaultBackground: ConsoleColor.Black);

            while (true)
            {
                for (int i = 0; i < console.Width; i++)
                {
                    for (int j = 0; j < console.Height; j++)
                    {
                        console.PutChar(
                            i,j,
                            (char)('A'+rnd.Next(26)), 
                            (ConsoleColor)rnd.Next(16), 
                            (ConsoleColor)rnd.Next(16));
                    }
                }
                console.Draw();
            }
        }
    }
}
