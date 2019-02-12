using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace DiVA.Services.YouTube
{
    /// <summary>
    /// JSON metadata of a stream
    /// </summary>
    public class StreamFormatMetadata
    {
        /// <summary>
        /// Stream Format
        /// </summary>
        [JsonProperty(PropertyName = "format")]
        public string Format { get; set; }

        /// <summary>
        /// Stream URL
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Stream audio codec
        /// </summary>
        [JsonProperty(PropertyName = "acodec")]
        public string Codec { get; set; }
    }

    /// <summary>
    /// Metadata of a stream
    /// </summary>
    public class StreamMetadata : IPlayable
    {
        /// <summary>
        /// Stream URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Stream URI
        /// </summary>
        public string Uri => Formats.First().Url;

        /// <summary>
        /// Stream file full path
        /// </summary>
        public string FullPath => Path.Combine(AppContext.BaseDirectory, "Songs", Uri);

        /// <summary>
        /// Stream requester
        /// </summary>
        public string Requester { get; set; }

        /// <summary>
        /// Stream Duration
        /// </summary>
        public string DurationString => "Live";

        /// <summary>
        /// Stream speed
        /// </summary>
        public int Speed => 48;

        /// <summary>
        /// Stream Title
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Stream ViewCount
        /// </summary>
        [JsonProperty(PropertyName = "view_count")]
        public string ViewCount { get; set; }

        /// <summary>
        /// Stream formats
        /// </summary>
        [JsonProperty(PropertyName = "formats")]
        public StreamMetadata[] Formats { get; set; }

        /// <summary>
        /// TO BE OVERRIDDEN
        /// </summary>
        public void OnPostPlay()
        {
        }
    }
}