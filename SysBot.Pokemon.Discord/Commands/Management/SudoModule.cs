﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class SudoModule : ModuleBase<SocketCommandContext>
    {
        [Command("blacklist")]
        [Summary("Blacklists mentioned user.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task BlackListUsers([Remainder] string _)
        {
            await Process(Context.Message.MentionedUsers.Select(z => z.Id), (z, x) => z.Add(x), z => z.BlacklistedUsers).ConfigureAwait(false);
        }

        [Command("unblacklist")]
        [Summary("Un-Blacklists mentioned user.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task UnBlackListUsers([Remainder] string _)
        {
            await Process(Context.Message.MentionedUsers.Select(z => z.Id), (z, x) => z.Remove(x), z => z.BlacklistedUsers).ConfigureAwait(false);
        }

        [Command("blacklistId")]
        [Summary("Blacklists IDs. (Useful if user is not in the server).")]
        [RequireSudo]
        public async Task BlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
        {
            await Process(GetIDs(content), (z, x) => z.Add(x), z => z.BlacklistedUsers).ConfigureAwait(false);
        }

        [Command("unBlacklistId")]
        [Summary("Un-Blacklists IDs. (Useful if user is not in the server).")]
        [RequireSudo]
        public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
        {
            await Process(GetIDs(content), (z, x) => z.Remove(x), z => z.BlacklistedUsers).ConfigureAwait(false);
        }

        [Command("clearcooldown")]
        [Alias("cc")]
        [Summary("Clears EggRaffle cooldown of the mentioned user.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task ClearCooldown([Remainder] string id)
        {
            if (!System.IO.File.Exists("EggRngBlacklist.txt"))
                System.IO.File.Create("EggRngBlacklist.txt").Close();

            System.IO.StreamReader reader = new System.IO.StreamReader("EggRngBlacklist.txt");
            var content = reader.ReadToEnd();
            reader.Close();

            id = System.Text.RegularExpressions.Regex.Match(id, @"\D*(\d*)", System.Text.RegularExpressions.RegexOptions.Multiline).Groups[1].Value;
            var parse = System.Text.RegularExpressions.Regex.Match(content, id + @" - (\S*\ \S*\ \w*)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (content.Contains(id))
            {
                content = content.Replace(parse.Groups[1].Value, "1/11/2000 12:00:00 AM").TrimEnd();
                System.IO.StreamWriter writer = new System.IO.StreamWriter("EggRngBlacklist.txt");
                writer.WriteLine(content);
                writer.Close();
                await ReplyAsync("Done.").ConfigureAwait(false);
            }
            else await ReplyAsync("User with that ID not found.").ConfigureAwait(false);
        }

        protected async Task Process(IEnumerable<ulong> values, Func<SensitiveSet<ulong>, ulong, bool> process, Func<DiscordManager, SensitiveSet<ulong>> fetch)
        {
            var mgr = SysCordInstance.Manager;
            var list = fetch(SysCordInstance.Manager);
            var any = false;
            foreach (var v in values)
                any |= process(list, v);

            if (!any)
            {
                await ReplyAsync("Failed.").ConfigureAwait(false);
                return;
            }

            mgr.Write();
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        protected static IEnumerable<ulong> GetIDs(string content)
        {
            return content.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
        }
    }
}