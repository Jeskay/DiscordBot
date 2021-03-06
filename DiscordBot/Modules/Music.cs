﻿using System;
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
        private readonly MusicService _musicService;
        private readonly BackDoorService _backDoorService;
        public Music(MusicService musicService, BackDoorService backDoorService)
        {
            _musicService = musicService;
            _backDoorService = backDoorService;
        }
        [Command("Help")]
        public async Task Help()
        {
            if (!_backDoorService.CheckMessage(Context.Message)) return;
            await _musicService.PrintHelp(Context.Channel);
            await Context.Channel.DeleteMessageAsync(Context.Message.Id);
        }
        [Command("Play"), Alias("p")]
        public async Task Play([Remainder] string query)
        {
            if (!_backDoorService.CheckMessage(Context.Message)) return;
            try
            {
                await Context.Channel.DeleteMessageAsync(Context.Message.Id);
            }
            catch (Discord.Net.HttpException)
            {

            }
            if (!await _musicService.JoinAsync(Context.User as SocketGuildUser, Context.Channel)) return;

            await _musicService.PrintTracks(await _musicService.SearchAsync(query), Context.User as SocketGuildUser, Context.Channel);
        }
        [Command("q"), Alias("queue")]
        public async Task Queue()
        {
            if (!_backDoorService.CheckMessage(Context.Message)) return;
            try 
            {
                await _musicService.PrintQueue(Context.Guild, Context.Channel);
                await Context.Channel.DeleteMessageAsync(Context.Message.Id);

            }
            catch (Exception)
            { 
            
            }
        }

        [Command("Skip"), Alias("s")]
        public async Task Skip()
        {
            if (!_backDoorService.CheckMessage(Context.Message)) return;
            await _musicService.SkipAsync(Context.Channel, Context.User as SocketGuildUser);
        }
        [Command("ControlPanel"), Alias("panel")]
        public async Task ControlPanel()
        {
            if (!_backDoorService.CheckMessage(Context.Message)) return;
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
