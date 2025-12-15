using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace RedStarLibrary.Helpers
{
    internal static class ConsoleHelper
    {
        public static void WriteError(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(message);
            StudioLogger.WriteError(message);

            Console.ForegroundColor = prevColor;
        }

        public static void WriteWarn(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(message);
            StudioLogger.WriteWarning(message);

            Console.ForegroundColor = prevColor;
        }

        public static void WriteLineInfo(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(message);
            StudioLogger.WriteLine(message);

            Console.ForegroundColor = prevColor;
        }
    }
}
