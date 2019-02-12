using Newtonsoft.Json;
using System;
using System.IO;

namespace DiVA.Services.YouTube
{
    /// <summary>
    /// Represents a downloaded video.
    /// </summary>
    public class DownloadedVideo : IPlayable
    {
        /// <summary>
        /// Creates a Video
        /// </summary>
        /// <param name="title"></param>
        /// <param name="duration"></param>
        /// <param name="url"></param>
        /// <param name="id"></param>
        /// <param name="filename"></param>
        public DownloadedVideo(string title, int duration, string url, string id, string filename)
        {
            this.Title = title;
            this.Duration = duration;
            this.Url = url;
            this.DisplayID = id;
            this.FileName = filename;
        }

        /// <summary>
        /// Title of the video
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Duration of the video in seconds.
        /// </summary>
        [JsonProperty(PropertyName = "duration")]
        public int Duration { get; set; }

        /// <summary>
        /// The URL used to access the video site (note: not the actual video itself).
        /// </summary>
        [JsonProperty(PropertyName = "webpage_url")]
        public string Url { get; set; }

        /// <summary>
        /// Unique ID of the video, e.g. YouTube video ID.
        /// </summary>
        [JsonProperty(PropertyName = "display_id")]
        public string DisplayID { get; set; }

        /// <summary>
        /// Name of the file it got stored on.
        /// </summary>
        [JsonProperty(PropertyName = "_filename")]
        public string FileName { get; set; }

        /// <summary>
        /// Person requesting the download
        /// </summary>
        public string Requester { get; set; }

        /// <summary>
        /// Duration of the IPlayable Item
        /// </summary>
        public string DurationString => TimeSpan.FromSeconds(Duration).ToString();

        /// <summary>
        /// Returns the Filename of a file
        /// </summary>
        public string Uri => DisplayID + ".mp3";

        /// <summary>
        /// Returns the full path of file
        /// </summary>
        public string FullPath => Path.Combine(AppContext.BaseDirectory, "Songs", Uri);

        /// <summary>
        /// Speed modifier passed to ffmpeg (Frequency)
        /// </summary>
        public int Speed { get; set; } = 48;

        /// <summary>
        /// After song Handler
        /// </summary>
        public void OnPostPlay()
        { /*File.Delete(Uri); //Comment this to ceep cached videos */ }
    }
}