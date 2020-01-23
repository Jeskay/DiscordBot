using System;
using System.Collections.Generic;
using System.Text;
using Victoria;
using Victoria.Enums;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Discord.Rest;

namespace DiscordBot.Services.Common
{
    public class ControlPanel : IEmbeds
    {
        public EmbedBuilder embedBuilder { get; }

        private readonly PlayerController playerController;
        private const string SiteURL = "https://docs.google.com";
        private readonly Emoji[] ControlPanelEmojis = 
            { 
            new Emoji("\u23EE"),
            new Emoji("\u23EF"),
            new Emoji("🔲"),
            new Emoji("\u23ED"),
            new Emoji("🔺"),
            new Emoji("🔻"),
            new Emoji("\u274C")
            };

        public SocketGuildUser Provider { get; private set; }
   
        private string Writetime(TimeSpan time, TimeSpan totaltime)
        {
            if (totaltime >= new TimeSpan(1, 0, 0)) return (time.Hours < 10 ? "0" + time.Hours  : time.Hours.ToString()) + ":" + (time.Minutes < 10 ? "0" + time.Minutes  : time.Minutes.ToString()) + ":" + (time.Seconds < 10 ? "0" + time.Seconds : time.Seconds.ToString());
            else return (time.Minutes < 10 ? "0" + time.Minutes : time.Minutes.ToString()) + ":" + (time.Seconds < 10 ? "0" + time.Seconds : time.Seconds.ToString());
        }
        private async Task Update()
        {
            embedBuilder.Author = new EmbedAuthorBuilder
            {
                Name = playerController.Track.Author
            };
            embedBuilder.Description = $"{playerController.Track.Title}\n[Открыть плейлист]({SiteURL}) | .q";//ссылка на видео { _player.CurrentTrack.Uri.ToString()}
            embedBuilder.ThumbnailUrl = await playerController.Track.FetchArtworkAsync();
            embedBuilder.Author.IconUrl = embedBuilder.ThumbnailUrl;
            embedBuilder.Author.IconUrl = embedBuilder.ThumbnailUrl;
            embedBuilder.Fields[0].Value = $"> [ {playerController.Volume} ]";
            embedBuilder.Fields[1].Value = $"> [{Writetime(playerController.Track.Position, playerController.Track.Duration)} / {Writetime(playerController.Track.Duration, playerController.Track.Duration)}]";
            embedBuilder.ImageUrl = Position(playerController.Track.Position, playerController.Track.Duration);
        }
        private string Position(TimeSpan position, TimeSpan Lenght)
        {
            double pos = position.TotalSeconds;
            double len = Lenght.TotalSeconds;
            int Coeff = (int)(Math.Round(pos / len, 1) * 100);
            switch (Coeff)
            {
                case(0):
                    return "https://i.ibb.co/k0XbRRj/Volume1.png";
                case (10):
                    return "https://i.ibb.co/Gd7ZShY/Volume20.png";
                case (20):
                    return "https://i.ibb.co/0p7wRp7/Volume40.png";
                case (30):
                    return "https://i.ibb.co/g61KvX2/Volume60.png";
                case (40):
                    return "https://i.ibb.co/QbcyKqN/Volume80.png";
                case (50):
                    return "https://i.ibb.co/6tH0P2K/Volume100.png";
                case (60):
                    return "https://i.ibb.co/N7QSGLS/Volume120.png";
                case (70):
                    return "https://i.ibb.co/mSKW12S/Volume140.png";
                case (80):
                    return "https://i.ibb.co/fCdmjP3/Volume160.png";
                case (90):
                    return "https://i.ibb.co/R0zvkjq/Volume180.png";
                case (100):
                    return "https://i.ibb.co/30pmR6B/Volume200.png";
            }
            return null;
        }
        
        public async Task AddPositionAsync()
        {
            if (await playerController.AddPositionAsync())
            {
                embedBuilder.Fields[1].Value = $"> [{Writetime(playerController.Track.Position, playerController.Track.Duration)} / {Writetime(playerController.Track.Duration, playerController.Track.Duration)}]";
                embedBuilder.ImageUrl = Position(playerController.Track.Position, playerController.Track.Duration);
            }
        }

        public async Task RemovePositionAsync()
        {
            if (await playerController.RemovePositionAsync())
            {
                embedBuilder.Fields[1].Value = $"> [{Writetime(playerController.Track.Position, playerController.Track.Duration)} / {Writetime(playerController.Track.Duration, playerController.Track.Duration)}]";
                embedBuilder.ImageUrl = Position(playerController.Track.Position, playerController.Track.Duration);
            }
        }

        public async Task IncreaseVolumeAsync()
        {
            if(await playerController.IncreaseVolumeAsync())
                embedBuilder.Fields[0].Value = $"> [ {playerController.Volume} ]";
        }

        public async Task DecreaseVolumeAsync()
        {
            if(await playerController.DecreaseVolumeAsync())
                embedBuilder.Fields[0].Value = $"> [ {playerController.Volume} ]";
        }

        public async Task PauseOrResumeAsync()
        {
            await playerController.PauseOrResumeAsync();
        }

        public async Task SkipAsync()
        {
            if(await playerController.SkipAsync())
                await Update();
        }
        public async Task<Dictionary<ulong, ulong>> CheckCommand(ISocketMessageChannel channel, SocketReaction reaction, RestUserMessage message, LavaNode lavaNode, Dictionary<ulong, ulong> Providers)//переделать на симофор
        {
            var guild = (channel as SocketGuildChannel).Guild;
            if (reaction.User.Value.Id != Provider.Id) return Providers;
            switch (reaction.Emote.Name)
            {
                case ("🔺"):
                    await IncreaseVolumeAsync();
                    break;
                case ("🔻"):
                    await DecreaseVolumeAsync();
                    break;
                case("\u23EF"):
                    await PauseOrResumeAsync();
                    break;
                case("\u23ED"):
                    await AddPositionAsync();
                     break;
                case("\u23EE"):
                    await RemovePositionAsync();
                    break;
                case ("🔲"):
                    await SkipAsync();
                    break;
                case ("\u274C"):
                    Providers.Remove(reaction.UserId);
                    await channel.DeleteMessageAsync(reaction.MessageId);
                    await lavaNode.LeaveAsync(guild.CurrentUser.VoiceChannel);
                    break;
            }
            await ModifyMessage(message);
            return Providers;
        }
        public async Task<RestUserMessage> CreateEmbed(ISocketMessageChannel messageChannel)
        {
            await Update();
            RestUserMessage message = await messageChannel.SendMessageAsync("", false, embedBuilder.Build());
            foreach (var item in ControlPanelEmojis)
            {
                await message.AddReactionAsync(item);
                Thread.Sleep(300);
            }
            return message;
        }

        public async Task ModifyMessage(RestUserMessage message)
        {
            await Update();
            await message.ModifyAsync(msg => { msg.Embed = embedBuilder.Build(); msg.Content = ""; });//костыль
        }

        public ControlPanel(LavaPlayer player, SocketGuildUser provider)
        {
            Provider = provider;
            playerController = new PlayerController(player);
            embedBuilder = new EmbedBuilder
            {
                Color = Color.DarkPurple
            };
            embedBuilder.AddField("Громкость", $"> [ {playerController.Volume} ]");
            embedBuilder.AddField("Текущая позиция", $"> [{Writetime(playerController.Track.Position, playerController.Track.Duration)} / {Writetime(playerController.Track.Duration, playerController.Track.Duration)}]");
            embedBuilder.Footer = new EmbedFooterBuilder
            {
                Text = $"Управляющий - {Provider.Username}"
            };
            embedBuilder.ImageUrl = Position(playerController.Track.Position, playerController.Track.Duration);
        }
    }
}
