using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Services.Info
{
    public class WarningEmbed
    {
        private EmbedBuilder embedBuilder;
        public Embed ShouldbeInVoice()
        {
            embedBuilder.Color = Color.Red;
            embedBuilder.Description = "Вы должны быть подключены к голосовому каналу для использования этой команды.";
            return embedBuilder.Build();
        }
        public Embed NotEnoughPermission(DiscordSocketClient socketClient)
        {
            embedBuilder.Color = Color.Red;
            embedBuilder.Description = "Недостаточно прав.";
            embedBuilder.Footer.Text = "Позовите администрацию.";
            embedBuilder.Footer.IconUrl = socketClient.CurrentUser.GetAvatarUrl();
            return embedBuilder.Build();
        }
        public Embed EndQueue()
        {
            embedBuilder.Color = Color.Blue;
            embedBuilder.Description = "Конец **плэйлиста**";
            embedBuilder.Footer.Text = "Чтобы открыть плейлист | .q";
            return embedBuilder.Build();
        }
        public Embed NowPlaying(string trackname)
        {
            embedBuilder.Color = Color.DarkPurple;
            embedBuilder.Description = $"Сейчас играет: **{trackname}**";
            embedBuilder.Footer.Text = ".help";
            return embedBuilder.Build();
        }
        public Embed Added(string username, string tracktitle)
        {
            embedBuilder.Color = Color.Green;
            embedBuilder.Description = $"{username} добавил в плейлист **{tracktitle}**";
            embedBuilder.Footer.Text = ".help";
            return embedBuilder.Build();
        }
        public Embed AddandPlay(string title, string lenght, string author)
        {
            embedBuilder.Color = Color.DarkPurple;
            embedBuilder.Description = $"Сейчас играет: **{title}** [{lenght}] (Добавил {author})";
            embedBuilder.Footer.Text = ".help";
            return embedBuilder.Build();
        }
        public Embed LeavingRoom(string room, string url)
        {
            embedBuilder.Color = Color.Green;
            embedBuilder.Description = $"Покидаю комнату [{room}]({url}).";
            embedBuilder.Footer.Text = ".help";
            return embedBuilder.Build();
        }
        public Embed NoUserPermission(string username)
        {
            embedBuilder.Color = Color.Red;
            embedBuilder.Description =  $"{username}У вас недостаточно прав.";
            return embedBuilder.Build();
        }
        public Embed IsUsing(SocketVoiceChannel voiceChannel)
        {
            embedBuilder.Color = Color.Blue;
            embedBuilder.Description = $"Бот уже находится в голосовом канале [{voiceChannel.Name}]({voiceChannel.CreateInviteAsync().Result.Url}).";
            return embedBuilder.Build();
        }
        public WarningEmbed()
        {
            embedBuilder = new EmbedBuilder();
            embedBuilder.Footer = new EmbedFooterBuilder();
        }
    }
}
