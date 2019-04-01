using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace DiVA.Services.YouTube
{
    /// <summary>
    /// Youtube Downloader
    /// </summary>
    public class YouTubeDownloadService
    {
        /// <summary>
        /// Donwload a video
        /// </summary>
        /// <param name="video"></param>
        /// <returns></returns>
        public async Task<DownloadedVideo> DownloadVideo(DownloadedVideo video)
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "Songs", video.DisplayID);
            var youtubeDl = StartYoutubeDl(
                $"-x -f bestaudio --audio-quality 0 --audio-format mp3 --add-metadata --print-json -o \"{outputPath}.%(ext)s\" {video.Url}");
                //$"-o {outputPath} --restrict-filenames --extract-audio --no-overwrites --print-json --audio-format mp3 {video.Url}");

            if (youtubeDl == null)
            {
                Logger.Log(Logger.Warning, "Error: Unable to start process", "Audio Download");
                return null;
            }

            var jsonOutput = await youtubeDl.StandardOutput.ReadToEndAsync();
            youtubeDl.WaitForExit();
            Logger.Log(Logger.Info, $"Download completed with exit code {youtubeDl.ExitCode}", "Audio Download");

            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "Songs", "songlist.cache"), $"{video.Title},{video.DisplayID}.mp3;\n");

            return JsonConvert.DeserializeObject<DownloadedVideo>(jsonOutput);
        }

        /// <summary>
        /// Download a Livestream's data
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<StreamMetadata> GetLivestreamData(string url)
        {
            var youtubeDl = StartYoutubeDl("--print-json --skip-download " + url);
            var jsonOutput = await youtubeDl.StandardOutput.ReadToEndAsync();
            youtubeDl.WaitForExit();
            Logger.Log(Logger.Info, $"Data recovery completed with exit code {youtubeDl.ExitCode}", "Audio Download");

            return JsonConvert.DeserializeObject<StreamMetadata>(jsonOutput);
        }
        
        /// <summary>
        /// Get a video's data
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static async Task<DownloadedVideo> GetVideoData(string search)
        {

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = DiVA.Configuration["tokens:youtube"],
                ApplicationName = "DiVA YT API"
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = search;
            searchListRequest.Type = "video";
            searchListRequest.MaxResults = 1;
            var searchListResponse = await searchListRequest.ExecuteAsync();

            var searchVideoRequest = youtubeService.Videos.List("snippet,contentDetails,statistics");
            searchVideoRequest.Id = searchListResponse.Items[0].Id.VideoId;
            var searchVideoResponse = await searchVideoRequest.ExecuteAsync();
            var video = searchVideoResponse.Items[0];
            int duration = (int)XmlConvert.ToTimeSpan(video.ContentDetails.Duration).TotalSeconds;
            Logger.Log(Logger.Info, $"Recovery complete : {video.Snippet.Title} saved to {video.Id}.mp3");
            return new DownloadedVideo(video.Snippet.Title, duration, "https://www.youtube.com/watch?v=" + video.Id, video.Id, video.Id+".mp3" );
        }

        /// <summary>
        /// Starting Youtube-DL process
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private static Process StartYoutubeDl(string arguments)
        { 
            var youtubeDlStartupInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                FileName = "youtube-dl",
                Arguments = arguments
            };

            Logger.Log(Logger.Info, $"Starting youtube-dl with arguments: {youtubeDlStartupInfo.Arguments}", "Audio Download");
            return Process.Start(youtubeDlStartupInfo);
        }
    }
}