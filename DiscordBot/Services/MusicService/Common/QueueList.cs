using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Victoria;
using Victoria.Entities;

namespace DiscordBot.Services.Common
{
    public class QueuList
    {
        private EmbedBuilder embedBuilder;
        private TimeSpan totalLenght;
        private const string InviteBot = "https://discordapp.com/api/oauth2/authorize?client_id=488973809185980416&permissions=0&scope=bot";

        private string Queuelist(List<Victoria.Queue.IQueueObject> queue)
        {
            string result = "";
            for (int i = 1; i <= queue.Count && i < 10; i++)
            {
                LavaTrack track = (LavaTrack)queue[i - 1];
                result += $"**{i}.** " + track.Title + " **[" + track.Length + "]**" + '\n';
                totalLenght += track.Length;
            }
            return result;
        }
        public Embed Print(SocketGuild guild, LavaPlayer player, DiscordSocketClient client)
        {
            embedBuilder.Author.IconUrl = guild.IconUrl;
            embedBuilder.Author.Name = $"{guild.Name}";
            if (player is null || player.Queue.Count == 0)
                embedBuilder.AddField("**Плэйлист**", "Плэйлист пуст" + '\n' + $"[Пригласить бота]({InviteBot})");
            else
            {
                embedBuilder.AddField("**Плейлист**", Queuelist(player.Queue.Items.ToList()) + '\n' + $"Количество треков [{player.Queue.Count}] | {totalLenght} общая длительность" + '\n' + $"[Пригласить бота]({InviteBot})");
            }
           
            embedBuilder.Footer.IconUrl = client.CurrentUser.GetAvatarUrl();
            embedBuilder.Footer.Text = $" | .help";
            return embedBuilder.Build();
        }
        public QueuList()
        {
            embedBuilder = new EmbedBuilder();
            embedBuilder.Author = new EmbedAuthorBuilder();
            embedBuilder.Footer = new EmbedFooterBuilder();
            embedBuilder.Color = Color.DarkPurple;
        }
    }
}
