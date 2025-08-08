using System;
using System.Threading.Tasks;
using GTA;
using GTA.UI;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace AdvancedTurnSignals
{

    public class MainScript : Script
    {
        private static SignalController ScriptController;
        public static  GameInputManager inputManager;
        private CancellationTokenSource PauseTickCanceller;

        private float ready_angle;
        private float off_angle;

        public MainScript()
        {
            Lang.init();
            Utils.SettingsLoad();
            if (!Utils.GetSettings(BoolSettingsItem.ScriptEnabled)) return; //スクリプト無効の場合は終了

            ScriptController = new SignalController();
            inputManager = new GameInputManager(ScriptController);
            PauseTickCanceller = new CancellationTokenSource();

            Tick += OnTick;
            KeyDown += inputManager.OnKeyDown;
            KeyUp += inputManager.OnKeyUp;
            Aborted += Aborter;

            ready_angle = Utils.GetSettings(FloatSettingsItem.ReadyAngleValue);
            off_angle = Utils.GetSettings(FloatSettingsItem.OffAngleValue);

            Task.Run(() => { Utils.pause_tick(PauseTickCanceller.Token); }); //非同期バックグラウンド実行

            Utils.ShowNotification(NotificationIcon.LsCustoms, NotificationClass.information, Lang.GetLangData(LangKeys.loaded));
            Debug.WriteLine("Advanced Turn Signals is READY!");
            
#if DEBUG
            Notification.Show("Advanced Turn Signals is READY!");
#endif
        }

        // スクリプトが停止された際の処理
        private async void Aborter(object sender, EventArgs e)
        {
            LogSystem.Shutdown();
            PauseTickCanceller?.Cancel();
            await ScriptController.Deactivate();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (Utils.game_pause)
            {
                //押しっぱなし判定の対策としてポーズ画面から解除したらリセット
                inputManager.ActiveKeys.Clear(); 
                inputManager.ActiveControls.Clear();
                Utils.pause_interlock = false; //インターロック解除

                // Notification.Show("~r~ActiveKeyControl has been Cleared!");
            }

            Utils.lastTickTime = DateTime.Now; //ゲーム稼働中を知らせる

            ScriptController.Update();

#if DEBUG
            string Aoil = "";
            string ss = "";
            if (ScriptController.AutoOffInterlock) Aoil = "~r~[LOCK]~s~";

            /*
            string kk = "";
            foreach (Keys k in inputManager.ActiveKeys)
            {
                kk += k + ", ";
            }

            ss += "ActiveKeys: [" + kk + "]";
            */
            ss += $"\nKeys: {inputManager.ActiveKeys.Count} / Controls: {inputManager.ActiveControls.Count} {Aoil}";
            // ss += $"\nisPause: {Utils.game_pause} / Cancel: {PauseTickCanceller.Token.IsCancellationRequested}";
            Vehicle cv = Utils.GetCurrentVehicle();
            if (cv != null)
            {
                string AutoOffOKstr = "";
                if (ScriptController.Off_OK) AutoOffOKstr = "[OFF]";
                ss += $"\nState: {ScriptController.currentState} / {ScriptController.TurnSignalState}";
                ss += $"\nAngle: {Math.Floor(cv.SteeringAngle * 100) / 100} ({ready_angle} -> {off_angle} {AutoOffOKstr})";
            }
            GTA.UI.Screen.ShowSubtitle(ss);
#endif
        }

        public static void SoundMuteToSignalController()
        {
            ScriptController.SoundMute();
        }
    }
}