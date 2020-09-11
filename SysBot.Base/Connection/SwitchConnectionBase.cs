using System.Net.Sockets;

namespace SysBot.Base
{
    public abstract class SwitchConnectionBase
    {
        public Socket Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
        public SwitchConnectionUSB ConnectionUSB;
        public SwitchBotConfig Config;
        public readonly string IP;
        public readonly int Port;
        public readonly string UsbPortIndex;

        public string Name { get; set; }
        public bool Connected { get; protected set; }
        public bool ConnectedUSB { get; protected set; }

        public void Log(string message) => LogUtil.LogInfo(message, Name);

        protected SwitchConnectionBase(string ipaddress, int port, SwitchBotConfig cfg)
        {
            IP = ipaddress;
            Port = port;
            UsbPortIndex = cfg.UsbPortIndex;
            ConnectionUSB = new SwitchConnectionUSB();
            Config = cfg;
            Name = $"{IP}{(cfg.ConnectionType == ConnectionType.USB ? " [#"+UsbPortIndex+"]" : string.Empty)}: {GetType().Name}";
            Log("Connection details created!");
        }
    }
}
