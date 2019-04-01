 namespace DiVA
{
    internal class DiVAConfiguration
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public DiVAConfiguration(string prefix = "..", Tokens token = null)
        {
            Prefix = prefix;
            Tokens = token;
        }
    }
    internal class Tokens
    {
        public string Discord { get; set; }
        public string Youtube { get; set; }
        public Tokens(string discord = "", string youtube = "")
        {
            Discord = discord;
            Youtube = youtube;
        }
    }
}