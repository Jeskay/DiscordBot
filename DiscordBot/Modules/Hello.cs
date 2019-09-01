using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

using Discord;
using Discord.Commands;
namespace DiscordBot.Modules
{
    public class Ping : ModuleBase<SocketCommandContext>
    {
        [Command("Ping")]
        public async Task Pong()
        {
            await ReplyAsync("Pong");
        }
    }
}
