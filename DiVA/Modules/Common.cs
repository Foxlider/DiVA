using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiVA.Helpers;
using DiVA.Services;
using DiVA.Services.YouTube;
using Microsoft.Extensions.Configuration;

namespace DiVA.Modules
{
    // Create a module with no prefix
    /// <summary>
    /// Common commands module
    /// </summary>
    [Name("Common")]
    [Summary("Common commands for DiVA")]
    public class Common : ModuleBase
    {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;
        private readonly DiscordSocketClient _client;
        Random __rnd = new Random();

        /// <summary>
        /// Override ReplyAsync
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isTTS"></param>
        /// <param name="embed"></param>
        /// <param name="options"></param>
        /// <param name="deleteafter"></param>
        /// <returns></returns>
        protected async Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, TimeSpan? deleteafter = null)
        {
            var msg = await base.ReplyAsync(message, isTTS, embed, options);
            if (deleteafter == null) return msg;
            var t = new Thread(async () =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(deleteafter.Value.TotalMilliseconds));
                await msg.DeleteAsync();
            })
            { IsBackground = true };
            t.Start();
            return msg;
        }

        /// <summary>
        /// Common Commands module builder
        /// </summary>
        /// <param name="service"></param>
        public Common(CommandService service)
        {
            _client = DiVA.Client;
            _service = service;
            _config = DiVA.Configuration;
        }

        #region COMMANDS

        #region echo
        /// <summary>
        /// SAY - Echos a message
        /// </summary>
        /// <param name="echo"></param>
        /// <returns></returns>
        [Command("say"), Summary("Echos a message.")]
        [Alias("echo")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder, Summary("The text to echo")] string echo)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            await ReplyAsync(echo);
        }

        #endregion echo

        #region say to

        [Command("sayto"), Summary("Echos a message to a server.")]
        [Alias("echoto")]
        [RequireContext(ContextType.DM)]
        public async Task SayTo(ulong discordId, [Remainder, Summary("The text to echo")] string echo)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            try
            {
                if (await Context.Client.GetChannelAsync(discordId) is IMessageChannel channel)
                { await channel.SendMessageAsync(echo); }
                else
                {
                    var user = await Context.Client.GetUserAsync(discordId);
                    await user.SendMessageAsync(echo);
                }
            }
            catch
            { await ReplyAsync("Could not find this channel"); }
        }

        #endregion say to

        #region hello
        /// <summary>
        /// HELLO - Says hello
        /// </summary>
        /// <returns></returns>
        [Command("hello"), Summary("Says hello")]
        [Alias("hi")]
        public async Task Hello()
        {
            await CommandHelper.SayHelloAsync(Context.Channel, Context.Client, Context.User, __rnd);
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
        }
        #endregion hello

        #region userinfo
        /// <summary>
        /// USERINFO - Returns the information of a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("userinfo"), Summary("Displays information about a user")]
        [Alias("user", "whois")]
        public async Task UserInfo([Summary("The (optional) user to get info for")] IUser user = null)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            var userInfo = user ?? Context.Client.CurrentUser;
            var builder = new EmbedBuilder();
            EmbedFieldBuilder field;

            builder.WithTitle($"User Informations");
            builder.WithDescription($"Informations of {userInfo.Mention} - {userInfo.Username}#{userInfo.Discriminator}");

            if (userInfo.IsBot)
            {
                field = new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "BOT : ",
                    Value = "User is a Bot"
                };
                builder.AddField(field);
            }

            field = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "User created at ",
                Value = userInfo.CreatedAt.DateTime.ToString("g", CultureInfo.CreateSpecificCulture("fr-FR"))
            };
            builder.AddField(field);


            var guildUser = (userInfo as IGuildUser);
            if (guildUser != null)
            {
                if (guildUser.JoinedAt != null) field = new EmbedFieldBuilder { IsInline = true, Name = "User joined at ", Value = guildUser.JoinedAt.Value.DateTime.ToString("g", CultureInfo.CreateSpecificCulture("fr-FR")) };
                builder.AddField(field);

                var userPerms = guildUser.RoleIds;
                foreach (var role in userPerms)
                {
                    if (Context.Guild.GetRole(role).Name != "@everyone") //You don't want to mention the world
                    {
                        field = new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Role :",
                            Value = Context.Guild.GetRole(role).Name
                        };
                        builder.AddField(field);
                    }
                }

                if (guildUser.VoiceChannel != null)
                {
                    field = new EmbedFieldBuilder
                    {
                        IsInline = false,
                        Name = "Connected to : ",
                        Value = guildUser.VoiceChannel.ToString()
                    };
                    builder.AddField(field);
                }
            }

            builder.WithThumbnailUrl(userInfo.GetAvatarUrl());
            builder.WithColor(Color.Blue);

            var embed = builder.Build();
            await Context.Channel.SendMessageAsync("", false, embed);
        }

        #endregion userinfo

        #region Help
        /// <summary>
        /// HELP - Displays some help
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [Command("help")]
        [Alias("h")]
        [Summary("Prints the help of available commands")]
        public async Task HelpAsync(string command = null)
        {
            if (command == null)    //______________________________________________        HELP WITH NO COMMAND PROVIDED
            {
                try
                { await Context.Message.DeleteAsync(); }
                catch { /* ignored */ }
                string prefix = _config["prefix"];
                var builder = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Title = "Help",
                    Description = "These are the commands you can use"
                };

                foreach (var module in _service.Modules)
                {
                    string description = "```";
                    foreach (var cmd in module.Commands)
                    {
                        var result = await cmd.CheckPreconditionsAsync(Context);
                        if (result.IsSuccess)
                        {
                            string alias = cmd.Aliases.First();
                            description += $"{prefix}{alias.PadRight(10)}\t{cmd.Summary}\n";
                        }
                    }
                    description += "```";


                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        builder.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = description;
                            x.IsInline = false;
                        });
                    }
                }
                await ReplyAsync("", false, builder.Build());
            }
            else //_________________________________________________________________       HELP WITH COMMAND PROVIDED
            {
                var result = _service.Search(Context, command);
                if (!result.IsSuccess)
                {
                    await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                    return;
                }
                var builder = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Description = $"Here are some commands like **{command}**"
                };

                foreach (var match in result.Commands)
                {
                    var cmd = match.Command;
                    builder.AddField(x =>
                    {
                        x.Name = $"({string.Join("|", cmd.Aliases)})";
                        x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                                  $"Summary: {cmd.Summary}";
                        x.IsInline = false;
                    });
                }
                await ReplyAsync("", false, builder.Build());
            }
        }

        #endregion Help

        #region version
        /// <summary>
        /// VERSION - Command Version
        /// </summary>
        /// <returns></returns>
        [Command("version"), Summary("Check the bot's version")]
        [Alias("v")]
        public async Task Version()
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            var OSdesc = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

            var builder = new EmbedBuilder();

            builder.WithTitle($"Bot Informations");
            builder.WithDescription($"{_client.CurrentUser.Mention} - {_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}");

            EmbedFieldBuilder field = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Bot : ",
                Value = Assembly.GetExecutingAssembly().GetName().Name
            };
            builder.AddField(field);

            field = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Version : ",
                Value = 'v' + DiVA.GetVersion()
            };
            builder.AddField(field);

            field = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Running on : ",
                Value = $"{OSdesc} ({arch})"
            };
            builder.AddField(field);

            builder.WithThumbnailUrl(_client.CurrentUser.GetAvatarUrl());
            builder.WithColor(Color.Blue);

            var embed = builder.Build();
            await Context.Channel.SendMessageAsync("", false, embed);
        }

        #endregion version

        #region choose
        /// <summary>
        /// CHOOSE - Command choose
        /// </summary>
        /// <param name="cString">Multiple strings to choose from</param>
        /// <returns></returns>
        [Command("choose"), Summary("If you want a robot to choose for you")]
        public async Task Choose([Remainder]string cString)
        {
            string[] choices = cString.Split().ToArray();
            Random _rnd = new Random();
            string answer = "";
            string chosenOne = choices[_rnd.Next(choices.Length)];
            if (choices[0].StartsWith("<@") && choices[0].EndsWith(">"))
            { answer = chosenOne; }
            else
            {
                foreach (string word in choices)
                {
                    if (word == chosenOne)
                    { answer += $" **{word}**"; }
                    else
                    { answer += $" {word}"; }
                }
            }
            await ReplyAsync(answer);
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored*/ }
        }

        #endregion choose

        #region roll
        /// <summary>
        /// ROLL - Command roll
        /// </summary>
        /// <param name="dice">string of the dices (ex. 1d10)</param>
        /// <returns></returns>
        [Command("roll"), Summary("Rolls a dice in NdN format")]
        [Alias("r")]
        public async Task Roll(string dice)
        {
            await ReplyAsync(CommandHelper.DiceRoll(dice, Context.User.Mention));
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
        }
        #endregion roll

        #region pvroll
        /// <summary>
        /// PVROLL - Command pvroll
        /// </summary>
        /// <param name="dice">string of the dices (ex. 1d10)</param>
        /// <returns></returns>
        [Command("pvroll"), Summary("Secretly rolls a dice")]
        [Alias("pvr")]
        public async Task PrivateRoll(string dice)
        {
            await Context.User.SendMessageAsync(CommandHelper.DiceRoll(dice, Context.User.Mention));
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
        }

        #endregion roll

        #region status
        /// <summary>
        /// STATUS - Command status
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
        [Command("status")]
        [Summary("Change the status of the bot")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Status(string stat = "")
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            if (stat == null || stat == "")
            { await DiVA.SetDefaultStatus(_client); }
            else
            { await _client.SetGameAsync(stat, type: ActivityType.Watching); }
        }

        #endregion status

        #region cmdtest
        ///// <summary>
        ///// Tests the console
        ///// </summary>
        ///// <returns></returns>
        //[Command("ctest")]
        //[Summary("TestConsole")]
        //public async Task ConsoleTest()
        //{
        //    try
        //    { await Context.Message.DeleteAsync(); }
        //    catch { /* ignored */ } 
        //    Logger.Log(Logger.Neutral, "Neutral", "Commands ConsoleTest");
        //    Logger.Log(Logger.Info, "Information", "Commands ConsoleTest");
        //    Logger.Log(Logger.Verbose, "Verbose", "Commands ConsoleTest");
        //    Logger.Log(Logger.Debug, "Debug", "Commands ConsoleTest");
        //    Logger.Log(Logger.Warning, "Warning", "Commands ConsoleTest");
        //    Logger.Log(Logger.Error, "Error", "Commands ConsoleTest");
        //    Logger.Log(Logger.Critical, "Critical", "Commands ConsoleTest");
        //}
        #endregion cmdtest

        #endregion COMMANDS
    }

    [Name("Interactive")]
    [Summary("Interactive commands for DiVA")]
    public class Interactive : ModuleBase
    {
        #region INTERACTIVE COMMANDS

        #region ping
        [Command("ping")]
        [Summary("Ping command for @Darlon")]
        [Alias("pong")] // alternative names for the command
        public async Task Ping() // this command takes no arguments
        {
            await Context.Channel.TriggerTypingAsync();
            //Hi @Darlon !
            await ReplyAsync($"Pong!");
        }
        #endregion

        #region poll
        [Command("poll", RunMode = RunMode.Async), Summary("Run a poll with reactions.")]
        public async Task Poll([Summary("Duration (10s)")] TimeSpan duration, [Summary("Options (:ok_hand: :thumbsup:)")] params string[] options)
        {
            var poll_options = options.Select(xe => xe.ToString());
            var embed = new EmbedBuilder
            {
                Title = "Poll time!",
                Description = string.Join(" ", poll_options)
            };
            var msg = await ReplyAsync(embed: embed.Build());
            List<Emote> emoteList = new List<Emote>();
            List<Emoji> emojiList = new List<Emoji>();

            foreach (var opt in options)
            {
                if (Emote.TryParse(opt, out var emote))
                {
                    emoteList.Add(emote);
                    await msg.AddReactionAsync(emote);
                }
                else
                {
                    var emoji = new Emoji(opt);
                    emojiList.Add(emoji);
                    await msg.AddReactionAsync(emoji);
                }
            }
            await Task.Delay(duration).ConfigureAwait(false);

            var finalMessage = await Context.Channel.GetMessageAsync(msg.Id) as IUserMessage;

            var resEmote = finalMessage.Reactions.Where(xkvp => emoteList.Contains(xkvp.Key))
                .Select(xkvp => $"{xkvp.Key}: {xkvp.Value.ReactionCount}");

            var resEmoji = finalMessage.Reactions.Where(xkvp => emojiList.Contains(xkvp.Key))
                .Select(xkvp => $"{xkvp.Key}: {xkvp.Value.ReactionCount}");

            var results = resEmoji.Union(resEmote).ToList();

            // and finally post the results
            await ReplyAsync($"Results : \n{string.Join("\n", results)}");
        }
        #endregion

        #endregion
    }

    /// <summary>
    /// Audio commands module
    /// </summary>
    [Name("Music")]
    [Summary("Audio commands for DiVA")]
    public class Audio : ModuleBase
    {
        /// <summary>
        /// Downloader from YouTube
        /// </summary>
        public YouTubeDownloadService YoutubeDownloadService { get; set; }

        /// <summary>
        /// Music handler
        /// </summary>
        public AudioService SongService { get; set; }

        /// <summary>
        /// Override ReplyAsync
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isTTS"></param>
        /// <param name="embed"></param>
        /// <param name="options"></param>
        /// <param name="deleteafter"></param>
        /// <returns></returns>
        protected async Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, int? deleteafter = null)
        {
            var msg = await base.ReplyAsync(message, isTTS, embed, options);
            if (deleteafter != null)
            {
                Thread.Sleep((int)TimeSpan.FromSeconds((int)deleteafter).TotalMilliseconds);
                await msg.DeleteAsync();
            }
            return msg;
        }

        #region AUDIO COMMANDS

        #region play
        /// <summary>
        /// PLAY - Function Play
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [Alias("play", "request", "songrequest")]
        [Command("sq", RunMode = RunMode.Async)]
        [Summary("Requests a song to be played")]
        public async Task Request([Remainder, Summary("URL of the video to play")] string url)
        {
            IDisposable typer = null;
            try
            {
                try { await Context.Message.DeleteAsync(); }
                catch
                {
                    /* ignored */
                }
                var _voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if (_voiceChannel == null)
                {
                    Logger.Log(Logger.Warning, "Error joining Voice Channel!", "Audio Request");
                    await ReplyAsync($"I can't connect to your Voice Channel.", deleteafter: 10);
                    return;
                }
                typer = Context.Channel.EnterTypingState();
                DownloadedVideo video = await YouTubeDownloadService.GetVideoData(url);
                if (video == null)
                {
                    await ReplyAsync($"{Context.User.Mention} unable to queue song, make sure its is a valid supported URL or contact a server admin.");
                    return;
                }

                Logger.Log(Logger.Verbose, $"Got video informations from YouTube API\n" +
                                           $"{url} :: {video.Title} ({video.Uri})", "Command Queue");

                if (!File.Exists(Path.Combine("Songs", $"{video.DisplayID}.mp3")))
                {
                    Logger.Log(Logger.Verbose, $"Audio not in cache folder. Starting download...", "Command Queue");
                    var downloadAnnouncement = await ReplyAsync($"{Context.User.Mention} attempting to download {url}");
                    await Context.Channel.TriggerTypingAsync();
                    video = await YoutubeDownloadService.DownloadVideo(video);
                    try
                    {
                        await downloadAnnouncement.DeleteAsync();
                        typer.Dispose();
                    }
                    catch
                    { /* ignored */ }
                    if (video == null)
                        throw new ArgumentNullException($"The video could not be downloaded. ");
                }
                Logger.Log(Logger.Verbose, $"Starting audio playback.", "Command Queue");
                video.Requester = Context.User.Mention;

                await ReplyAsync($"{Context.User.Mention} queued **{video?.Title}** | `{TimeSpan.FromSeconds(video.Duration)}`", deleteafter: 20);
                SongService.Queue(video, _voiceChannel, Context.Message.Channel);
            }
            catch (Exception e)
            {
                Logger.Log(Logger.Warning, $"Error while processing song requet: {e}", "Audio Request");
                try
                { await ReplyAsync($"Error while processing song requet: {e.Message}", deleteafter: 20); }
                catch
                { /* ignored */}
            }
            finally { typer?.Dispose(); }
        }
        #endregion

        #region test
        /// <summary>
        /// TEST - Sound test (watch your ears)
        /// </summary>
        /// <returns></returns>
        [Alias("test")]
        [Command("soundtest", RunMode = RunMode.Async)]
        [Summary("Performs a sound test")]
        public async Task SoundTest()
        { await Request("https://www.youtube.com/watch?v=i1GOn7EIbLg"); }
        #endregion

        #region stream
        /// <summary>
        /// STREAM - Stream Player
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [Command("stream", RunMode = RunMode.Async)]
        [Summary("Streams a livestream URL")]
        public async Task Stream(string url)
        {
            try
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    await ReplyAsync($"{Context.User.Mention} please provide a valid URL");
                    return;
                }

                var downloadAnnouncement = await ReplyAsync($"{Context.User.Mention} attempting to open {url}");
                var stream = await YouTubeDownloadService.GetLivestreamData(url);
                try
                { await downloadAnnouncement.DeleteAsync(); }
                catch { /* ignored */ }

                if (stream == null)
                {
                    await ReplyAsync($"{Context.User.Mention} unable to open live stream, make sure its is a valid supported URL or contact a server admin.");
                    return;
                }

                stream.Requester = Context.User.Mention;
                stream.Url = url;

                Logger.Log(Logger.Info, $"Attempting to stream {stream}", "Audio Stream");

                await ReplyAsync($"{Context.User.Mention} queued **{stream.Title}** | `{stream.DurationString}`");
                var _voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if (_voiceChannel == null)
                {
                    Logger.Log(Logger.Warning, "Error joining Voice Channel!", "Audio Stream");
                    await ReplyAsync($"I can't connect to your Voice Channel.");
                }
                else
                { SongService.Queue(stream, _voiceChannel, Context.Message.Channel); }
            }
            catch (Exception e)
            { Logger.Log(Logger.Warning, $"Error while processing song requet: {e}", "Audio Stream"); }
        }
        #endregion

        #region audiosay
        /// <summary>
        /// AUDIOSAY - Say TTS things in vocal
        /// </summary>
        /// <param name="said"></param>
        /// <returns></returns>
        [Command("audiosay", RunMode = RunMode.Async)]
        [Alias("asay")]
        [Summary("Says something")]
        public async Task AudioSay([Remainder, Summary("What should DiVA say")] string said)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }

            var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                Logger.Log(Logger.Warning, "Error joining Voice Channel!", "Audio Stream");
                await ReplyAsync($"I can't connect to your Voice Channel.");
            }
            else { SongService.Say(said, voiceChannel); }
        }

        /// <summary>
        /// AUDIOSAYTO - Say TTS things to a specific channel
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="said"></param>
        /// <returns></returns>
        [Command("audiosayto", RunMode = RunMode.Async)]
        [Alias("asayto")]
        [Summary("Says something")]
        public async Task AudioSayTo(ulong channelId, [Remainder, Summary("What should DiVA say")] string said)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            var voiceChannel = await Context.Client.GetChannelAsync(channelId);
            if (!(await Context.Client.GetChannelAsync(channelId) is IVoiceChannel))
            {
                Logger.Log(Logger.Warning, "Error joining Voice Channel!", "Audio Stream");
                await ReplyAsync($"I can't connect to your Voice Channel.");
            }
            else { SongService.Say(said, voiceChannel as IVoiceChannel); }
        }

        /// <summary>
        /// AUDIOSAY - Say TTS things in vocal
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="said"></param>
        /// <returns></returns>
        [Command("audiolsay", RunMode = RunMode.Async)]
        [Alias("alsay")]
        [Summary("Says something in the given language")]
        public async Task AudioSayL([Summary("Language selected ('en-US'/'fr-FR'")] string culture, [Remainder, Summary("What should DiVA say")] string said)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }

            var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
            if (voiceChannel == null)
            {
                Logger.Log(Logger.Warning, "Error joining Voice Channel!", "Audio Stream");
                await ReplyAsync($"I can't connect to your Voice Channel.");
            }
            else { SongService.Say(said, voiceChannel, culture); }
        }

        /// <summary>
        /// AUDIOSAYTO - Say TTS things to a specific channel
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="culture"></param>
        /// <param name="said"></param>
        /// <returns></returns>
        [Command("audiolsayto", RunMode = RunMode.Async)]
        [Alias("alsayto")]
        [Summary("Says something in the given language")]
        public async Task AudioSayToL(ulong channelId, [Summary("Language selected ('en-US'/'fr-FR'")]string culture, [Remainder, Summary("What should DiVA say")] string said)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            var voiceChannel = await Context.Client.GetChannelAsync(channelId);
            if (!(await Context.Client.GetChannelAsync(channelId) is IVoiceChannel))
            {
                Logger.Log(Logger.Warning, "Error joining Voice Channel!", "Audio Stream");
                await ReplyAsync($"I can't connect to your Voice Channel.");
            }
            else
            { SongService.Say(said, voiceChannel as IVoiceChannel, culture); }
        }
        #endregion

        #region disconnect
        [Command("quit", RunMode = RunMode.Async)]
        [Summary("Quit a channel")]
        public async Task DisconnectFrom(ulong channelId = 0)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            if (channelId == 0)
            {
                var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if (voiceChannel == null)
                {
                    Logger.Log(Logger.Warning, "Error getting Voice Channel!", "Audio Stream");
                    await ReplyAsync($"I can't get your Voice Channel.");
                }
                await SongService.Quit(voiceChannel.Guild);
            }
            else
            {
                var voiceChannel = await Context.Client.GetChannelAsync(channelId);
                if (!(await Context.Client.GetChannelAsync(channelId) is IVoiceChannel))
                {
                    Logger.Log(Logger.Warning, "Error joining Voice Channel!", "Audio Stream");
                    await ReplyAsync($"I can't connect to your Voice Channel.");
                }
                else
                {
                    var temp = voiceChannel as IVoiceChannel;
                    await SongService.Quit(temp.Guild);
                }
            }
        }

        #endregion

        #region clear
        /// <summary>
        /// CLEAR - Command Clear
        /// </summary>
        /// <returns></returns>
        [Command("clear")]
        [Summary("Clears all songs in queue")]
        public async Task ClearQueue()
        {
            SongService.Clear(Context.Guild);
            await ReplyAsync("Queue cleared");
        }
        #endregion

        #region stop
        /// <summary>
        /// STOP - Command stop
        /// </summary>
        /// <returns></returns>
        [Command("stop")]
        [Summary("Stops the playback and disconnect")]
        public async Task Stop()
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            SongService.Clear(Context.Guild);
            await SongService.Quit(Context.Guild);
        }
        #endregion

        #region skip
        /// <summary>
        /// SKIP - Command Skip
        /// </summary>
        /// <returns></returns>
        [Alias("next", "nextsong")]
        [Command("skip")]
        [Summary("Skips current song")]
        public async Task SkipSong()
        {
            SongService.Next(Context.Guild.Id);
            await ReplyAsync("Skipped song");
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
        }
        #endregion

        #region volume
        /// <summary>
        /// VOLUME - Command volume
        /// </summary>
        /// <param name="vol"></param>
        /// <returns></returns>
        [Command("volume")]
        [Alias("vol")]
        [Summary("Changes the volume of a song")]
        public async Task Volume(int? vol = null)
        {
            if (vol == null)
            {
                await ReplyAsync($"Current volume : {SongService.GetVolume(Context.Guild.Id)}", deleteafter: 5);
                return;
            }

            if (vol < 0 || vol > 100)
            {
                await ReplyAsync("The volume needs to be between 0 and 100", deleteafter: 5);
                return;
            }

            var volume = SongService.SetVolume(Context.Guild.Id, vol);
            await ReplyAsync($"Volume set to {volume}%", deleteafter: 5);
            try
            { await Context.Message.DeleteAsync(); }
            catch { /*ignored*/ }
        }
        #endregion

        #region queue
        /// <summary>
        /// QUEUE - Command queue
        /// </summary>
        /// <returns></returns>
        [Alias("songlist")]
        [Command("queue")]
        [Summary("Lists current songs")]
        public async Task SongList()
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            List<IPlayable> songlist = SongService.SongList(Context.Guild);
            if (songlist.Count == 0)
            { await ReplyAsync($"{Context.User.Mention} current queue is empty"); }
            else
            {
                string          msg        = "";
                var             nowPlaying = songlist.FirstOrDefault();
                List<IPlayable> qList      = songlist.TakeLast(songlist.Count - 1).ToList();
                msg += $"** Now Playing : **\n  - *{nowPlaying.Title}* (`{nowPlaying.DurationString}`)\n\n";
                if (qList.Count > 0)
                {
                    msg        += "** Songs in queue : **"; 
                    foreach (var song in qList)
                    { msg += $"\n  - *{song.Title}* (`{song.DurationString}`)"; }
                }
                await ReplyAsync(msg);
            }
        }
        #endregion

        #region nowplaying
        /// <summary>
        /// NOWPLAYING - Command nowPlaying
        /// </summary>
        /// <returns></returns>
        [Command("nowplaying")]
        [Alias("np", "currentsong", "songname", "song")]
        [Summary("Prints current playing song")]
        public async Task NowPlaying()
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
            List<IPlayable> songlist = SongService.SongList(Context.Guild);
            if (songlist.Count == 0)
            { await ReplyAsync($"{Context.User.Mention} current queue is empty"); }
            else
            { await ReplyAsync($"{Context.User.Mention} now playing `{songlist.FirstOrDefault().Title}` requested by {songlist.FirstOrDefault().Requester}"); }
        }
        #endregion

        #endregion
    }

    /// <summary>
    /// Cache handler group
    /// </summary>
    [Group("sudo")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class Admin : ModuleBase
    {
        #region CACHE COMMANDS

        #region cache list
        /// <summary>
        /// CACHE LIST - List the cached music files
        /// </summary>
        /// <returns></returns>
        [Command("clist"), Summary("Displays the list of cached videos")]
        public async Task DisplayList()
        {
            try
            {
                string cachePath = Path.Combine(AppContext.BaseDirectory, "Songs");
                if (File.Exists(Path.Combine(cachePath, "songlist.cache")))
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle($"**Cached song list**");
                    builder.WithDescription($"List of songs downloaded\n");
                    foreach (var line in File.ReadAllLines(Path.Combine(cachePath, "songlist.cache")))
                    {
                        var filename = line.Split(",").Last().Trim(';');
                        var title = line.Substring(0, line.Length - (filename.Length + 2));
                        var field = new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = filename,
                            Value = title
                        };
                        builder.AddField(field);
                    }

                    builder.WithColor(Color.Blue);
                    var embed = builder.Build();
                    if (File.ReadAllLines(Path.Combine(cachePath, "songlist.cache")).Length <= 5)
                    { await Context.Channel.SendMessageAsync("", false, embed); }
                    else
                    { await Context.User.SendMessageAsync("", false, embed); } //Send to user to avoid flood
                }
                else
                { await Context.Channel.SendMessageAsync("No music have been downloaded yet"); }
            }
            finally
            {
                try
                { await Context.Message.DeleteAsync(); }
                catch { /* ignored */ }
            }
        }
        #endregion

        #region cache delete
        /// <summary>
        /// CACHE DELETE - Delete cached files
        /// </summary>
        /// <returns></returns>
        [Command("cdel"), Summary("Delete cache files")]
        public async Task Delete(string input = null)
        {
            string cachePath = Path.Combine(AppContext.BaseDirectory, "Songs");
            DirectoryInfo d = new DirectoryInfo(cachePath);
            if (d.GetFiles().Length > 0)
            {
                if (input == null)
                {
                    foreach (FileInfo file in d.GetFiles())
                    { file.Delete(); }
                }
                else
                {
                    var hasDeleted = false;
                    foreach (FileInfo file in d.GetFiles())
                    {
                        if (file.Name == input)
                        {
                            file.Delete();
                            await Context.User.SendMessageAsync($"File {input}.mp3 deleted.");
                            hasDeleted = true;
                            break;
                        }
                    }
                    if (!hasDeleted)
                    { await Context.User.SendMessageAsync($"No file have been found under the name {input}"); }
                }
            }
            try
            { await Context.Message.DeleteAsync(); }
            catch { /* ignored */ }
        }
        #endregion

        #endregion

        #region SUDO COMMANDS

        #region loglvl
        [Command("loglvl"), Summary("Delete cache files")]
        public async Task LogLevel(ushort log = 10)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /*ignored*/ }
            if (log <= 5)
            {
                DiVA.LogLvl = log;
                await Context.User.SendMessageAsync($"Setting Log Level to {log}");
            }
            else
            { await Context.User.SendMessageAsync($"Please enter a value between 0 (Critical Messages) and 5(Debug messages)"); }
        }
        #endregion

        #endregion
    }

}
