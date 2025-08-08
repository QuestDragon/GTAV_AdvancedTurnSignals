using GTA;
using System.Collections.Generic;
using System.Reflection;

namespace AdvancedTurnSignals
{
    public enum LangKeys
    {
        scriptname,
        versionchar,
        information,
        warning,
        error,
        loaded,
        button_warn,
        duration_warn,
        num_warn,
        overnum_warn,
        overangle_err,
        audio_warn,
        audio_err,
        default_warn,
        tips,
        reload_tips
    }

    internal class Lang
    {
        private static Dictionary<LangKeys, string> localizations = new Dictionary<LangKeys, string>()
        {
            {LangKeys.scriptname, "" },
            {LangKeys.versionchar,"" },
            {LangKeys.information, "" },
            {LangKeys.warning, "" },
            {LangKeys.error, "" },
            {LangKeys.loaded, "" },
            {LangKeys.button_warn, "" },
            {LangKeys.duration_warn, "" },
            {LangKeys.num_warn, "" },
            {LangKeys.overnum_warn, "" },
            {LangKeys.overangle_err, "" },
            {LangKeys.audio_warn, "" },
            {LangKeys.default_warn, "" },
            {LangKeys.audio_err, "" },
            {LangKeys.tips, "" },
            {LangKeys.reload_tips, "" }
        };

        /// <summary>
        /// Localization.iniを読み込みます。
        /// </summary>
        public static void init()
        {
            ScriptSettings local = ScriptSettings.Load(@"scripts\AdvancedTurnSignals\Localization.ini"); //INI File
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName(); //アセンブリ情報
                                                                               // iniのデータを読み込む (セクション、キー、デフォルト値)
            localizations[LangKeys.scriptname] = local.GetValue<string>("General", "ScriptName", assembly.Name);
            localizations[LangKeys.versionchar] = local.GetValue<string>("General", "VersionChar", "~q~v");

            localizations[LangKeys.information] = local.GetValue<string>("Level", "Information", "~b~Information");
            localizations[LangKeys.warning] = local.GetValue<string>("Level", "Warning", "~o~Warning");
            localizations[LangKeys.error] = local.GetValue<string>("Level", "Error", "~r~Error");

            localizations[LangKeys.loaded] = local.GetValue<string>("Message", "Loaded", "~g~has loaded!");
            localizations[LangKeys.button_warn] = local.GetValue<string>("Message", "ButtonWarn", "~r~The ~y~{0} ~r~controller button specification is ~h~incorrect. ~h~~n~~o~The default value ~b~({1})~o~ is used.");
            localizations[LangKeys.num_warn] = local.GetValue<string>("Message", "NumWarn", "~y~{0} ~r~in {1} is ~h~not a number. ~h~~n~~o~The default number ~b~({2}) ~o~will be used.");
            localizations[LangKeys.overnum_warn] = local.GetValue<string>("Message", "OverAngleWarn", "~o~There are settings where the steering wheel angle exceeds GTA5's maximum steering angle of ~h~40 ~h~~o~degrees.~n~The script will continue to work, ~y~but the turn signal ~h~may not turn off automatically.");
            localizations[LangKeys.overangle_err] = local.GetValue<string>("Message", "OverAngleErr", "~r~OffAngle value ~y~{0} ~r~cannot be higher than ReadyAngle value ~b~{1}~r~. ~n~~o~Auto-off feature has been ~h~disabled.");
            localizations[LangKeys.duration_warn] = local.GetValue<string>("Message", "DurationWarn", "~y~{0} ~r~in AutoOffDuration is ~h~not a number. ~h~~n~~o~The default number ~b~(1000) ~o~will be used.");
            localizations[LangKeys.audio_warn] = local.GetValue<string>("Message", "AudioNotFoundWarn", "~r~Audio file ~y~({0})~r~ not found. ~n~~o~AdvancedTurnSignals will continue to work, but ~h~no audio will play~h~ if you perform operations that use the specified audio file.");
            localizations[LangKeys.default_warn] = local.GetValue<string>("Message", "UseDefaultAudioWarn", "~r~Audio file ~y~({0})~r~ not found. ~n~~o~AdvancedTurnSignals plays default sounds when possible.");
            localizations[LangKeys.audio_err] = local.GetValue<string>("Message", "AudioLoadErr", "~r~Failed to retrieve audio file. ~n~~o~UseSound feature has been ~h~disabled.");

            localizations[LangKeys.tips] = local.GetValue<string>("Tips", "Tips", "~b~Tip");
            localizations[LangKeys.reload_tips] = local.GetValue<string>("Tips", "Reload", "If you want to ~o~reload the audio file~s~, press the ~b~ReloadKey~s~ specified in ~y~ScriptHookVDotNet.ini~s~ to reload the script.");
        }

        public static string GetLangData(LangKeys key)
        {
            return localizations[key];
        }
    }
}
