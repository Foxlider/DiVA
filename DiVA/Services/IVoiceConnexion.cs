using Discord;
using Discord.Audio;
using System.Collections.Generic;

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

        /// <summary>
        /// Queue of Iplayables
        /// </summary>
        public List<IPlayable> Queue { get; set; }


    }
}
