using System;
using System.Windows.Controls;
using InfluxDb.Client;
using InfluxDb.Client.V18;
using InfluxDb.Client.Write;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.API.Session;
using Utils.Torch;

namespace InfluxDb.Torch
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class TorchInfluxDbPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchInfluxDbConfig> _config;
        UserControl _userControl;

        IInfluxDbWriteEndpoints _endpoints;
        ThrottledInfluxDbWriteClient _throttledWriteClient;
        FileLoggingConfigurator _loggingConfigurator;

        public TorchInfluxDbConfig Config => _config.Data;
        public UserControl GetControl() => _config.GetOrCreateUserControl(ref _userControl);

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.OnSessionStateChanged(TorchSessionState.Loaded, OnGameLoaded);
            this.OnSessionStateChanged(TorchSessionState.Unloading, OnGameUnloading);

            var configFilePath = this.MakeFilePath("TorchInfluxDbPlugin.cfg");
            _config = Persistent<TorchInfluxDbConfig>.Load(configFilePath);

            _loggingConfigurator = new FileLoggingConfigurator("InfluxDbLogFile", new[] { "InfluxDb.*" }, TorchInfluxDbConfig.DefaultLogFilePath);
            _loggingConfigurator.Initialize();
            _loggingConfigurator.Configure(Config);

            if (Config.UseV18)
            {
                _endpoints = new InfluxDbWriteEndpointsV18(Config);
            }
            else
            {
                var auth = new InfluxDbAuth(Config);
                _endpoints = new InfluxDbWriteEndpoints(auth);
            }

            var writeClient = new InfluxDbWriteClient(_endpoints);
            var interval = TimeSpan.FromSeconds(Config.WriteIntervalSecs);
            _throttledWriteClient = new ThrottledInfluxDbWriteClient(writeClient, interval);

            TorchInfluxDbWriter.WriteEndpoints = _endpoints;
            TorchInfluxDbWriter.WriteClient = _throttledWriteClient;

            if (Config.Enable)
            {
                try
                {
                    Log.Info("Testing database connection...");

                    var point = new InfluxDbPoint("plugin_init").Field("message", "successfully initialized");
                    _endpoints.WriteAsync(new[] { point.BuildLine() }).Wait();

                    Log.Info("Done testing databse connection");
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        void OnGameLoaded()
        {
            Config.PropertyChanged += (_, _) =>
            {
                OnConfigUpdated(Config);
            };

            OnConfigUpdated(Config);
        }

        void OnConfigUpdated(TorchInfluxDbConfig config)
        {
            _loggingConfigurator.Configure(config);

            if (config.Enable != TorchInfluxDbWriter.Enabled)
            {
                TorchInfluxDbWriter.Enabled = config.Enable;
                _throttledWriteClient.SetRunning(config.Enable);

                Log.Info($"Writing enabled: {config.Enable}");
            }
        }

        void OnGameUnloading()
        {
            Log.Info("Unloading...");

            _config.Dispose();
            _throttledWriteClient?.StopWriting();
            _throttledWriteClient?.Flush();
            _endpoints?.Dispose();

            Log.Info("Unloaded");
        }
    }
}