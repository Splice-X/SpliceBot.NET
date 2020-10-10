using PKHeX.Core;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class TradeExtensions
    {
        public PokeTradeHub<PK8> Hub;

        public TradeExtensions(PokeTradeHub<PK8> hub)
        {
            Hub = hub;
        }

        public static List<string> EggRollCooldown = new List<string>();
        public static int[] validEgg =
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

        public static int[] regional = { 27, 37, 50, 52, 77, 79, 83, 122, 222, 263, 554, 562, 618 };

        public static int[] shinyOdds = { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                                          3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                                          3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                                          5, 5, 5, 5, 5, 5, 5, 6, 6, 6};

        public static int[] abilityIndex = { 0, 1, 2 };
        public static int[] formIndex1 = { 0, 1 };
        public static int[] formIndex2 = { 0, 1, 2 };

        public bool IsItemMule(PK8 pk8)
        {
            if (Hub.Config.Trade.ItemMuleSpecies == Species.None || Hub.Config.Trade.DittoTrade && pk8.Species == 132 || Hub.Config.Trade.EggTrade && pk8.Nickname == "Egg")
                return true;
            return !(pk8.Species != SpeciesName.GetSpeciesID(Hub.Config.Trade.ItemMuleSpecies.ToString()) || pk8.IsShiny);
        }

        public static void DittoTrade(PKM pk8)
        {
            if (pk8.IsNicknamed == false)
                return;

            var dittoStats = new string[] { "ATK", "SPE", "SPA" };
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
            pk8.ClearNickname();
            pk8.SetSuggestedHyperTrainingData();
        }

        public static void EggTrade(PK8 pk8)
        {
            pk8.IsEgg = true;
            pk8.Egg_Location = 60002;
            pk8.EggMetDate = pk8.MetDate = System.DateTime.Parse("2020/10/20");
            pk8.HeldItem = 0;
            pk8.CurrentLevel = 1;
            pk8.EXP = 0;
            pk8.DynamaxLevel = 0;
            pk8.Met_Level = 1;
            pk8.Met_Location = 0;
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
    }
}
