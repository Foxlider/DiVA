using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiVA.Helpers;
using DiVA.Services;
using DiVA.Services.YouTube;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DiVA
{
    public class DiVA
    {
        private CommandService _commands;
        public static DiscordSocketClient Client;
        private readonly IServiceProvider _services;
        public static IConfigurationRoot Configuration;
        internal static int LogLvl = 3;

        static void Main(string[] args)
        {
            try
            {
                RunAsync(args).GetAwaiter().GetResult();
                Logger.Log(Logger.Info, "DiVA Exiting", "Main");
            }
            catch (Exception e)
            {
                Logger.Log(Logger.Error, $"An error occured : {e.Message}\nSOURCE : {e.Source}\nTRIGGERED BY : {e.InnerException?.Message}\nSTACKTRACE{e.StackTrace}", "Main");
            }
        }

        public static async Task RunAsync(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            var diVa = new DiVA(args);
            await diVa.RunAsync();
        }

        public DiVA(string[] args)
        {
            TryGenerateConfiguration();
            var builder = new ConfigurationBuilder()        // Create a new instance of the config builder
                .SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
                .AddJsonFile("config.json");        // Add this (json encoded) file to the configuration
            Configuration = builder.Build();                // Build the configuration

            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _services = serviceCollection.BuildServiceProvider();

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "--cache_cleanup":
                    case "-c":
                        CacheCleanup();
                        break;
                    case "--verbose":
                    case "-v":
                        LogLvl = 5;
                        Logger.Log(Logger.Verbose, "Verbose logging activated.", "Argument Handler");
                        break;
                    case "--help":
                    case "-h":
                        EchoHelp();
                        break;
                }
            }
        }

        private void EchoHelp()
        {
            Logger.Log(Logger.Neutral, $" __[ DiVA v{GetVersion()} ]__" +
                $"" +
                $"Project Webpage: https://github.com/Foxlider/FoxliBot/tree/DiVA" +
                $"" +
                $"" +
                $"Usage: ./DiVA [*parameters]" +
                $"" +
                $"Options" +
                $"  -c --cache_cleanup          Delete the cache folder and its content." +
                $"  -v --verbose                Display more information in the console for debug." +
                $"  -h --help                   Print this message and exit" +
                $"", "Help");
            Environment.Exit(0);
        }

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            string version = $"{Assembly.GetExecutingAssembly().GetName().Name} v{GetVersion()}";
            Logger.Log(Logger.Neutral,
                       $"Booting up...\n"
                     + $"┌─{new string('─', version.Length )}─┐\n"
                     + $"│ {version} │\n"
                     + $"└─{new string('─', version.Length )}─┘\n", "DiVA start");
            Client = new DiscordSocketClient(new DiscordSocketConfig
            { LogLevel = LogSeverity.Debug });
            Client.Log += LogMessage;
            _commands = new CommandService();

            //services.GetService<AudioService>().AudioPlaybackService = services.GetService<AudioPlaybackService>();

            //services = new ServiceCollection().BuildServiceProvider();             // Create a new instance of a service collection

            await InstallCommands();
            if (Configuration["tokens:discord"] == null || Configuration["tokens:discord"] == "" ||
                Configuration["tokens:youtube"] == null || Configuration["tokens:youtube"] == "")
            {
                Logger.Log(Logger.Error, "Impossible to read Configuration.", "DiVA Login");
                Logger.Log(Logger.Neutral, "Do you want to edit the configuration file ? (Y/n)\n", "DiVA Login");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                { EditToken(); }
                else
                {
                    Logger.Log(Logger.Warning, "Shutting Down...\nPress Enter to continue.", "DiVA Logout");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }
            await Client.LoginAsync(TokenType.Bot, Configuration["tokens:discord"]);
            await Client.StartAsync();

            Client.Ready += async () =>
            {
                Logger.Log(Logger.Neutral, $"{Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator} is connected !\n\n__[ CONNECTED TO ]__\n  ┌─", "DiVA Login");
                foreach (var guild in Client.Guilds)
                {
                    Logger.Log(Logger.Neutral,
                               $"  │┌───────────────\n  ││ {guild.Name} \n  ││ Owned by {guild.Owner.Nickname}#{guild.Owner.Discriminator}\n  ││ {guild.MemberCount} members\n  │└───────────────", "DiVA Login");
                }
                Logger.Log(Logger.Neutral, "  └─", "DiVA Login");
                Console.Title = $"{Assembly.GetExecutingAssembly().GetName().Name} v{GetVersion()}";
                await SetDefaultStatus(Client);
            };
            await Task.Delay(-1);
        }

        /// <summary>
        /// Install commands for the bot
        /// </summary>
        /// <returns></returns>
        public async Task InstallCommands()
        {
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            // Hook the MessageReceived Event into our Command Handler
            _commands.CommandExecuted += OnCommandExecuteAsync;
            Client.MessageReceived += HandleCommand;

            Client.UserLeft += UserLeftGuildHandler;
            Client.UserJoined += UserJoinedGuildHandler;
        }

        /// <summary>
        /// Get services set up
        /// </summary>
        /// <param name="serviceCollection"></param>
        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(new YouTubeDownloadService());
            //serviceCollection.AddSingleton(new AudioPlaybackService());
            serviceCollection.AddSingleton(new Logger());
            serviceCollection.AddSingleton(new AudioService());
        }

        /// <summary>
        /// Post-Command Handler
        /// </summary>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static async Task OnCommandExecuteAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // We have access to the information of the command executed,
            // the context of the command, and the result returned from the
            // execution in this event.
            var argPos = 0;
            if (!(context.Message.HasStringPrefix(Configuration["prefix"], ref argPos) || context.Message.HasMentionPrefix(Client.CurrentUser, ref argPos) || context.Message.Author.IsBot))
            { return; }
            var commandName = command.IsSpecified ? command.Value.Name : "A command";
            // We can tell the user what went wrong
            if (!string.IsNullOrEmpty(result?.ErrorReason))
            {
                Logger.Log(new LogMessage(LogSeverity.Warning, "CMDExecution", $"{commandName} was called and failed : {result.ErrorReason}."));
                var msg = await context.Channel.SendMessageAsync(result.ErrorReason);
                await Task.Delay(2000);
                await msg.DeleteAsync();
            }
            Logger.Log(new LogMessage(LogSeverity.Info, "CMDExecution", $"{commandName} was executed."));
        }

        /// <summary>
        /// Handle every Discord command
        /// </summary>
        /// <param name="messageParam"></param>
        /// <returns></returns>
        private async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message))
            { return; }

            //Log every private message received
            if (message.Channel is IPrivateChannel)
            { Logger.Log(Logger.Neutral, $"{message.Author} in {message.Channel.Name}\n{"└─".PadLeft(message.Author.ToString().Length - 3)}{ message.Content}", "DirectMessage"); }

            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;
            if (!(message.HasStringPrefix(Configuration["prefix"], ref argPos))) { return; }
            if (message.HasMentionPrefix(Client.CurrentUser, ref argPos)) { return; }
            if (message.Author.IsBot) { return; }

            // Create a Command Context
            var context = new CommandContext(Client, message);
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        /// <summary>
        /// Handling user joining guild
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private static async Task UserJoinedGuildHandler(SocketGuildUser param)
        {
            Random rnd = new Random();
            var channel = Client.GetChannel(param.Guild.DefaultChannel.Id) as SocketTextChannel;
            await CommandHelper.SayHelloAsync(channel, Client, param, rnd);
        }

        /// <summary>
        /// Handling user leaving guild
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private static async Task UserLeftGuildHandler(SocketGuildUser param)
        {
            if (Client.GetChannel(param.Guild.DefaultChannel.Id) is SocketTextChannel channel)
            { await channel.SendMessageAsync($"{param.Nickname} ({param.Username}) left us... Say bye ! "); }
        }

        /// <summary>
        /// Shutdown procedure
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static async void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            try
            { await Client.LogoutAsync(); }
            catch { /*ignored*/ }
            finally
            { Client.Dispose(); }
            Logger.Log(Logger.Warning, "Shutting Down...", "DiVA Logout");
            Environment.Exit(0);
        }

        /// <summary>
        /// Log system
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Task LogMessage(LogMessage message)
        {
            Logger.Log(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Configuration generator
        /// </summary>
        /// <returns></returns>
        private static bool TryGenerateConfiguration()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(filePath))
            { return false; }
            object config = new DiVAConfiguration();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
            return true;
        }

        private static void EditToken()
        {
            const string url = "https://discordapp.com/developers/applications/538306821333712916/bots";
            BrowerLauncher(url);
            Logger.Log(Logger.Neutral, "Please enter the bot's token below.\n", "DiVA Login");
            var answer = Console.ReadLine();
            Configuration["tokens:discord"] = answer;
            EditKey();
        }
        private static void EditKey()
        {
            const string url = "https://console.developers.google.com/apis/credentials?project=diva-discord";
            BrowerLauncher(url);
            Logger.Log(Logger.Neutral, "Please enter the DiVA API Key below.\n", "DiVA Login");
            string answer = Console.ReadLine();
            Configuration["tokens:youtube"] = answer;
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            object config = new DiVAConfiguration(Configuration["prefix"], new Tokens(Configuration["tokens:discord"], Configuration["tokens:youtube"]));
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.Delete(filePath);
            File.WriteAllText(filePath, json);
        }

        private static void BrowerLauncher(string url)
        {
            try
            { Process.Start(url); }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                { Process.Start("xdg-open", url); }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                { Process.Start("open", url); }
                else
                { throw; }
            }
        }

        private static void CacheCleanup()
        {
            try
            {
                var cachePath = Path.Combine(AppContext.BaseDirectory, "Songs");
                DirectoryInfo d = new DirectoryInfo(cachePath);
                Logger.Log(Logger.Info, $"Searching for files in folder \n{cachePath}", "DiVA Login");
                if (d.GetFiles().Length > 0)
                {
                    Logger.Log(Logger.Info, $"Found {d.GetFiles().Length} files : ", "DiVA Login");
                    foreach (FileInfo file in d.GetFiles())
                    { Logger.Log(Logger.Info, $"\tFile {file.Name}", "DiVA Login"); }

                    Logger.Log(Logger.Neutral, "Do you want to erase all cache ? (Y/n)\n", "DiVA Login");
                    var answer = Console.ReadKey();
                    Logger.Log(Logger.Neutral, "", "DiVA Login");
                    if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                    {
                        Logger.Log(Logger.Info, "Deleting files... Please wait...", "DiVA Login");
                        foreach (FileInfo file in d.GetFiles())
                        { file.Delete(); }
                        Logger.Log(Logger.Info, "Proceeding with launch.", "DiVA Login");
                    }
                    else
                    { Logger.Log(Logger.Info, "Keeping cache files. Proceeding with launch...", "DiVA Login"); }
                }
                else
                { Logger.Log(Logger.Info, "No file found. Proceeding with launch...", "DiVA Login"); }
            }
            catch (DirectoryNotFoundException)
            { Logger.Log(Logger.Info, $"No cache folder detected... Proceeding with launch...", "DiVA Login"); }
            catch (Exception e)
            { Logger.Log(Logger.Error, $"An error occured. Please check and remove the cache manually : \n{e.Message}", "DiVA Login"); }
        }

        /// <summary>
        /// Setting current status
        /// </summary>
        public static async Task SetDefaultStatus(DiscordSocketClient client)
        { await client.SetGameAsync($"Discord Virtual Assistant v{GetVersion()}", type: ActivityType.Watching); }

        /// <summary>
        /// Get current version
        /// </summary>
        /// <returns></returns>
        public static string GetVersion()
        {
            string rev = $"{(char)(Assembly.GetExecutingAssembly().GetName().Version.Build + 97)}";
#if DEBUG
            rev += "-debug";
#endif
            return $"{Assembly.GetExecutingAssembly().GetName().Version.Major}."
                 + $"{Assembly.GetExecutingAssembly().GetName().Version.Minor}"
                 + $"{rev}";
        }

    }
}
