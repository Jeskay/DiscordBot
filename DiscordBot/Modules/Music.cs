using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Services;
using Victoria;
using Victoria.Enums;

namespace DiscordBot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private MusicService _musicService;
        private Emoji[] ChooseEmojis = { new Emoji("1\u20E3"), new Emoji("2\u20E3"), new Emoji("3\u20E3"), new Emoji("4\u20E3"), new Emoji("5\u20E3") };

        public Music(MusicService musicService)
        {
            _musicService = musicService;
        }
        [Command("Help")]
        public async Task Help()
        {
            if (!_musicService.CheckMessage(Context.Message)) return;
            await ReplyAsync("", false, _musicService.PrintHelp());
            await Context.Channel.DeleteMessageAsync(Context.Message.Id);
        }
        [Command("Play"), Alias("p")]
        public async Task Play([Remainder] string query)
        {
            if (!_musicService.CheckMessage(Context.Message)) return;
            try
            {
                await Context.Channel.DeleteMessageAsync(Context.Message.Id);
            }
            catch (Discord.Net.HttpException)
            {

            }
            if (!await _musicService.JoinAsync(Context.User as SocketGuildUser, Context.Channel)) return;
            /*if (_musicService.checkWebsite(query))
            {
                await ReplyAsync("", false, await _musicService.SearchUrl(query, Context.Guild, Context.User as SocketGuildUser));
                _musicService.ControlPanelAsync(Context.Channel);
                return;
            }*/
            var r = await _musicService.SearchAsync(query);
            RestUserMessage msg = await ReplyAsync("", false, _musicService.PrintTracks(r, Context.User as SocketGuildUser)) as RestUserMessage;
            foreach (var item in ChooseEmojis)
            {
                await msg.AddReactionAsync(item);
                Thread.Sleep(300);
            }
            _musicService._TrackingSearch.Add(msg.Id, new KeyValuePair<ulong, IEnumerable<LavaTrack>>(Context.User.Id, r));
            await _musicService.DeleteTimeoutAsync(msg);

        }
        [Command("q"), Alias("queue")]
        public async Task Queue()
        {
            if (!_musicService.CheckMessage(Context.Message)) return;
            var result = _musicService.PrintQueue(Context.Guild);
            await ReplyAsync("", false, result);
            await Context.Channel.DeleteMessageAsync(Context.Message.Id);
        }

        [Command("Skip"), Alias("s")]
        public async Task Skip()
        {
            if (!_musicService.CheckMessage(Context.Message)) return;
            await _musicService.SkipAsync(Context.Channel, Context.User as SocketGuildUser);
        }
        [Command("ControlPanel"), Alias("panel")]
        public async Task ControlPanel()
        {
            if (!_musicService.CheckMessage(Context.Message)) return;
            _musicService.ControlPanelAsync(Context.Channel);
            try
            {
                await Context.Channel.DeleteMessageAsync(Context.Message.Id);
            }
            catch (Discord.Net.HttpException)
            {

            }
        }
    }
}
