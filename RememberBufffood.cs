using Dalamud.Plugin;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Lumina.Data.Parsing;
using ECommons;
using ECommons.Automation;
using Dalamud.Logging;
using Dalamud.Game.Text;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using System.Text.RegularExpressions;
using Lumina.Excel.GeneratedSheets;

namespace RememberBufffood
{
    public class RememberBufffood : IDalamudPlugin
    {
        public string Name => "RememberBufffood";
        public RememberBufffoodConfig PluginConfig { get; private set; }

        private bool drawConfigWindow;

        private ICommandManager _cm { get; init; }
        private IFramework _if { get; init; }
        private IPartyList _pl { get; init; }
        private IClientState _cs { get; init; }
        private IAddonLifecycle _al { get; init; }
        private IChatGui _cg { get; init; }

        private Chat chatties;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        public void Dispose()
        {
            ECommonsMain.Dispose();
            PluginInterface.UiBuilder.Draw -= this.BuildUI;
            _cg.ChatMessage -= OnChatMessage;
            PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
            PluginInterface.UiBuilder.Draw -= OnUpdate;
            RemoveCommands();
        }

        public RememberBufffood(
            [RequiredVersion("1.0")] IFramework iframe,
            [RequiredVersion("1.0")] IPartyList pl,
            [RequiredVersion("1.0")] ICommandManager cm,
            [RequiredVersion("1.0")] IChatGui cg,
            [RequiredVersion("1.0")] IAddonLifecycle al,
            [RequiredVersion("1.0")] IClientState cs
            )
        {
            _cm = cm;
            _if = iframe;
            _pl = pl;
            _cs = cs;
            _cg = cg;
            _al = al;
            al.RegisterListener(AddonEvent.PreReceiveEvent, "ScreenInfo_CountDown", ActiveTimePull);
            ECommonsMain.Init(PluginInterface, this);
            this.PluginConfig = (RememberBufffoodConfig)PluginInterface.GetPluginConfig() ?? new RememberBufffoodConfig();
            this.PluginConfig.Init(this);
            chatties = new Chat();
            _cg.ChatMessage += OnChatMessage;
            SetupCommands();
            PluginInterface.UiBuilder.Draw += this.BuildUI;
            PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
            PluginInterface.UiBuilder.Draw += OnUpdate;
        }

        private static bool AllowAnnouncement = false;

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            string messageText = message.TextValue;
            string pattern = @"Battle commencing in (\d+) seconds!";
            PluginLog.Warning("TerritoryType: " + _cs.TerritoryType);
            Match match = Regex.Match(messageText, pattern);
            if (match.Success)
            {
                string seconds = match.Groups[1].Value;
                PluginLog.Warning("Countdown detected: Battle commencing in {0} seconds!", seconds);
                AllowAnnouncement = true;
            }
        }


        public void SetupCommands()
        {
            _cm.AddHandler("/bufffood", new CommandInfo(OnConfigCommandHandler)
            {
                HelpMessage = $"Opens the config window for {this.Name}.",
                ShowInHelp = true
            });
        }

        private unsafe void ActiveTimePull(AddonEvent type, AddonArgs args)
        {
            //PluginLog.Warning("Test");
        }

        private DateTime lastExecutionTime = DateTime.MinValue;
        private const int IntervalSeconds = 180;

        private void OnUpdate()
        {
            if (_cs.LocalPlayer is null || !_cs.IsLoggedIn || !PluginConfig.Enable ||
                (_cs.TerritoryType != 1047 && // The Unending Coil of Bahamut (Ultimate)
                 _cs.TerritoryType != 1048 && // The Weapon's Refrain (Ultimate)
                 _cs.TerritoryType != 887 && // The Epic of Alexander (Ultimate) true
                 _cs.TerritoryType != 968 && // Dragonsong's Reprise (Ultimate) true
                 _cs.TerritoryType != 1051))  // The Omega Protocol (Ultimate)
            {
                return;
            }

            if (AllowAnnouncement == false) { return; }

            if ((DateTime.Now - lastExecutionTime).TotalSeconds >= IntervalSeconds)
            {
                var playersWithoutBuff = new List<string>();

                for (int i = 0; i < _pl.Length; i++)
                {
                    var player = _pl[i];
                    if (player == null) continue;

                    var buff = player.Statuses.FirstOrDefault(s => s.StatusId == 48);
                    if (buff == null || buff.RemainingTime < 300) // 15 minutes
                    {
                        playersWithoutBuff.Add(player.Name.TextValue.ToString());
                    }
                }

                if (playersWithoutBuff.Count > 0)
                {
                    string message = "Bufffood is missing or less than 5 minutes for: " + string.Join(", ", playersWithoutBuff);
                    chatties.SendMessage("/p " + message);
                }
                lastExecutionTime = DateTime.Now;
            }
            AllowAnnouncement = false;
        }

        private void OpenConfigUi()
        {
            OnConfigCommandHandler(null, null);
        }

        public void OnConfigCommandHandler(string? command, string? args)
        {
            drawConfigWindow = true;
        }

        public void RemoveCommands()
        {
            _cm.RemoveHandler("/bufffood");
        }

        private void BuildUI()
        {
            drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();
        }
    }
}