using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiVA.Helpers
{
    class CommandHelper
    {
        /// <summary>
        /// Answers hello to user
        /// </summary>
        /// <param name="Channel"></param>
        /// <param name="Client"></param>
        /// <param name="User"></param>
        /// <param name="_rnd"></param>
        /// <param name="iAm"></param>
        /// <returns></returns>
        public static async Task SayHelloAsync(IMessageChannel Channel, IDiscordClient Client, IUser User, Random _rnd, bool iAm = true)
        {
            List<string> hiList = new List<string>
            {
                $"Oh hello {User.Mention} ! ",
                $"Hi {User.Mention} ! ",
                $"Well hello to you, {User.Mention} ! ",
                $"G'day {User.Mention} !",
                $"Greetings {User.Mention}.",
                $"Oh ! Hi {User.Mention} !"
            };
            List<string> IAmList = new List<string>
            {
                $"I am {Client.CurrentUser.Username}, your Discord Virtual Assistant.",
                $"I am {Client.CurrentUser.Username}.",
                $"My name is {Client.CurrentUser.Username}. Pleased to meet you.",
                $"Name's {Client.CurrentUser.Username}. I'm your Discord Virtual Assistant.",
                $"I am {Client.CurrentUser.Username}. Can i do anything for you, today ?",
                $"I am {Client.CurrentUser.Username}. Can I help you ?"
            };
            var msg = hiList[_rnd.Next(hiList.Count - 1)];
            if (iAm)
            { msg += $"\n{IAmList[_rnd.Next(IAmList.Count - 1)]}"; }
            await Channel.SendMessageAsync(msg);
        }
    }
}
