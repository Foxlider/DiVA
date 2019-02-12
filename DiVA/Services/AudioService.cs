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
        { //_songQueue = new BufferBlock<IPlayable>(); 
        }

        /// <summary>
        /// Playback service
        /// </summary>
        public AudioPlaybackService AudioPlaybackService { get; set; }

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
            { AudioPlaybackService.StopCurrentOperation(); }
            finally
            { }
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
            AudioPlaybackService.StopCurrentOperation();
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
                { Log.Information("Connected!", "Audio Queue"); }
                firstConnexion = true;
                
            }
            ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion voice);

            voice.Queue.Add(video);

            if (firstConnexion)
            { ProcessQueue(voiceChannel, messageChannel); }
        }

        /// <summary>
        /// Lists current songs
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public List<IPlayable> SongList(IGuild guild)
        {
            ConnectedChannels.TryGetValue(guild.Id, out VoiceConnexion voice);
            return voice.Queue;
        }

        /// <summary>
        /// Handled the Queue of a Voice Client
        /// </summary>
        /// <param name="voiceChannel"></param>
        /// <param name="messageChannel"></param>
        private async void ProcessQueue(IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion voice);
            using (var stream = voice.Client.CreatePCMStream(AudioApplication.Mixed, bitrate: 48000, bufferMillis: 2000))
            {
                while (voice.Queue.Count > 0)
                {
                    Log.Information("Waiting for songs", "Audio ProcessQueue");
                    NowPlaying = voice.Queue.FirstOrDefault();
                    try
                    {
                        await messageChannel?.SendMessageAsync($"Now playing **{NowPlaying.Title}** | `{NowPlaying.DurationString}` | requested by {NowPlaying.Requester}");
                        await voice.Client.SetSpeakingAsync(true);
                        try
                        { await AudioPlaybackService.SendAsync(voice.Client, NowPlaying.FullPath, stream); }
                        catch (OperationCanceledException)
                        { Log.Verbose("Song have been skipped.", "Audio ProcessQueue"); }
                        catch (InvalidOperationException)
                        { Log.Verbose("Song have been skipped.", "Audio ProcessQueue"); }
                        await voice.Client.SetSpeakingAsync(false);
                        voice.Queue.Remove(NowPlaying);
                        NowPlaying.OnPostPlay();
                        Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    { Log.Warning($"Error while playing song: {e}", "Audio ProcessQueue"); }
                }
            }
            await voice.Channel.DisconnectAsync();
            ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out VoiceConnexion tempVoice);
        }
    }


}