using System;
using Torch.Commands;
using Torch.Commands.Permissions;
using Utils.General;
using Utils.Torch;
using VRage.Game.ModAPI;
using VRageMath;

namespace InfluxDb.Torch
{
    [Category("influxdb")]
    public sealed class TorchInfluxDbCommandModule : CommandModule
    {
        TorchInfluxDbPlugin Plugin => (TorchInfluxDbPlugin) Context.Plugin;

        [Command("configs", "List all configurable options")]
        [Permission(MyPromoteLevel.Admin)]
        public void Configs()
        {
            this.GetOrSetProperty(Plugin.Config);
        }

        [Command("commands", "List all commands")]
        [Permission(MyPromoteLevel.Admin)]
        public void Commands()
        {
            this.ShowCommands();
        }

        [Command("reload", "Reload config from the disk")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            Plugin.ReloadConfig();
        }

        [Command("w", "Tries to write a raw line to the integrated InfluxDB instance.")]
        [Permission(MyPromoteLevel.Admin)]
        public void WriteLine() => this.CatchAndReport(async () =>
        {
            var line = Context.RawArgs;
            line.ThrowIfNullOrEmpty(nameof(line));

            Context.Respond($"Writing line: '{line}'");

            try
            {
                await TorchInfluxDbWriter.WriteLineAsync(line);
            }
            catch (Exception e)
            {
                Context.Respond($"{e.Message}", Color.Red);
                return;
            }

            Context.Respond("Done writing line");
        });
    }
}