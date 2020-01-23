using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;

namespace DiscordBot.Services.Info
{
    public class SelectEmbed : IEmbeds
    {
        public EmbedBuilder embedBuilder { get; }

        private readonly Emoji[] ChooseEmojis = 
            {
            new Emoji("1\u20E3"),
            new Emoji("2\u20E3"),
            new Emoji("3\u20E3"),
            new Emoji("4\u20E3"),
            new Emoji("5\u20E3") 
            };
        private readonly Dictionary<ulong, KeyValuePair<ulong, IEnumerable<LavaTrack>>> _TrackingSearch;
        private SocketSelfUser selfUser;
        private IReadOnlyList<LavaTrack> searchResults;
        private SocketGuildUser user;

        private string PrintTracks(IReadOnlyList<LavaTrack> tracks)
        {
            string result = "";
            for (int i = 1; (i <= 5) && (i <= tracks.Count); i++)
            {
                result += $"`{i}.` " + $"[{tracks[i - 1].Title}]({tracks[i - 1].Url.ToString()})" + " **[" + tracks[i - 1].Duration + "]** \n";
            }
            return result;
        }
        public void AddSelection(ulong MessageId, IReadOnlyList<LavaTrack> search)
        {
            _TrackingSearch.Add(MessageId, new KeyValuePair<ulong, IEnumerable<LavaTrack>>(user.Id, search));
        }
        public void RemoveSelection(ulong MessageId)
        {
            if(_TrackingSearch.ContainsKey(MessageId))_TrackingSearch.Remove(MessageId);
        }
        public LavaTrack CheckSelection(SocketGuildUser guildUser, SocketReaction reaction)
        {
            if (!_TrackingSearch.ContainsKey(reaction.MessageId)) return null;
            if (guildUser.Id != _TrackingSearch[reaction.MessageId].Key) return null;

            for (int i = 1; i <= 5; i++)
                if (reaction.Emote.Name[0].ToString() == i.ToString()) return _TrackingSearch[reaction.MessageId].Value.ElementAt(i - 1);
            return null;
        }
        public void Update(SocketSelfUser selfUser, IReadOnlyList<LavaTrack> searchResults, SocketGuildUser user)
        {
            this.selfUser = selfUser;
            this.searchResults = searchResults;
            this.user = user;
        }
        public async Task<RestUserMessage> CreateEmbed(ISocketMessageChannel messageChannel)
        {
            embedBuilder.Author.Name = "Выберите трек";
            embedBuilder.Author.IconUrl = selfUser.GetAvatarUrl();
            embedBuilder.Description = PrintTracks(searchResults);
            embedBuilder.Footer.IconUrl = user.GetAvatarUrl();
            embedBuilder.Footer.Text = "Для выбора трека нажмите на реакцию | .help";

            RestUserMessage message = await messageChannel.SendMessageAsync("", false, embedBuilder.Build());
            foreach (var item in ChooseEmojis)
            {
                await message.AddReactionAsync(item);
                Thread.Sleep(300);
            }
            return message;
        }

        public Task ModifyMessage(RestUserMessage message)
        {
            throw new NotImplementedException();
        }

        public SelectEmbed()
        {
            _TrackingSearch = new Dictionary<ulong, KeyValuePair<ulong, IEnumerable<LavaTrack>>>();
            embedBuilder = new EmbedBuilder
            {
                Footer = new EmbedFooterBuilder(),
                Author = new EmbedAuthorBuilder(),
                Color = Color.Green
            };
        }
    }
}
