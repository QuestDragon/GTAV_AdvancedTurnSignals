using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedTurnSignals
{
    public static class LogSystem
    {
        private static readonly BlockingCollection<string> logQueue = new BlockingCollection<string>();
        private const string LogPath = @"scripts\AdvancedTurnSignals\IndicatorState.Log";
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// 非同期型ログシステム
        /// </summary>
        static LogSystem()
        {
            // DISABLED
            /*
            File.Delete(LogPath); //最初に初期化
            Task.Factory.StartNew(ProcessLogQueue, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            */
        }

        /// <summary>
        /// ログの書き込み予約を実行します。
        /// </summary>
        /// <param name="s">CurrentState。</param>
        /// <param name="t">TurnSignalState。</param>
        /// <param name="message">任意の文字列。</param>
        public static void Log(IndicatorState s, IndicatorState t, string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | Current state: {s} / {t} ({message})";
            logQueue.Add(logEntry); // 自動でスレッドセーフ
        }

        private static void ProcessLogQueue()
        {
            foreach (var entry in logQueue.GetConsumingEnumerable(cts.Token))
            {
                try
                {
                    File.AppendAllText(LogPath, entry + Environment.NewLine);
                }
                catch (Exception ex) 
                {
                    // 書き込みエラーは握りつぶすか別途ログ保存
                    GTA.UI.Notification.Show($"~r~ATS Log Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// ログシステムを終了します。
        /// </summary>
        public static void Shutdown()
        {
            logQueue.CompleteAdding();
            cts.Cancel();
        }
    }
}
