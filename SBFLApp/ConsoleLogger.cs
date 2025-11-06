using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBFLApp
{
    internal static class ConsoleLogger
    {
        /// <summary>
        /// Write to the console using the <see cref="ConsoleColor.Cyan"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        public static void Info(string message)
        {
            ConsoleWriteLine($"Info: {message}", ConsoleColor.Cyan);

        }

        /// <summary>
        /// Write to the console using the <see cref="ConsoleColor.DarkYellow"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        public static void Warning(string message)
        {
            ConsoleWriteLine($"Warning: {message}", ConsoleColor.DarkYellow);
        }

        /// <summary>
        /// Write to the console using the <see cref="ConsoleColor.Red"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        public static void Error(string message)
        {
            ConsoleWriteLine($"Error: {message}", ConsoleColor.Red);
        }

        /// <summary>
        /// Write a message with an appended newline to the console using the specified <see cref="ConsoleColor"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        /// <param name="color">The color to use.</param>
        private static void ConsoleWriteLine(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Write a message to the console using the specified <see cref="ConsoleColor"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        /// <param name="color">The color to use.</param>
        static void ConsoleWrite(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }
    }
}
