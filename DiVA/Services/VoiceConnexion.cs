using Discord;
using Discord.Audio;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DiVA.Services
{
    /// <summary>
    /// Represents a connexion to a voicechannel of a server
    /// </summary>
    public class VoiceConnexion
    {
        /// <summary>
        /// Voice channel we are connected at
        /// </summary>
        public IVoiceChannel Channel { get; set; }

        /// <summary>
        /// Audio Client used by the channel
        /// </summary>
        public IAudioClient Client { get; set; }

        public AudioOutStream CurrentStream { get; set; }

        /// <summary>
        /// Queue of Iplayables
        /// </summary>
        public List<IPlayable> Queue { get; set; }

        /// <summary>
        /// Volume set for the client
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// FFMpeg process
        /// </summary>
        private Process _currentProcess;

        /// <summary>
        /// Current song
        /// </summary>
        public  IPlayable NowPlaying { get; set; }

        /// <summary>
        /// Handled the Queue of a Voice Client
        /// </summary>
        /// <param name="voiceChannel"></param>
        /// <param name="messageChannel"></param>
        /// <param name="ConnectedChannels"></param>
        public async void ProcessQueue(IVoiceChannel voiceChannel, IMessageChannel messageChannel, ConcurrentDictionary<ulong, VoiceConnexion> ConnectedChannels)
        {
            using (var stream = Client.CreatePCMStream(AudioApplication.Music, bufferMillis: 2000))
            {
                while (Queue.Count > 0)
                {
                    Logger.Log(Logger.Info, "Waiting for songs", "Audio ProcessQueue");
                    NowPlaying = Queue.FirstOrDefault();
                    try
                    {
                        await messageChannel?.SendMessageAsync($"Now playing **{NowPlaying.Title}** | `{NowPlaying.DurationString}` | requested by {NowPlaying.Requester}");
                        await Client.SetSpeakingAsync(true);
                        Volume = 0.25f;
                        try
                        { await SendAsync(Volume, NowPlaying.FullPath, stream); }
                        catch (OperationCanceledException)
                        { Logger.Log(Logger.Verbose, "Song have been skipped.", "Audio ProcessQueue"); }
                        catch (InvalidOperationException)
                        { Logger.Log(Logger.Verbose, "Song have been skipped.", "Audio ProcessQueue"); }
                        await Client.SetSpeakingAsync(false);
                        Queue.Remove(NowPlaying);
                        NowPlaying.OnPostPlay();
                        Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    { Logger.Log(Logger.Warning, $"Error while playing song: {e}", "Audio ProcessQueue"); }
                }
            }
            await Channel.DisconnectAsync();
            ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out VoiceConnexion tempVoice);
        }

        

        /// <summary>
        /// Voice sender
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="path"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task SendAsync(float volume, string path, AudioOutStream stream)
        {
            _currentProcess = CreateStream(path);
            _currentProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
            await Task.Delay(1000).ConfigureAwait(false);
            while (true)
            {
                if (_currentProcess.HasExited)
                { break; }
                int    blockSize = 2880;
                byte[] buffer    = new byte[blockSize];
                int    byteCount;
                byteCount = await _currentProcess.StandardOutput.BaseStream.ReadAsync(buffer, 0, blockSize);
                if (byteCount == 0)
                { break; }
                if (stream != null) await stream.WriteAsync(buffer, 0, byteCount);
            }
            if (stream != null) await stream.FlushAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="said">Text to say</param>
        /// <param name="culture">Supported cultures are 'en-US' or 'fr-FR'</param>
        /// <returns></returns>
        public async Task SayAsync(string said, string culture = "en-US")
        {
            var filename = $"TTS{DateTime.Now:HH-mm-ss-ffff}.wav";
            var TTSHelper = new ProcessStartInfo
            {
                UseShellExecute        = false,
                RedirectStandardOutput = false,
                CreateNoWindow         = true,
                FileName               = "TTSHelper.exe",
                Arguments              = $"{Path.Combine(AppContext.BaseDirectory, "temp", filename)} {culture} {said}"
            };

            Logger.Log(Logger.Info, $"Starting tts helper with arguments: {TTSHelper.Arguments}", "Say TTS");
            Process.Start(TTSHelper)?.WaitForExit();

            var path = Path.Combine(AppContext.BaseDirectory, "temp", filename);

            Volume = 0.25f;
            try
            { await SendAsync(Volume, path, CurrentStream); }
            catch (OperationCanceledException)
            { Logger.Log(Logger.Verbose, "Stopped speaking", "Say Audio"); }
            catch (InvalidOperationException)
            { Logger.Log(Logger.Verbose, "Stopped speaking.", "Say Audio"); }
            await Client.SetSpeakingAsync(false);
            try
            { File.Delete(Path.Combine(AppContext.BaseDirectory, "temp", filename)); }
            finally
            { CurrentStream.FlushAsync(); }
        }

        //TO DO make this work
        //public byte[] ScaleVolumeSafeAllocateBuffers(byte[] audioSamples, float volume)
        //{
        //    if (audioSamples == null) return null;
        //    if (audioSamples.Length % 2 != 0) return null;
        //    if (volume < 0f || volume > 1f) return null;
        //    var output = new byte[audioSamples.Length];
        //    if (Math.Abs(volume - 1f) < 0.0001f)
        //    {
        //        Buffer.BlockCopy(audioSamples, 0, output, 0, audioSamples.Length);
        //        return output;
        //    }
        //    int volumeFixed = (int)Math.Round(volume * 65536d);
        //    for (var i = 0; i < output.Length; i += 2)
        //    {
        //        int sample = (short)((audioSamples[i + 1] << 8) | audioSamples[i]);
        //        int processed = (sample * volumeFixed) >> 16;
        //        output[i] = (byte)processed;
        //        output[i + 1] = (byte)(processed >> 8);
        //    }
        //    return output;
        //}


        /// <summary>
        /// Skipper
        /// </summary>
        public void StopCurrentOperation()
        {
            _currentProcess?.Close();
            //We should not need this if we dispose it _currentProcess?.Kill();
            _currentProcess?.Dispose();
        }

        /// <summary>
        /// Stream creator
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                //Arguments = $"-i \"{path}\" -ac 2 -f s16le -filter:a \"volume=0.02\" -ar {speedModifier}000 pipe:1",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            Logger.Log(Logger.Info, $"Starting ffmpeg with args {ffmpeg.Arguments}", "Audio Create");
            return Process.Start(ffmpeg);
        }



    }
}
