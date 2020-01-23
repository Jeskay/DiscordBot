using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace DiscordBot.Services.Info
{
    public class QueueList : IEmbeds
    {
        public EmbedBuilder embedBuilder { get; }
        private TimeSpan totalLenght;
        private const string InviteBot = "https://discordapp.com/api/oauth2/authorize?client_id=488973809185980416&permissions=0&scope=bot";

        private string Queuelist(List<Victoria.Interfaces.IQueueable> queue)
        {
            string result = "";
            for (int i = 1; i <= queue.Count && i < 10; i++)
            {
                LavaTrack track = (LavaTrack)queue[i - 1];
                result += $"**{i}.** " + track.Title + " **[" + track.Duration + "]**" + '\n';
                totalLenght += track.Duration;
            }
            return result;
        }
        public void UpdateQueue(SocketGuild guild, LavaPlayer player, SocketSelfUser selfUser) 
        {
            embedBuilder.Author.IconUrl = guild.IconUrl;
            embedBuilder.Author.Name = $"{guild.Name}";
            if (player is null || player.Queue.Count == 0)
                embedBuilder.AddField("**Плэйлист**", "Плэйлист пуст" + '\n' + $"[Пригласить бота]({InviteBot})");
            else
            {
                embedBuilder.AddField("**Плейлист**", Queuelist(player.Queue.Items.ToList()) + '\n' + $"Количество треков [{player.Queue.Count}] | {totalLenght} общая длительность" + '\n' + $"[Пригласить бота]({InviteBot})");
            }

            embedBuilder.Footer.IconUrl = selfUser.GetAvatarUrl();
            embedBuilder.Footer.Text = $" | .help";
        }
        public async Task<RestUserMessage> CreateEmbed(ISocketMessageChannel messageChannel)
        {
            return await messageChannel.SendMessageAsync("", false, embedBuilder.Build());
        }

        public async Task ModifyMessage(RestUserMessage message)
        {
            await message.ModifyAsync(msg => { msg.Embed = embedBuilder.Build(); msg.Content = ""; });//костыль
        }

        public QueueList()
        {
            embedBuilder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder(),
                Footer = new EmbedFooterBuilder(),
                Color = Color.DarkPurple
            };
        }
    }
}
