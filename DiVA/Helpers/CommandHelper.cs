using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
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
                $"I am {Client.CurrentUser.Username}. Can I do anything for you today ?",
                $"I am {Client.CurrentUser.Username}. Can I help you ?"
            };
            var msg = hiList[_rnd.Next(hiList.Count - 1)];
            if (iAm)
            { msg += $"\n{IAmList[_rnd.Next(IAmList.Count - 1)]}"; }
            await Channel.SendMessageAsync(msg);
        }

        public static string DiceRoll(string dice, string mention)
        {
            try
            {
                var result = dice
                             .Split('d')
                             .Select(input =>
                             {
                                 int? output = null;
                                 if (int.TryParse(input, out var parsed))
                                 { output = parsed; }
                                 return output;
                             })
                             .Where(x => x != null)
                             .Select(x => x.Value)
                             .ToArray();
                string msg   = $"{mention} rolled {result[0]}d{result[1]}";
                var    range = Enumerable.Range(0, result[0]);
                int[]  dices = new int[result[0]];
                Random _rnd  = new Random();
                foreach (var r in range)
                { dices[r] = _rnd.Next(1, result[1]); }
                msg += "\n [ **";
                msg += string.Join("** | **", dices);
                msg += "** ]";
                return msg;
            }
            catch
            { return "Haha what the fuck was that ?"; }
        }
    }
}
