using Discord;
using Discord.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;

namespace DiVA.Services
{

    /// <summary>
    /// Audio service
    /// </summary>
    public class AudioService
    {

        /// <summary>
        /// List of VoiceChannels by server
        /// </summary>
        public readonly ConcurrentDictionary<ulong, VoiceConnexion> ConnectedChannels = new ConcurrentDictionary<ulong, VoiceConnexion>();

        /// <summary>
        /// Service CTOR
        /// </summary>
        public AudioService()
        { //Not used : _songQueue = new BufferBlock<IPlayable>(); 
        } 

        /// <summary>
        /// NowPlaying var
        /// </summary>
        public IPlayable NowPlaying { get; private set; }

        /// <summary>
        /// Quit the voice channel
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task Quit(IGuild guild)
        {
            ConnectedChannels.TryGetValue(guild.Id, out VoiceConnexion voice);
            try
            { voice.StopCurrentOperation(); }
            finally
            { Logger.Log(Logger.Verbose, $"Stopped current audio stream for guild {voice.Channel.Guild.Name}", "Audio Quit"); }
            await voice.Channel.DisconnectAsync();
            ConnectedChannels.TryRemove(voice.Channel.Guild.Id, out VoiceConnexion _tempVoice);
        }

        /// <summary>
        /// Skips current song
        /// </summary>
        public void Next(ulong id)
        {
            ConnectedChannels.TryGetValue(id, out VoiceConnexion voice);
            voice?.Queue.Remove(voice.Queue.FirstOrDefault());
            voice?.StopCurrentOperation();
        }

        /// <summary>
        /// Clear queue
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public IList<IPlayable> Clear(IGuild guild)
        {
            try
            {
                ConnectedChannels.TryGetValue(guild.Id, out VoiceConnexion voice);
                Console.WriteLine(voice);
                Console.WriteLine(voice.Queue);
                Console.WriteLine(voice.Queue.Count);
                Console.WriteLine(voice.Queue.FirstOrDefault().Title);
                Logger.Log(Logger.Info, $"Skipped {voice.Queue.Count} songs", "Audio Skip");
                voice.Queue.Clear();
                return voice.Queue;
            }
            catch
            { return null; }
        }

        /// <summary>
        /// Add a song to the queue
        /// </summary>
        /// <param name="video"></param>
        /// <param name="voiceChannel"></param>
        /// <param name="messageChannel"></param>
        public async void Queue(IPlayable video, IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            bool firstConnexion = false;
            if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion tempsVoice))
            {
                Logger.Log(Logger.Info, "Connecting to voice channel", "Audio Queue");
                VoiceConnexion connexion = new VoiceConnexion
                {
                    Channel = voiceChannel,
                    Queue = new List<IPlayable>(),
                    Client = await voiceChannel.ConnectAsync()
                };
                if (ConnectedChannels.TryAdd(voiceChannel.Guild.Id, connexion))
                { Logger.Log(Logger.Info, "Connected to voice", "Audio Queue"); }
                Logger.Log(Logger.Verbose, $"Connected to {ConnectedChannels.Count} guilds", "Audio Queue");
                firstConnexion = true;
            }
            Logger.Log(Logger.Verbose, $"Added video : {video.Title} ({video.Uri})\n   to channel {voiceChannel.Guild.Name} :: {voiceChannel.Name}", "Audio Queue");
            ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion voice);
            lock(voice)
            { voice.Queue.Add(video); }
            
            if (firstConnexion)
            { voice.ProcessQueue(voiceChannel, messageChannel, ConnectedChannels); }
        }

        /// <summary>
        /// Lists current songs
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public List<IPlayable> SongList(IGuild guild)
        {
            ConnectedChannels.TryGetValue(guild.Id, out VoiceConnexion voice);
            Logger.Log(Logger.Verbose, $"{voice.Queue.Count} songs registered.", "Audio");
            return voice.Queue;
        }

        public async void Say(string said, IVoiceChannel voiceChannel, string culture = "en-US")
        {
            if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion tempsVoice))
            {
                Logger.Log(Logger.Info, "Connecting to voice channel", "Audio Queue");
                VoiceConnexion connexion = new VoiceConnexion
                {
                    Channel = voiceChannel,
                    Queue   = new List<IPlayable>(),
                    Client  = await voiceChannel.ConnectAsync()
                };
                connexion.CurrentStream = connexion.Client.CreatePCMStream(AudioApplication.Mixed);
                tempsVoice = connexion;
                if (ConnectedChannels.TryAdd(voiceChannel.Guild.Id, connexion))
                { Logger.Log(Logger.Info, "Connected to voice", "Audio Queue"); }
                Logger.Log(Logger.Verbose, $"Connected to {ConnectedChannels.Count} guilds", "Audio Queue");
            }
            await tempsVoice.SayAsync(said, culture);
        }

        internal float SetVolume(ulong id, int? vol)
        {
            ConnectedChannels.TryGetValue(id, out VoiceConnexion voice);
            voice.Volume = (float)(vol/100.0);
            return voice.Volume;
        }

        internal object GetVolume(ulong id)
        {
            ConnectedChannels.TryGetValue(id, out VoiceConnexion voice);
            return voice.Volume;
        }
    }


}