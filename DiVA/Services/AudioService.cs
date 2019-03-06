using Discord;
using Discord.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        /// Playback service
        /// </summary>
        //public AudioPlaybackService AudioPlaybackService { get; set; }

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
            { Log.Verbose($"Stopped current audio stream for guild {voice.Channel.Guild.Name}", "Audio Quit"); }
            await voice.Channel.DisconnectAsync();
            ConnectedChannels.TryRemove(voice.Channel.Guild.Id, out VoiceConnexion tempVoice);
        }

        /// <summary>
        /// Skips current song
        /// </summary>
        public void Next(ulong Id)
        {
            ConnectedChannels.TryGetValue(Id, out VoiceConnexion voice);
            voice.Queue.Remove(voice.Queue.FirstOrDefault());
            voice.StopCurrentOperation();
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
                var songQueue = voice.Queue;
                Log.Information($"Skipped {songQueue.Count} songs", "Audio Skip");
                songQueue.Clear();
                return songQueue;
            }
            catch
            { return null; }
        }

        /// <summary>
        /// Add a song to the queue
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="video"></param>
        /// <param name="voiceChannel"></param>
        /// <param name="messageChannel"></param>
        public async void Queue(IPlayable video, IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            bool firstConnexion = false;
            if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion tempsVoice))
            {
                Log.Information("Connecting to voice channel", "Audio Queue");
                VoiceConnexion connexion = new VoiceConnexion
                {
                    Channel = voiceChannel,
                    Queue = new List<IPlayable>(),
                    Client = await voiceChannel.ConnectAsync()
                };
                if (ConnectedChannels.TryAdd(voiceChannel.Guild.Id, connexion))
                { Log.Information("Connected to voice", "Audio Queue"); }
                Log.Verbose($"Connected to {ConnectedChannels.Count} guilds", "Audio Queue");
                firstConnexion = true;
            }
            Log.Verbose($"Added video : {video.Title} ({video.Uri})\n   to channel {voiceChannel.Guild.Name} :: {voiceChannel.Name}", "Audio Queue");
            ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion voice);
            voice.Queue.Add(video);
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
            Log.Verbose($"{voice.Queue.Count} songs registered.", "Audio");
            return voice.Queue;
        }

        

        internal float SetVolume(ulong id, int? vol)
        {
            ConnectedChannels.TryGetValue(id, out VoiceConnexion voice);
            voice.volume = (float)(vol/100.0);
            return voice.volume;
        }

        internal object GetVolume(ulong id)
        {
            ConnectedChannels.TryGetValue(id, out VoiceConnexion voice);
            return voice.volume;
        }
    }


}