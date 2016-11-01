﻿using Discord;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IA.SDK
{
    class RuntimeChannel : DiscordChannel
    {
        IChannel channel;

        public RuntimeChannel(IChannel c)
        {
            channel = c;
        }

        public override ulong Id
        {
            get
            {
                return channel.Id;
            }
        }

        public override async Task SendFileAsync(string path)
        {
            await (channel as IMessageChannel).SendFileAsync(path);
        }

        public override Task SendFileAsync(MemoryStream stream, string extension)
        {
            throw new NotImplementedException();
        }

        public override async Task<DiscordMessage> SendMessage(string message)
        {
            RuntimeMessage m = new RuntimeMessage(await (channel as IMessageChannel).SendMessage(message));
            return m;
        }
    }
}
