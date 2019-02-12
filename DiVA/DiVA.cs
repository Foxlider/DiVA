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
    class DiVA
    {
        private CommandService commands;
        public static DiscordSocketClient client;
        private IServiceProvider services;
        public static IConfigurationRoot Configuration;
        public static bool DEV_MODE = false;
        public static bool verbose = false;
        public static ushort logLvl = 3;

        static void Main(string[] args) => RunAsync(args).GetAwaiter().GetResult();

        public static async Task RunAsync(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            var DiVA = new DiVA(args);
            await DiVA.RunAsync();
        }


        public DiVA(string[] args)
        {
            TryGenerateConfiguration();
            var builder = new ConfigurationBuilder()        // Create a new instance of the config builder
                .SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
                .AddJsonFile("config.json");        // Add this (json encoded) file to the configuration
            Configuration = builder.Build();                // Build the configuration

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
                        verbose = true;
                        logLvl = 5;
                        Log.Verbose("Verbose logging activated.", "Argument Handler");
                        break;
                    case "--help":
                    case "-h":
                        EchoHelp();
                        break;
                    default:
                        break;
                }
            }
        }

        private void EchoHelp()
        {
            Console.WriteLine($" __[ DiVA v{GetVersion()} ]__" +
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
                $"");
            Environment.Exit(0);
        }

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            Log.Neutral(
                $"Booting up...\n" +
                $"____________\n" +
                $"{Assembly.GetExecutingAssembly().GetName().Name} " +
                $"v{GetVersion()}\n" +
                $"____________\n");
            var loglvl = LogSeverity.Info;
            if (verbose)
            { loglvl = LogSeverity.Debug; }
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = loglvl
            });
            client.Log += LogMessage;
            commands = new CommandService();

            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            services = serviceCollection.BuildServiceProvider();

            services.GetService<AudioService>().AudioPlaybackService = services.GetService<AudioPlaybackService>();

            //services = new ServiceCollection().BuildServiceProvider();             // Create a new instance of a service collection

            await InstallCommands();
            if (Configuration["tokens:discord"] == null || Configuration["tokens:discord"] == "" ||
                Configuration["tokens:youtube"] == null || Configuration["tokens:youtube"] == "")
            {
                Log.Error("Impossible to read Configuration.", "DiVA Login");
                Log.Neutral("Do you want to edit the configuration file ? (Y/n)\n", "DiVA Login");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                { EditToken(); }
                else
                {
                    Log.Warning("Shutting Down...\nPress Enter to continue.", "DiVA Logout");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }
            await client.LoginAsync(TokenType.Bot, Configuration["tokens:discord"]);
            await client.StartAsync();

            client.Ready += () =>
            {
                Log.Neutral($"{client.CurrentUser.Username}#{client.CurrentUser.Discriminator} is connected !\n\n" +
                    $"__[ CONNECTED TO ]__\n", "DiVA Login");
                foreach (var guild in client.Guilds)
                {
                    Log.Neutral(
                        $"\t_______________\n" +
                        $"\t{guild.Name} \n" +
                        $"\tOwned by {guild.Owner.Nickname}#{guild.Owner.Discriminator}\n" +
                        $"\t{guild.MemberCount} members", "DiVA Login");
                }
                Log.Neutral("\t_______________", "DiVA Login");
                //ConsoleColor[] colors = (ConsoleColor[])ConsoleColor.GetValues(typeof(ConsoleColor));
                //foreach (var color in colors)
                //{
                //    Console.ForegroundColor = color;
                //    Console.WriteLine(" The foreground color is {0}.", color);
                //}
                Console.Title = $"{Assembly.GetExecutingAssembly().GetName().Name} v{GetVersion()}";
                SetDefaultStatus();
                return Task.CompletedTask;
            };
            await Task.Delay(-1);
        }

        /// <summary>
        /// Install commands for the bot
        /// </summary>
        /// <returns></returns>
        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            client.UserLeft += UserLeftGuildHandler;
            client.UserJoined += UserJoinedGuildHandler;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        /// <summary>
        /// Get services set up
        /// </summary>
        /// <param name="serviceCollection"></param>
        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(new YouTubeDownloadService());
            serviceCollection.AddSingleton(new AudioPlaybackService());
            serviceCollection.AddSingleton(new AudioService());
        }

        /// <summary>
        /// Handle every command
        /// </summary>
        /// <param name="messageParam"></param>
        /// <returns></returns>
        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message)) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            if (!(message.HasStringPrefix(Configuration["prefix"], ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        /// <summary>
        /// Handling user joining guild
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private async Task UserJoinedGuildHandler(SocketGuildUser param)
        {
            Random _rnd = new Random();
            var channel = client.GetChannel(param.Guild.DefaultChannel.Id) as SocketTextChannel;
            await CommandHelper.SayHelloAsync(channel, client, param as IUser, _rnd);
        }

        /// <summary>
        /// Handling user leaving guild
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private async Task UserLeftGuildHandler(SocketGuildUser param)
        {
            Random _rnd = new Random();
            var channel = client.GetChannel(param.Guild.DefaultChannel.Id) as SocketTextChannel;
            await channel.SendMessageAsync($"{param.Mention} left us... Say bye ! ");
        }

        /// <summary>
        /// Shutdown procedure
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            try
            { client.LogoutAsync(); }
            catch { }
            finally
            { client.Dispose(); }
            Log.Warning("Shutting Down...", "DiVA Logout");
            Environment.Exit(0);
        }

        /// <summary>
        /// Log system
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Task LogMessage(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Debug:
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
            //Console.WriteLine($"[{message.Severity} {message.Source}][{DateTime.Now.ToString()}] : {message.Message}");
            Log.Message(message.Severity, message.Message, message.Source);
            //Console.ResetColor();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Configuration generator
        /// </summary>
        /// <returns></returns>
        public static bool TryGenerateConfiguration()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(filePath)) return false;
            object config = new DiVAConfiguration();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
            return true;
        }

        private void EditToken()
        {
            string url = "https://discordapp.com/developers/applications/538306821333712916/bots";
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

            Log.Neutral("Please enter the bot's token below.\n", "DiVA Login");
            string answer = Console.ReadLine();
            Configuration["tokens:discord"] = answer;
            EditKey();
        }
        private void EditKey()
        {
            string url = "https://console.developers.google.com/apis/credentials?project=diva-discord";
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

            Log.Neutral("Please enter the DiVA API Key below.\n", "DiVA Login");
            string answer = Console.ReadLine();
            Configuration["tokens:youtube"] = answer;
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            object config = new DiVAConfiguration(Configuration["prefix"], new Tokens(Configuration["tokens:discord"], Configuration["tokens:youtube"]));
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.Delete(filePath);
            File.WriteAllText(filePath, json);
        }

        private void CacheCleanup()
        {
            try
            {
                string cachePath = Path.Combine(AppContext.BaseDirectory, "Songs");
                DirectoryInfo d = new DirectoryInfo(cachePath);
                Log.Information($"Searching for files in folder \n{cachePath}", "DiVA Login");
                if (d.GetFiles().Length > 0)
                {
                    Log.Information($"Found {d.GetFiles().Length} files : ", "DiVA Login");
                    foreach (FileInfo file in d.GetFiles())
                    { Log.Information($"\tFile {file.Name}", "DiVA Login"); }

                    Log.Neutral("Do you want to erase all cache ? (Y/n)\n", "DiVA Login");
                    var answer = Console.ReadKey();
                    Log.Neutral("", "DiVA Login");
                    if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                    {
                        Log.Information("Deleting files... Please wait...", "DiVA Login");
                        foreach (FileInfo file in d.GetFiles())
                        { file.Delete(); }
                        Log.Information("Proceeding with launch.", "DiVA Login");
                    }
                    else
                    { Log.Information("Keeping cache files. Proceeding with launch...", "DiVA Login"); }
                }
                else
                { Log.Information("No file found. Proceeding with launch...", "DiVA Login"); }
            }
            catch (DirectoryNotFoundException)
            { Log.Information($"No cache folder detected... Proceeding with launch...", "DiVA Login"); }
            catch (Exception e)
            { Log.Error($"An error occured. Please check and remove the cache manually : \n{e.Message}", "DiVA Login"); }
        }

        /// <summary>
        /// Setting current status
        /// </summary>
        public void SetDefaultStatus()
        { client.SetGameAsync($"Discord Virtual Assistant or DiVA v{GetVersion()}"); }

        /// <summary>
        /// Get current version
        /// </summary>
        /// <returns></returns>
        public static string GetVersion()
        {
            string rev = "b";
            if (DEV_MODE)
                rev = "a";
            return $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}{rev}";
        }

    }
}
