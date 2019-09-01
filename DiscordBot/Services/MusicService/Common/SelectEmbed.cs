using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Victoria.Entities;

namespace DiscordBot.Services.Common
{
    public class SelectEmbed
    {
        private EmbedBuilder embed;
        public Embed Print(DiscordSocketClient socketClient, SearchResult searchResult, SocketGuildUser user)
        {
            embed.Author.Name = "Выберите трек";
            embed.Author.IconUrl = socketClient.CurrentUser.GetAvatarUrl();
            embed.Description = PrintTracks(searchResult);
            embed.Footer.IconUrl = user.GetAvatarUrl();
            embed.Footer.Text = "Для выбора трека нажмите на реакцию | .help";
            return embed.Build();
        }
        private string PrintTracks(SearchResult search)
        {
            var tracks = search.Tracks.ToList();
            string result = "";
            for (int i = 1; (i <= 5) && (i <= tracks.Count); i++)
            {
                result += $"`{i}.` " + $"[{tracks[i - 1].Title}]({tracks[i - 1].Uri.ToString()})" + " **[" + tracks[i - 1].Length + "]** \n";
            }
            return result;
        }
        public SelectEmbed()
        {
            embed = new EmbedBuilder();
            embed.Footer = new EmbedFooterBuilder();
            embed.Author = new EmbedAuthorBuilder();
            embed.Color = Color.Green;
        }
    }
}
