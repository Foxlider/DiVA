using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Discord;
using DiVA;

namespace DiVA.Services
{
    /// <summary>
    /// Logging service to handle Application-side logs
    /// </summary>
    public class Logger
    {
        public const int Neutral = -1;
        public const int Critical = 0;
        public const int Error = 1;
        public const int Warning = 2;
        public const int Info = 3;
        public const int Verbose = 4;
        public const int Debug = 5;

        private static readonly object Locked = new object();

        private static readonly List<(string, ConsoleColor)> LogLevels = new List<(string, ConsoleColor)>
        {
            //  (Item1, Item2)
            ("Critical", ConsoleColor.DarkRed),
            ("Error", ConsoleColor.Red),
            ("Warning", ConsoleColor.DarkYellow),
            ("Info", ConsoleColor.Green),
            ("Verbose", ConsoleColor.Gray),
            ("Debug", ConsoleColor.Blue),
            ("Neutral", ConsoleColor.White)
        };


        private static string LogDirectory { get; set; }
        private static string LogFile { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Logger()
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(LogDirectory))
            { Directory.CreateDirectory(LogDirectory); }
            LogFile = Path.Combine(LogDirectory, $"EvaLogs-{DateTime.Now:yyyy-MM-dd}.txt");
        }

        /// <summary>
        /// Log message function handling Discord Logging
        /// </summary>
        /// <param name="severity">Message's severity</param>
        /// <param name="message">Message's text</param>
        /// <param name="source">Message's source</param>
        public static void Log(int severity, string message, string source = "")
        {
            if (source == null)
            { source = ""; }
            LogToFile(severity, message, source);
            LogToConsole(severity, message, source);
        }

        public static void Log(LogMessage logMessage)
        {
            LogToFile((int)logMessage.Severity, logMessage.Message, logMessage.Source    ?? "");
            LogToConsole((int)logMessage.Severity, logMessage.Message, logMessage.Source ?? "");
        }

        private static void LogToFile(int severity, string message, string source)
        {
            lock (Locked)
            {
                var logfile = LogFile ?? Path.Combine(AppContext.BaseDirectory, "logs", $"EvaLogs-{DateTime.Now:yyyy-MM-dd}.txt");
                if (DiVA.LogLvl < severity) return;
                var currentlevel = LogLevels[(severity % LogLevels.Count + LogLevels.Count) % LogLevels.Count];
                var logName = currentlevel.Item1.PadRight(8);
                var formatLines = FormatFullText(message, $"[{logName} {source.PadLeft(20)}][{DateTime.Now.ToString(CultureInfo.CurrentCulture)}] : ").ToList();
                File.AppendAllLines(logfile, formatLines);
            }
        }

        /// <summary>
        /// Logging message to console
        /// </summary>
        /// <param name="severity">Severity level of the message</param>
        /// <param name="message">Message sent</param>
        /// <param name="source">Source of the message</param>
        private static void LogToConsole(int severity, string message, string source)
        {
            if (DiVA.LogLvl < severity) return;
            string logName;
            ConsoleColor consoleColor;
            lock (Locked)
            { (logName, consoleColor) = LogLevels[(severity % LogLevels.Count + LogLevels.Count) % LogLevels.Count]; }
            if (consoleColor != ConsoleColor.White)
            { Console.ForegroundColor = consoleColor; } //Change default color if needed
            foreach (var line in FormatFullText(message, $"[{logName.PadRight(8)} {source.PadLeft(15)}][{FormatDate()}] : "))
            { Console.WriteLine(line); }
            Console.ResetColor();
        }

        /// <summary>
        /// Simple date formatter
        /// </summary>
        /// <returns></returns>
        private static string FormatDate()
        { return DateTime.Now.ToString("HH:mm:ss"); }

        private static IEnumerable<string> FormatFullText(string message, string prefix)
        {
            var bufferLen = Console.BufferWidth;
            var prefixLen = prefix.Length;
            var lines = message.Split("\n");
            var result = new List<string>();
            foreach (var line in lines)
            {
                if (line.Length > bufferLen - prefixLen)
                {
                    var s = prefix;
                    var currLine = s;
                    foreach (var word in line.Split(' '))
                    {
                        if ($"{currLine} {word}".Length >= bufferLen)
                        {
                            s += $"\n{prefix}";
                            currLine = prefix;
                        }
                        currLine += " " + word;
                        s += " " + word;
                    }
                    result.Add(s.Split("\n")[0]);
                    result.Add(s.Split("\n")[1]);
                }
                else
                { result.Add($"{prefix} {line}"); }
            }
            return result.ToArray();
        }

        
    }
}
