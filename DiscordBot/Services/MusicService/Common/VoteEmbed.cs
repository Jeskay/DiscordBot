using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;

namespace DiscordBot.Services.Common
{
    public class VoteEmbed : IEmbeds
    {
        public static Dictionary<ulong, VoteEmbed> TrackingVote = new Dictionary<ulong, VoteEmbed>();
        private readonly SocketGuildUser user;
        private readonly LavaPlayer _player;
        private readonly Mutex voteMutex = new Mutex();
        public EmbedBuilder embedBuilder { get; }
        public int Votes { get; set; }
        public IVoiceChannel Channel { get; private set; }

        private async Task<Embed> Skip()
        {
            LavaTrack oldtrack = _player.Track;
            if (_player is null || _player.Queue.Count == 0)
            {
                if (_player.Track != null)
                {
                    await _player.SeekAsync(_player.Track.Duration);
                }
                embedBuilder.Description = $"пропущено {oldtrack.Title}";
                return embedBuilder.Build();
            }
            await _player.SkipAsync();
            embedBuilder.Description = $"пропущено {oldtrack.Title}, сейчас играет {_player.Track.Title}.";
            return embedBuilder.Build();
        }
        public async Task<Dictionary<ulong, VoteEmbed>> AddVote(SocketReaction reaction, ISocketMessageChannel messageChannel)
        {
            if (reaction.Emote.Name != "\u2611") return TrackingVote;
            try
            {
                await Channel.GetUserAsync(reaction.UserId);

            }
            catch (Exception)
            {
                return TrackingVote;
            }
            Votes++;
            var chnl = Channel as SocketGuildChannel;
            int users = chnl.Users.Count - 1;
            if (Votes >= (users / 2.0))
            {
                await messageChannel.SendMessageAsync("", false, await Skip());
                voteMutex.WaitOne();
                TrackingVote.Remove(reaction.MessageId);
                voteMutex.ReleaseMutex();
                await messageChannel.DeleteMessageAsync(reaction.MessageId);
            }
            return TrackingVote;
        }
        public async Task<Dictionary<ulong, VoteEmbed>> RemoveVote(SocketReaction reaction)//семафор или мьютекс со статическим словарем
        {
            if (reaction.Emote.Name != "\u2611") return TrackingVote;
            voteMutex.WaitOne();
            try
            {
                await TrackingVote[reaction.MessageId].Channel.GetUserAsync(reaction.UserId);
            }
            catch (Exception)
            {
                return TrackingVote;
            }
            TrackingVote[reaction.MessageId].Votes--;
            voteMutex.ReleaseMutex();
            return TrackingVote;
        }
        public async Task<RestUserMessage> CreateEmbed(ISocketMessageChannel channel)
        {
            embedBuilder.Description = $"{user.Mention} начал голосование за пропуск песни **{_player.Track.Title}**. \n Нажмите на реакцию для пропуска трека.";
            RestUserMessage msg = await channel.SendMessageAsync("", false, embedBuilder.Build());
            await msg.AddReactionAsync(new Emoji("\u2611"));
            return msg;
        }

        public async Task ModifyMessage(RestUserMessage message)//for future addons
        {
            await message.ModifyAsync(msg => { msg.Embed = embedBuilder.Build(); msg.Content = ""; });
        }

        public VoteEmbed(SocketGuildUser user, LavaPlayer player, SocketSelfUser selfuser)
        {
            embedBuilder = new EmbedBuilder();
            this.user = user;
            _player = player;
            Channel = player.VoiceChannel;
            embedBuilder.Footer = new EmbedFooterBuilder
            {
                Text = "Для просмотра плейлиста | . q",
                IconUrl = selfuser.GetAvatarUrl()
            };
        }
    }
}
