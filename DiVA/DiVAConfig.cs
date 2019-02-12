namespace DiVA
{
    internal class DiVAConfiguration
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public DiVAConfiguration(string prefix = "..", Tokens token = null)
        {
            this.Prefix = prefix;
            this.Tokens = token;
        }
    }
    internal class Tokens
    {
        public string Discord { get; set; }
        public string Youtube { get; set; }
        public Tokens(string discord = "", string youtube = "")
        {
            this.Discord = discord;
            this.Youtube = youtube;
        }
    }
}