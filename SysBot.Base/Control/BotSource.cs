﻿using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    public class BotSource<T> where T : SwitchBotConfig
    {
        public readonly SwitchRoutineExecutor<T> Bot;
        private CancellationTokenSource Source = new CancellationTokenSource();

        public BotSource(SwitchRoutineExecutor<T> bot) => Bot = bot;

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        public void Stop()
        {
            if (!IsRunning)
                return;

            Source.Cancel();
            Source = new CancellationTokenSource();

            // Detach Controllers
            Task.Run(() => Bot.Config.ConnectionType == PokeConnectionType.WiFi ? Bot.Connection.SendAsync(SwitchCommand.DetachController(), CancellationToken.None) : Bot.ConnectionUSB.SendAsync(SwitchCommand.DetachController()));
            IsPaused = IsRunning = false;
        }

        public void Pause()
        {
            IsPaused = true;
            Bot.SoftStop();
        }

        public void Start()
        {
            if (IsPaused)
                Stop(); // can't soft-resume; just re-launch

            if (IsRunning)
                return;

            Task.Run(() => Bot.Config.ConnectionType == PokeConnectionType.WiFi ? Bot.RunAsync(Source.Token) : Bot.RunUSBAsync(Source.Token)
                .ContinueWith(ReportFailure, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously)
                .ContinueWith(_ => IsRunning = false));

            IsRunning = true;
        }

        private void ReportFailure(Task finishedTask)
        {
            var ident = Bot.Connection.Name;
            var ae = finishedTask.Exception;
            if (ae == null)
            {
                LogUtil.LogError("Bot has stopped without error.", ident);
                return;
            }

            LogUtil.LogError("Bot has crashed!", ident);

            if (!string.IsNullOrEmpty(ae.Message))
                LogUtil.LogError("Aggregate message:" + ae.Message, ident);

            var st = ae.StackTrace;
            if (!string.IsNullOrEmpty(st))
                LogUtil.LogError("Aggregate stacktrace:" + st, ident);

            foreach (var e in ae.InnerExceptions)
            {
                if (!string.IsNullOrEmpty(e.Message))
                    LogUtil.LogError("Inner message:" + e.Message, ident);
                LogUtil.LogError("Inner stacktrace:" + e.StackTrace, ident);
            }
        }

        public void Resume()
        {
            Start();
        }
    }
}