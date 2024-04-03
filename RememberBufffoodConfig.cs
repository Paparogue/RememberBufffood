using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using Dalamud.Configuration;
using ImGuiNET;

namespace RememberBufffood
{
    public class RememberBufffoodConfig : IPluginConfiguration
    {
        public int Version { get; set; }

        [NonSerialized] private RememberBufffood plugin;

        // Configuration properties
        public bool Enable = false;

        // Load default configuration values
        public void LoadDefaults()
        {
            foreach (var f in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                f.SetValue(this, f.GetValue(plugin));
            }
        }

        // Initialize the plugin
        public void Init(RememberBufffood plugin)
        {
            this.plugin = plugin;
        }

        // Save the configuration
        public void Save()
        {
            RememberBufffood.PluginInterface.SavePluginConfig(this);
        }

        // Draw the configuration user interface
        public bool DrawConfigUI()
        {
            var drawConfig = true;
            var windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;
            ImGui.Begin($"{plugin.Name} UI", ref drawConfig, windowFlags);
            var changed = false;

            changed |= ImGui.Checkbox("Enable", ref Enable);
            ImGui.SameLine();
            ImGui.TextDisabled("Enable or Disable Bufffood Reminder");
            ImGui.Separator();

            if (ImGui.Button("Reset"))
            {
                LoadDefaults();
                changed = true;
            }

            if (changed)
            {
                Save();
            }

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF5E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF5E5BAA);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF5E5BDD);
            ImGui.PopStyleColor(3);

            ImGui.End();

            return drawConfig;
        }
    }
}