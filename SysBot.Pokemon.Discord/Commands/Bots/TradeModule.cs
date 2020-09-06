using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Link Code trades")]
    public class TradeModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("tradeList")]
        [Alias("tl")]
        [Summary("Prints the users in the trade queues.")]
        [RequireSudo]
        public async Task GetTradeListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("eggList")]
        [Alias("el")]
        [Summary("Prints the users in the EggRoll queue.")]
        [RequireSudo]
        public async Task GetEggRollListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.EggRoll);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending EggRoll Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the provided Pokémon file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttach([Summary("Trade Code")] int code)
        {
            var sudo = Context.User.GetIsSudo();

            var attachment = Context.Message.Attachments.FirstOrDefault();
            if (attachment == default)
            {
                await ReplyAsync("No attachment provided!").ConfigureAwait(false);
                return;
            }

            var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
            if (!att.Success || !(att.Data is PK8 pk8))
            {
                await ReplyAsync("No PK8 attachment provided!").ConfigureAwait(false);
                return;
            }

            await AddTradeToQueueAsync(code, Context.User.Username, pk8, sudo).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
        {
            const int gen = 8;
            content = ReusableActions.StripCodeBlock(content);
            SpecifyOT(content, out string specifyOT);
            if (specifyOT != string.Empty)
                content = System.Text.RegularExpressions.Regex.Replace(content, @"OT:(\S*\s?\S*\s?\S*)?$\W?", "", System.Text.RegularExpressions.RegexOptions.Multiline);

            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);

            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            var sav = AutoLegalityWrapper.GetTrainerInfo(gen);
            var pkm = sav.GetLegal(template, out _);
            if (specifyOT != string.Empty)
                pkm.OT_Name = specifyOT;

            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[template.Species];
            var invalid = !(pkm is PK8) || (!la.Valid && SysCordInstance.Self.Hub.Config.Legality.VerifyLegality);
            if (invalid && !Info.Hub.Config.Trade.Memes)
            {
                var imsg = $"Oops! I wasn't able to create something from that. Here's my best attempt for that {spec}!";
                await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
                return;
            }
            else if (Info.Hub.Config.Trade.Memes)
            {
                if (await TrollAsync(invalid, template).ConfigureAwait(false))
                    return;
            }

            pkm.ResetPartyStats();
            var sudo = Context.User.GetIsSudo();
            await AddTradeToQueueAsync(code, Context.User.Username, (PK8)pkm, sudo).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Showdown Set")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsync(code, content).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the attached file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttach()
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncAttach(code).ConfigureAwait(false);
        }

        [Command("eggroll")]
        [Alias("roll", "r")]
        [Summary("Makes the bot trade you a randomly generated egg.")]
        [RequireQueueRole(nameof(DiscordManager.RolesEggRoll))]
        public async Task EggRollAsync()
        {
            if (!Info.Hub.Config.Trade.EggRollChannels.Contains(Context.Channel.Id.ToString()) && !Info.Hub.Config.Trade.EggRollChannels.Equals(""))
            {
                await ReplyAsync($"You're typing the command in the wrong channel!").ConfigureAwait(false);
                return;
            }

            if (Info.Hub.Config.Trade.EggRollCooldown < 0)
                Info.Hub.Config.Trade.EggRollCooldown = default;

            if (!System.IO.File.Exists("EggRollCooldown.txt"))
                System.IO.File.Create("EggRollCooldown.txt").Close();

            System.IO.StreamReader reader = new System.IO.StreamReader("EggRollCooldown.txt");
            var content = reader.ReadToEnd();
            reader.Close();

            var id = $"{Context.User.Id}";
            var parse = System.Text.RegularExpressions.Regex.Match(content, @"(" + id + @") - (.*$)", System.Text.RegularExpressions.RegexOptions.Multiline);
            System.DateTime.TryParse(parse.Groups[2].Value, out System.DateTime time);
            var timer = time.AddHours(Info.Hub.Config.Trade.EggRollCooldown);
            var timeRemaining = timer - System.DateTime.Now;

            if (content.Contains(id) && System.DateTime.Now < timer)
            {
                await ReplyAsync($"{Context.User.Mention}, please try again in {timeRemaining.Hours:N0}h : {timeRemaining.Minutes:N0}m : {timeRemaining.Seconds:N0}s!").ConfigureAwait(false);
                return;
            }

            var code = Info.GetRandomTradeCode();
            int[] validEgg = 
                { 1, 4, 7, 10, 27, 37, 43, 50, 52, 54, 58, 60, 63, 66, 72,
                  77, 79, 81, 83, 90, 92, 95, 98, 102, 104, 108, 109, 111, 114, 115,
                  116, 118, 120, 122, 123, 127, 128, 129, 131, 133, 137, 163, 170, 172, 173,
                  174, 175, 177, 194, 206, 211, 213, 214, 215, 220, 222, 223, 225, 227, 236,
                  241, 246, 263, 270, 273, 278, 280, 290, 293, 298, 302, 303, 309, 318, 320,
                  324, 328, 337, 338, 339, 341, 343, 349, 355, 360, 361, 403, 406, 415, 420,
                  422, 425, 427, 434, 436, 438, 439, 440, 446, 447, 449, 451, 453, 458, 459,
                  479, 506, 509, 517, 519, 524, 527, 529, 532, 535, 538, 539, 543, 546, 548,
                  550, 551, 554, 556, 557, 559, 561, 562, 568, 570, 572, 574, 577, 582, 587,
                  588, 590, 592, 595, 597, 599, 605, 607, 610, 613, 616, 618, 619, 621, 622,
                  624, 626, 627, 629, 631, 632, 633, 636, 659, 661, 674, 677, 679, 682, 684,
                  686, 688, 690, 692, 694, 701, 702, 704, 707, 708, 710, 712, 714, 722, 725,
                  728, 736, 742, 744, 746, 747, 749, 751, 753, 755, 757, 759, 761, 764, 765,
                  766, 767, 769, 771, 776, 777, 778, 780, 781, 782, 810, 813, 816, 819, 821,
                  824, 827, 829, 831, 833, 835, 837, 840, 843, 845, 846, 848, 850, 852, 854,
                  856, 859, 868, 870, 871, 872, 874, 875, 876, 877, 878, 884, 885 };

            int[] regional = { 27, 37, 50, 52, 77, 79, 83, 122, 222, 263, 554, 562, 618 };

            int[] shinyOdds = { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                                3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                                3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                                5, 5, 5, 5, 5, 5, 5, 6, 6, 6};

            int[] abilityIndex = { 0, 1, 2 };
            int[] formIndex1 = { 0, 1 };
            int[] formIndex2 = { 0, 1, 2 };

            var rng = new System.Random();
            int shinyRng = rng.Next(0, shinyOdds.Length);
            int abilityRng = rng.Next(0, abilityIndex.Length);
            var set = new ShowdownSet($"Egg({SpeciesName.GetSpeciesName(rng.Next(new Zukan8Index(Zukan8Type.None, 1).Index, GameUtil.GetMaxSpeciesID(GameVersion.SWSH)), 2)})");
            while (!validEgg.ToList().Contains(set.Species))
                set = new ShowdownSet($"Egg({SpeciesName.GetSpeciesName(rng.Next(new Zukan8Index(Zukan8Type.None, 1).Index, GameUtil.GetMaxSpeciesID(GameVersion.SWSH)), 2)})");

            var template = AutoLegalityWrapper.GetTemplate(set);
            var sav = AutoLegalityWrapper.GetTrainerInfo(8);
            var pkm = (PK8)sav.GetLegal(template, out _);

            if (regional.ToList().Contains(pkm.Species))
            {
                int formRng = rng.Next(0, formIndex1.Length);
                int formRng2 = rng.Next(0, formIndex2.Length);

                if (pkm.Species != 52)
                    pkm.SetAltForm(formIndex1[formRng]);
                else pkm.SetAltForm(formIndex2[formRng2]);

                if (pkm.AltForm != 0)
                {
                    if (pkm.Species == 27)
                        pkm.RelearnMove3 = 10;
                    else if (pkm.Species == 37)
                        pkm.RelearnMove4 = 39;
                    else if (pkm.Species == 52)
                    {
                        pkm.RelearnMove2 = 252;
                        pkm.RelearnMove3 = 45;
                    }
                    else if (pkm.Species == 83)
                    {
                        pkm.RelearnMove1 = 64;
                        pkm.RelearnMove4 = 28;
                    }
                    else if (pkm.Species == 222)
                        pkm.RelearnMove1 = 33;
                    else if (pkm.Species == 263)
                    {
                        pkm.RelearnMove2 = 43;
                        pkm.RelearnMove3 = 0;
                        pkm.RelearnMove4 = 0;
                    }
                }
            }

            EggTrade(pkm);
            pkm.Nature = rng.Next(0, 24);
            pkm.StatNature = pkm.Nature;
            pkm.SetAbilityIndex(abilityIndex[abilityRng]);
            pkm.IVs = pkm.SetRandomIVs(3);
            BallApplicator.ApplyBallLegalRandom(pkm);

            if (shinyOdds[shinyRng] == 3)
            {
                CommonEdits.SetShiny(pkm, Shiny.Never);
                pkm.SetUnshiny();
            }
            else if (shinyOdds[shinyRng] == 5)
                CommonEdits.SetShiny(pkm, Shiny.AlwaysStar);
            else CommonEdits.SetShiny(pkm, Shiny.AlwaysSquare);

            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[template.Species];
            var invalid = !(pkm is PK8) || (!la.Valid && SysCordInstance.Self.Hub.Config.Legality.VerifyLegality);
            if (invalid)
            {
                var imsg = $"Oops! I scrambled your egg! Don't tell Ramsay! Here's my best attempt for that {spec}!";
                await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
                return;
            }

            pkm.ResetPartyStats();
            var sudo = Context.User.GetIsSudo();
            await Context.AddToQueueAsync(code, Context.User.Username, sudo, pkm, PokeRoutineType.EggRoll, PokeTradeType.EggRoll).ConfigureAwait(false);
        }

        private async Task AddTradeToQueueAsync(int code, string trainerName, PK8 pk8, bool sudo)
        {
            if (!pk8.CanBeTraded() || !IsItemMule(pk8))
            {
                await ReplyAsync($"{(Info.Hub.Config.Trade.ItemMuleCustomMessage == string.Empty || IsItemMule(pk8) ? "Provided Pokémon content is blocked from trading!" : Info.Hub.Config.Trade.ItemMuleCustomMessage)}").ConfigureAwait(false);
                return;
            }

            if (Info.Hub.Config.Trade.DittoTrade && pk8.Species == 132)
                DittoTrade(pk8);

            if (Info.Hub.Config.Trade.EggTrade && pk8.Nickname == "Egg")
                EggTrade(pk8);

            var la = new LegalityAnalysis(pk8);
            if (!la.Valid && SysCordInstance.Self.Hub.Config.Legality.VerifyLegality)
            {
                await ReplyAsync("PK8 attachment is not legal, and cannot be traded!").ConfigureAwait(false);
                return;
            }

            await Context.AddToQueueAsync(code, trainerName, sudo, pk8, PokeRoutineType.LinkTrade, PokeTradeType.Specific).ConfigureAwait(false);
        }

        private bool IsItemMule(PK8 pk8)
        {
            if (Info.Hub.Config.Trade.ItemMuleSpecies == Species.None || Info.Hub.Config.Trade.DittoTrade && pk8.Species == 132 || Info.Hub.Config.Trade.EggTrade && pk8.Nickname == "Egg")
                return true;
            return !(pk8.Species != SpeciesName.GetSpeciesID(Info.Hub.Config.Trade.ItemMuleSpecies.ToString()) || pk8.IsShiny);
        }

        private async Task<bool> TrollAsync(bool invalid, IBattleTemplate set)
        {
            var rng = new System.Random();
            var path = Info.Hub.Config.Trade.MemeFileNames.Split(',');
            var msg = $"Oops! I wasn't able to create that {GameInfo.Strings.Species[set.Species]}. Here's a meme instead!\n";

            if (path.Length == 0)
                path = new string[] { "https://i.imgur.com/qaCwr09.png" }; //If memes enabled but none provided, use a default one.

            if (invalid || !ItemRestrictions.IsHeldItemAllowed(set.HeldItem, 8) || (Info.Hub.Config.Trade.ItemMuleSpecies != Species.None && set.Shiny) || Info.Hub.Config.Trade.EggTrade && set.Nickname == "Egg" && set.Species >= 888
                || (Info.Hub.Config.Trade.ItemMuleSpecies != Species.None && GameInfo.Strings.Species[set.Species] != Info.Hub.Config.Trade.ItemMuleSpecies.ToString() && !(Info.Hub.Config.Trade.DittoTrade && set.Species == 132 || Info.Hub.Config.Trade.EggTrade && set.Nickname == "Egg" && set.Species < 888)))
            {
                if (Info.Hub.Config.Trade.MemeFileNames.Contains(".com") || path.Length == 0)
                    _ = invalid == true ? await Context.Channel.SendMessageAsync($"{msg}{path[rng.Next(path.Length)]}").ConfigureAwait(false) : await Context.Channel.SendMessageAsync($"{path[rng.Next(path.Length)]}").ConfigureAwait(false);
                else _ = invalid == true ? await Context.Channel.SendMessageAsync($"{msg}{path[rng.Next(path.Length)]}").ConfigureAwait(false) : await Context.Channel.SendMessageAsync($"{path[rng.Next(path.Length)]}").ConfigureAwait(false);
                return true;
            }
            return false;
        }

        public static void DittoTrade(PKM pk8)
        {
            if (pk8.IsNicknamed == false)
                return;

            var dittoLang = new string[] { "JPN", "ENG", "FRE", "ITA", "GER", "ESP", "KOR", "CHS", "CHT" };
            var dittoStats = new string[] { "ATK", "SPE", "SPA" };

            if (pk8.Nickname.Contains(dittoLang[0]))
                pk8.Language = (int)LanguageID.Japanese;
            else if (pk8.Nickname.Contains(dittoLang[1]))
                pk8.Language = (int)LanguageID.English;
            else if (pk8.Nickname.Contains(dittoLang[2]))
                pk8.Language = (int)LanguageID.French;
            else if (pk8.Nickname.Contains(dittoLang[3]))
                pk8.Language = (int)LanguageID.Italian;
            else if (pk8.Nickname.Contains(dittoLang[4]))
                pk8.Language = (int)LanguageID.German;
            else if (pk8.Nickname.Contains(dittoLang[5]))
                pk8.Language = (int)LanguageID.Spanish;
            else if (pk8.Nickname.Contains(dittoLang[6]))
                pk8.Language = (int)LanguageID.Korean;
            else if (pk8.Nickname.Contains(dittoLang[7]))
                pk8.Language = (int)LanguageID.ChineseS;
            else if (pk8.Nickname.Contains(dittoLang[8]))
                pk8.Language = (int)LanguageID.ChineseT;

            pk8.MetDate = System.DateTime.Parse("2020/10/20");
            pk8.StatNature = pk8.Nature;
            pk8.SetAbility(7);
            pk8.SetAbilityIndex(1);
            pk8.Met_Level = 60;
            pk8.Move1 = 144;
            pk8.Move1_PP = 0;
            pk8.Met_Location = 154;
            pk8.Ball = 21;
            pk8.IVs = new int[] { 31, pk8.Nickname.Contains(dittoStats[0]) ? 0 : 31, 31, pk8.Nickname.Contains(dittoStats[1]) ? 0 : 31, pk8.Nickname.Contains(dittoStats[2]) ? 0 : 31, 31 };
            pk8.SetSuggestedHyperTrainingData();
        }

        public static void EggTrade(PK8 pk8)
        {
            pk8.IsEgg = true;
            pk8.Egg_Location = 60002;
            pk8.EggMetDate = System.DateTime.Parse("2020/10/20");
            pk8.HeldItem = 0;
            pk8.CurrentLevel = 1;
            pk8.EXP = 0;
            pk8.DynamaxLevel = 0;
            pk8.Met_Level = 1;
            pk8.Met_Location = 0;
            pk8.MetDate = System.DateTime.Parse("2020/10/20");
            pk8.CurrentHandler = 0;
            pk8.OT_Friendship = 1;
            pk8.HT_Name = "";
            pk8.HT_Friendship = 0;
            pk8.HT_Language = 0;
            pk8.HT_Gender = 0;
            pk8.HT_Memory = 0;
            pk8.HT_Feeling = 0;
            pk8.HT_Intensity = 0;
            pk8.EVs = new int[] { 0, 0, 0, 0, 0, 0 };
            pk8.Markings = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            pk8.ClearRecordFlags();
            pk8.GetSuggestedRelearnMoves();
            pk8.Moves = pk8.RelearnMoves;
            pk8.Move1_PPUps = pk8.Move2_PPUps = pk8.Move3_PPUps = pk8.Move4_PPUps = 0;
            pk8.SetMaximumPPCurrent(pk8.Moves);
            pk8.SetSuggestedHyperTrainingData();
        }

        public static string SpecifyOT(string content, out string specifyOT)
        {
            if (!content.Contains("OT: "))
                return specifyOT = string.Empty;

            return specifyOT = System.Text.RegularExpressions.Regex.Match(content, @"OT:(.*)?$\W?", System.Text.RegularExpressions.RegexOptions.Multiline).Groups[1].Value.Trim();
        }
    }
}
