using Frosty.Core;
using FrostySdk.Attributes;
using FrostySdk.IO;
using DatapathFixPlugin.Actions;

namespace DatapathFixPlugin.Options {

    [DisplayName("DatapathFix Options")]
    public class LaunchOptions : OptionsExtension {
        [Category("DatapathFix")]
        [DisplayName("Enabled")]
        [Description("Enables DatapathFix")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool DatapathFixEnabled { get; set; } = true;

        [Category("DatapathFix")]
        [DisplayName("First Launch")]
        [Description("Enables window upon first launch when DatapathFix is enabled.")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool DatapathFixFirstLaunch { get; set; } = true;

        [Category("Debugging")]
        [DisplayName("Debug Mode")]
        [Description("Used for Debugging\nDo not use unless directed to by a developer/etc")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool DatapathFixDebugMode { get; set; } = false;

        public override void Load() {
            DatapathFixEnabled = Config.Get("DatapathFixEnabled", true);
            DatapathFixFirstLaunch = Config.Get("DatapathFixFirstLaunch", true);
            DatapathFixDebugMode = Config.Get("DatapathFixDebugMode", false);
        }

        public override void Save() {
            Config.Add("DatapathFixEnabled", DatapathFixEnabled);
            Config.Add("DatapathFixFirstLaunch", DatapathFixFirstLaunch);
            Config.Add("DatapathFixDebugMode", DatapathFixDebugMode);

            // Prevent LaunchPlatformPlugin from being used at the same time at DPFix
            if (DatapathFixEnabled && Config.Get("PlatformLaunchingEnabled", false, ConfigScope.Game))
                Config.Add("PlatformLaunchingEnabled", false, ConfigScope.Game);

            LaunchExecutionAction.ExtractDatapathFix();
        }
    }
}
