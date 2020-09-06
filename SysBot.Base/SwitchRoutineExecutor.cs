using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    /// <summary>
    /// Commands a Bot to a perform a routine asynchronously.
    /// </summary>
    public abstract class SwitchRoutineExecutor<T> where T : SwitchBotConfig
    {
        public readonly SwitchConnectionAsync Connection;
        public readonly SwitchConnectionUSB<T> ConnectionUSB;
        public readonly T Config;

        protected SwitchRoutineExecutor(T cfg)
        {
            Config = cfg;
            Connection = new SwitchConnectionAsync(cfg.IP, cfg.Port);
            ConnectionUSB = new SwitchConnectionUSB<T>(cfg);
        }

        public string LastLogged { get; private set; } = "Not Started";
        public DateTime LastTime { get; private set; } = DateTime.Now;

        protected void ReportStatus() => LastTime = DateTime.Now;

        public void Log(string message)
        {
            if (Config.ConnectionType == PokeConnectionType.WiFi)
                Connection.Log(message);
            else ConnectionUSB.LogUSB(message);
            LastLogged = message;
            LastTime = DateTime.Now;
        }

        /// <summary>
        /// Connects to the console, then runs the bot.
        /// </summary>
        /// <param name="token">Cancel this token to have the bot stop looping.</param>
        public async Task RunAsync(CancellationToken token)
        {
            Connection.Connect();
            Log("Initializing connection with console...");
            await EchoCommands(false, token).ConfigureAwait(false);
            await MainLoop(token).ConfigureAwait(false);
            Connection.Disconnect();
        }

        public async Task RunUSBAsync(CancellationToken token)
        {
            ConnectionUSB.Connect();
            Log("Initializing connection with USB device...");
            await EchoCommands(false, token).ConfigureAwait(false);
            await MainLoop(token).ConfigureAwait(false);
            ConnectionUSB.Disconnect();
        }

        protected abstract Task MainLoop(CancellationToken token);
        public abstract void SoftStop();

        public async Task Click(SwitchButton b, int delay, CancellationToken token)
        {
            if (Config.ConnectionType == PokeConnectionType.WiFi)
                await Connection.SendAsync(SwitchCommand.Click(b), token).ConfigureAwait(false); 
            else await ConnectionUSB.SendAsync(SwitchCommand.Click(b)).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task PressAndHold(SwitchButton b, int hold, int delay, CancellationToken token)
        {
            // Set hold delay
            var delaycgf = SwitchCommand.Configure(SwitchConfigureParameter.buttonClickSleepTime, hold);
            if (Config.ConnectionType == PokeConnectionType.WiFi)
                await Connection.SendAsync(delaycgf, token).ConfigureAwait(false);
            else await ConnectionUSB.SendAsync(delaycgf);
            // Press the button
            await Click(b, delay, token).ConfigureAwait(false);
            // Reset delay
            delaycgf = SwitchCommand.Configure(SwitchConfigureParameter.buttonClickSleepTime, 50); // 50 ms
            if (Config.ConnectionType == PokeConnectionType.WiFi)
                await Connection.SendAsync(delaycgf, token).ConfigureAwait(false);
            else await ConnectionUSB.SendAsync(delaycgf);
        }

        public async Task DaisyChainCommands(int Delay, SwitchButton[] buttons, CancellationToken token)
        {
            SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, Delay);
            var commands = buttons.Select(SwitchCommand.Click).ToArray();
            var chain = commands.SelectMany(x => x).ToArray();
            if (Config.ConnectionType == PokeConnectionType.WiFi)
                await Connection.SendAsync(chain, token).ConfigureAwait(false);
            else await ConnectionUSB.SendAsync(chain);
            SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, 0);
        }

        public async Task SetStick(SwitchStick stick, short x, short y, int delay, CancellationToken token)
        {
            var cmd = SwitchCommand.SetStick(stick, x, y);
            if (Config.ConnectionType == PokeConnectionType.WiFi)
                await Connection.SendAsync(cmd, token).ConfigureAwait(false);
            else await ConnectionUSB.SendAsync(cmd);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task DetachController(CancellationToken token)
        {
            if (Config.ConnectionType == PokeConnectionType.WiFi)
                await Connection.SendAsync(SwitchCommand.DetachController(), token).ConfigureAwait(false);
            else await ConnectionUSB.SendAsync(SwitchCommand.DetachController());
        }

        public async Task EchoCommands(bool value, CancellationToken token)
        {
            var cmd = SwitchCommand.Configure(SwitchConfigureParameter.echoCommands, value ? 1 : 0);
            if (Config.ConnectionType == PokeConnectionType.WiFi)
                await Connection.SendAsync(cmd, token).ConfigureAwait(false);
            else await ConnectionUSB.SendAsync(cmd);
        }
    }
}