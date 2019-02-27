using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiVA
{
    /// <summary>
    /// Log service to handle Discord-side logs
    /// </summary>
    public class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        private string _logDirectory { get; }
        private string _logFile => Path.Combine(_logDirectory, $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}.txt");

        // DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
        /// <summary>
        /// Logging service
        /// </summary>
        /// <param name="discord"></param>
        /// <param name="commands"></param>
        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            _discord = discord;
            _commands = commands;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage msg)
        {
            if (!Directory.Exists(_logDirectory))     // Create the log directory if it doesn't exist
            { Directory.CreateDirectory(_logDirectory); }
            if (!File.Exists(_logFile))               // Create today's log file if it doesn't exist
            { File.Create(_logFile).Dispose(); }

            string logText = $"{DateTime.UtcNow.ToString("hh:mm:ss")} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
            File.AppendAllText(_logFile, logText + "\n");     // Write the log text to a file

            return Console.Out.WriteLineAsync(logText);       // Write the log text to the console
        }

        
    }

    /// <summary>
    /// Logging service to handle Application-side logs
    /// </summary>
    public class Log
    {
        /// <summary>
        /// Log message function handling Discord Logging
        /// </summary>
        /// <param name="Severity">Message's severity</param>
        /// <param name="message">Message's text</param>
        /// <param name="source">Message's source</param>
        public static void Message(LogSeverity Severity, string message, string source = "")
        {
            switch (Severity)
            {
                case LogSeverity.Critical:
                    Critical(message, source);
                    break;
                case LogSeverity.Error:
                    Error(message, source);
                    break;
                case LogSeverity.Warning:
                    Warning(message, source);
                    break;
                case LogSeverity.Info:
                    Information(message, source);
                    break;
                case LogSeverity.Verbose:
                    Verbose(message, source);
                    break;
                case LogSeverity.Debug:
                    Debug(message, source);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Logging debug (lvl 5)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        public static void Debug(string message, string source = "")
        {
            if (DiVA.logLvl >= 5)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                string Severity = "Debug".PadRight(8);
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Verbose (lvl 4)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        public static void Verbose(string message, string source = "")
        {
            if (DiVA.logLvl >= 4)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                string Severity = "Verbose".PadRight(8);
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Error (lvl 1)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        public static void Error(string message, string source = "")
        {
            if (DiVA.logLvl >= 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                string Severity = "Error".PadRight(8);
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Critical (lvl 0)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        public static void Critical(string message, string source = "")
        {
            if (DiVA.logLvl >= 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                string Severity = "Critical".PadRight(8);
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Information (lvl 3) DEFAULT VALUE
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        public static void Information(string message, string source = "")
        {
            if (DiVA.logLvl >= 3)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                string Severity = "Info".PadRight(8);
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Warning (lvl 2)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        public static void Warning(string message, string source = "")
        {
            if (DiVA.logLvl >= 2)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                string Severity = "Warning".PadRight(8);
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        public static void Neutral(string message, string source = "")
        {
            string Severity = "Normal".PadRight(8);
            string[] lines = message.Split("\n");
            foreach (var line in lines)
            { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}");}
            Console.ResetColor();
        }
    }
     
}