using PKHeX.Core;
using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class TradeSettings
    {
        private const string TradeCode = nameof(TradeCode);
        private const string TradeConfig = nameof(TradeConfig);
        private const string Dumping = nameof(Dumping);
        public override string ToString() => "Trade Bot Settings";

        [Category(TradeConfig), Description("Time to wait for a trade partner in seconds.")]
        public int TradeWaitTime { get; set; } = 45;

        [Category(TradeCode), Description("Minimum Link Code.")]
        public int MinTradeCode { get; set; } = 8180;

        [Category(TradeCode), Description("Maximum Link Code.")]
        public int MaxTradeCode { get; set; } = 8199;

        [Category(Dumping), Description("Link Trade: Dumping routine will stop after a maximum number of dumps from a single user.")]
        public int MaxDumpsPerTrade { get; set; } = 20;

        [Category(Dumping), Description("Link Trade: Dumping routine will stop after spending x seconds in trade.")]
        public int MaxDumpTradeTime { get; set; } = 180;

        [Category(TradeCode), Description("Link Trade: Will restrict trading to a single non-shiny species. Useful for item trades in servers (such as raid servers) that don't want full-on genning.")]
        public Species ItemMuleSpecies { get; set; } = Species.None;

        [Category(TradeCode), Description("Custom message to display if a non-ItemMule species is requested via $trade.")]
        public string ItemMuleCustomMessage { get; set; } = string.Empty;

        [Category(TradeCode), Description("Toggle Ditto trades for breeding. Can be used with \"ItemMule\".")]
        public bool DittoTrade { get; set; } = false;

        [Category(TradeCode), Description("Toggle Egg trades. Can be used with \"ItemMule\".")]
        public bool EggTrade { get; set; } = false;

        [Category(TradeCode), Description("Silly, useless feature to post a meme if someone requests an illegal item for \"ItemMule\".")]
        public bool Memes { get; set; } = false;

        [Category(TradeCode), Description("Enter either meme website links or file names with extensions. Six memes (in order; Cherish Ball, Park Ball, Dynamax Crystals, Shiny, Legendary Eggs, off-species). I.e. file1.png,file2.jpg, etc.")]
        public string MemeFileNames { get; set; } = string.Empty;

        [Category(TradeCode), Description("Enable randomly generated egg trades via $roll. Silliest feature to date.")]
        public bool EggRaffle { get; set; } = false;

        [Category(TradeCode), Description("Enter Channel ID(s) where $roll should be active (if used for raffle chats and discussions), or leave blank to work everywhere.")]
        public string EggRaffleChannels { get; set; } = string.Empty;

        [Category(TradeCode), Description("Enter the amount of time in hours until a user can requeue.")]
        public double EggRaffleCooldown { get; set; } = 3;

        /// <summary>
        /// Gets a random trade code based on the range settings.
        /// </summary>
        public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);
    }
}
