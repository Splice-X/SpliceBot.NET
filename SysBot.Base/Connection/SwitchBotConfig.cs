﻿using System.Net;

namespace SysBot.Base
{
    /// <summary>
    /// Stored config of a bot
    /// </summary>
    public abstract class SwitchBotConfig
    {
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; } = 6000;
        public PokeConnectionType ConnectionType { get; set; }
        public string DeviceAddress { get; set; } = string.Empty;

        public bool IsValidIP() => IPAddress.TryParse(IP, out _);
        public IPAddress GetAddress() => IPAddress.Parse(IP);

        public static T GetConfig<T>(string[] lines) where T : SwitchBotConfig, new()
        {
            return GetConfig<T>(lines[0], int.Parse(lines[1]), (PokeConnectionType)lines[2].IndexOf(lines[2]), lines[3]);
        }

        public static T GetConfig<T>(string ip, int port, PokeConnectionType type, string deviceAddress) where T : SwitchBotConfig, new()
        {
            var cfg = new T
            {
                IP = ip,
                Port = port,
                ConnectionType = type,
                DeviceAddress = deviceAddress,
            };
            cfg.IP = cfg.GetAddress().ToString(); // sanitize leading zeroes out for paranoia's sake
            cfg.ConnectionType = type;
            cfg.DeviceAddress = deviceAddress;
            return cfg;
        }
    }
}