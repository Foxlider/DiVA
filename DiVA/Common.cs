﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiVA.Helpers;
using DiVA.Services;
using DiVA.Services.YouTube;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
        /// Common Commands module builder
        /// </summary>
        /// <param name="service"></param>
        public Common(CommandService service)
        {
            _client = DiVA.client;
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
            finally {}
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
            catch {}
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
            catch {}
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
            catch {}
            var userInfo = user ?? Context.Client.CurrentUser;
            var builder = new EmbedBuilder();
            EmbedFieldBuilder field = new EmbedFieldBuilder
            {
                IsInline = false
            };

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
                field = new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "User joined at ",
                    Value = guildUser.JoinedAt.Value.DateTime.ToString("g", CultureInfo.CreateSpecificCulture("fr-FR"))
                };
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
                catch {}
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
            catch {}
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
            catch { }
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
            
            try
            {
                var result = dice
                    .Split('d')
                    .Select(input =>
                    {
                        int? output = null;
                        if (int.TryParse(input, out var parsed))
                        {
                            output = parsed;
                        }
                        return output;
                    })
                    .Where(x => x != null)
                    .Select(x => x.Value)
                    .ToArray();
                string msg = $"{Context.User.Mention} rolled {result[0]}d{result[1]}";
                var range = Enumerable.Range(0, result[0]);
                int[] dices = new int[result[0]];
                Random _rnd = new Random();
                foreach (var r in range)
                { dices[r] = _rnd.Next(1, result[1]); }
                msg += "\n [ **";
                msg += string.Join("** | **", dices);
                msg += "** ]";
                await ReplyAsync(msg);
            }
            catch
            { await ReplyAsync("C'est con mais j'ai pas compris..."); }
            finally
            {
                try
                { await Context.Message.DeleteAsync(); }
                catch {}
            }
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
            try
            {
                var result = dice
                    .Split('d')
                    .Select(input =>
                    {
                        int? output = null;
                        if (int.TryParse(input, out var parsed))
                        {
                            output = parsed;
                        }
                        return output;
                    })
                    .Where(x => x != null)
                    .Select(x => x.Value)
                    .ToArray();
                string msg = $"{Context.User.Mention} rolled {result[0]}d{result[1]}";
                var range = Enumerable.Range(0, result[0]);
                int[] dices = new int[result[0]];
                Random _rnd = new Random();
                foreach (var r in range)
                { dices[r] = _rnd.Next(1, result[1]); }
                msg += "\n [ **";
                msg += string.Join("** | **", dices);
                msg += "** ]";
                await Context.User.SendMessageAsync(msg);
            }
            catch
            { await Context.User.SendMessageAsync("C'est con mais j'ai pas compris..."); }
            finally
            {
                try
                { await Context.Message.DeleteAsync(); }
                catch {}
            }
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
        public async Task Status(string stat = "")
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch {}
            if ( stat == null ||stat == "")
            { await _client.SetGameAsync($"Ready to meet {Assembly.GetExecutingAssembly().GetName().Name} v{DiVA.GetVersion()} ?"); }
            else
            { await _client.SetGameAsync(stat); }
        }

        #endregion status

        #region cmdtest
        /// <summary>
        /// Tests the console
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
        [Command("consoletest")]
        [Summary("TestConsole")]
        public async Task ConsoleTest(string stat = "")
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch {}
            Log.Neutral("Neutral", "Commands ConsoleTest");
            Log.Information("Information", "Commands ConsoleTest");
            Log.Verbose("Verbose", "Commands ConsoleTest");
            Log.Debug("Debug", "Commands ConsoleTest");
            Log.Warning("Warning", "Commands ConsoleTest");
            Log.Error("Error", "Commands ConsoleTest");
            Log.Critical("Critical", "Commands ConsoleTest");
        }
        #endregion cmdtest

        #endregion COMMANDS
    }

    [Name("Interactive")]
    [Summary("Interactive commands for DiVA")]
    public class Interactive : ModuleBase
    {
        [Command("ping")]
        [Summary("Ping command for @Darlon")]
        [Alias("pong")] // alternative names for the command
        public async Task Ping() // this command takes no arguments
        {
            await Context.Channel.TriggerTypingAsync();
            //Hi @Darlon !
            await ReplyAsync($"Pong!");
        }

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
            try
            {
                await Context.Channel.TriggerTypingAsync();
                DownloadedVideo video = await YouTubeDownloadService.GetVideoData(url);
                if (video == null)
                {
                    await ReplyAsync($"{Context.User.Mention} unable to queue song, make sure its is a valid supported URL or contact a server admin.");
                    return;
                }
                var _voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if (_voiceChannel == null)
                {
                    Log.Warning("Error joining Voice Channel!", "Audio Request");
                    await ReplyAsync($"I can't connect to your Voice Channel.");
                }

                Log.Verbose($"Got video informations from YouTube API\n" +
                          $"{url} :: {video.Title} ({video.Uri})", "Command Queue");
                try
                { await Context.Message.DeleteAsync(); }
                catch {}
                if (!File.Exists(Path.Combine("Songs", $"{video.DisplayID}.mp3")))
                {
                    Log.Verbose($"Audio not in cache folder. Starting download...", "Command Queue");
                    var downloadAnnouncement = await ReplyAsync($"{Context.User.Mention} attempting to download {url}");
                    video = await YoutubeDownloadService.DownloadVideo(video);
                    try
                    { await downloadAnnouncement.DeleteAsync(); }
                    catch {}
                }
                Log.Verbose($"Starting audio playback.", "Command Queue");
                video.Requester = Context.User.Mention;

                await ReplyAsync($"{Context.User.Mention} queued **{video.Title}** | `{TimeSpan.FromSeconds(video.Duration)}`");
                SongService.Queue(video, _voiceChannel, Context.Message.Channel);
            }
            catch (Exception e)
            { Log.Warning($"Error while processing song requet: {e}", "Audio Request"); }
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
                catch {}

                if (stream == null)
                {
                    await ReplyAsync($"{Context.User.Mention} unable to open live stream, make sure its is a valid supported URL or contact a server admin.");
                    return;
                }

                stream.Requester = Context.User.Mention;
                stream.Url = url;

                Log.Information($"Attempting to stream {stream}", "Audio Stream");

                await ReplyAsync($"{Context.User.Mention} queued **{stream.Title}** | `{stream.DurationString}`");
                var _voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if (_voiceChannel == null)
                {
                    Log.Warning("Error joining Voice Channel!", "Audio Stream");
                    await ReplyAsync($"I can't connect to your Voice Channel.");
                }
                else
                { SongService.Queue(stream, _voiceChannel, Context.Message.Channel); }
            }
            catch (Exception e)
            { Log.Warning($"Error while processing song requet: {e}", "Audio Stream"); }
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
            catch {}
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
            List<IPlayable> songlist = SongService.SongList(Context.Guild);
            if (songlist.Count == 0)
            { await ReplyAsync($"{Context.User.Mention} current queue is empty"); }
            else
            {
                string msg = "";
                var nowPlaying = songlist.FirstOrDefault();
                var qList = songlist;
                qList.Remove(nowPlaying);
                msg += $"** Now Playing : **\n  - *{nowPlaying.Title}* (`{nowPlaying.DurationString}`)\n\n";
                if (qList.Count > 0)
                { msg += "** Songs in queue : **"; }
                foreach (var song in qList)
                { msg += $"\n  - *{song.Title}* (`{song.DurationString}`)"; }
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
        [Command("cachelist"), Summary("Displays the list of cached videos")]
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
                        var title = line.Substring(0, line.Length-(filename.Length + 2));
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
                catch {}
            }
        }
        #endregion

        #region cache delete
        /// <summary>
        /// CACHE DELETE - Delete cached files
        /// </summary>
        /// <returns></returns>
        [Command("cachedel"), Summary("Delete cache files")]
        public async Task Delete(string input=null)
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
            catch {}
        }
        #endregion

        [Command("loglvl"), Summary("Delete cache files")]
        public async Task LogLevel(ushort log = 10)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { }
            if (log <= 5)
            {
                DiVA.logLvl = log;
                await Context.User.SendMessageAsync($"Setting Log Level to {log}");
            }
            else
            { await Context.User.SendMessageAsync($"Please enter a value between 0 (Critical Messages) and 5(Debug messages)"); }
        }


        #endregion
    }

}
