using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord.Rest;

namespace DiscordBot.Services
{
    public interface IEmbeds
    {
       Task<RestUserMessage> CreateEmbed(ISocketMessageChannel massagechannel);

       Task ModifyMessage(RestUserMessage message);
       
       EmbedBuilder embedBuilder { get; }
    }
}
