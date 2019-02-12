namespace DiVA.Services
{
    /// <summary>
    /// Define a Playable Item. Can be either a Stream or a Video
    /// </summary>
    public interface IPlayable
    {
        /// <summary>
        /// URL of the item
        /// </summary>
        string Url { get; set; }

        /// <summary>
        /// URI of the item
        /// </summary>
        string Uri { get; }

        /// <summary>
        /// Title of the item
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Requester of the item
        /// </summary>
        string Requester { get; set; }

        /// <summary>
        /// Duration of the item as a string
        /// </summary>
        string DurationString { get; }

        /// <summary>
        /// FullPath of file
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// Speed of the item
        /// </summary>
        int Speed { get; }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        void OnPostPlay();
    }
}
