using System;
using System.Windows.Controls;
using InfluxDb.Client;
using InfluxDb.Client.V18;
using InfluxDb.Client.Write;
using NLog;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.API.Session;
using Utils.Torch;

namespace InfluxDb.Torch
{
    public sealed class TorchInfluxDbPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchInfluxDbConfig> _config;
        UserControl _userControl;
        InfluxDbAuth _auth;
        IInfluxDbWriteEndpoints _endpoints;
        InfluxDbWriteClient _writeClient;
        FileLoggingConfigurator _loggingConfigurator;
        bool _runOnce;

        public UserControl GetControl() => _config.GetOrCreateUserControl(ref _userControl);

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.OnSessionStateChanged(TorchSessionState.Loaded, OnGameLoaded);
            this.OnSessionStateChanged(TorchSessionState.Unloading, OnGameUnloading);

            _loggingConfigurator = new FileLoggingConfigurator("InfluxDbLogFile", new[] { "InfluxDb.*" }, TorchInfluxDbConfig.DefaultLogFilePath);
            _loggingConfigurator.Initialize();

            _auth = new InfluxDbAuth();
            if (TorchInfluxDbConfig.Instance.UseV18)
            {
                _endpoints = new InfluxDbWriteEndpointsV18(_auth);
            }
            else
            {
                _endpoints = new InfluxDbWriteEndpoints(_auth);
            }

            ReloadConfig();

            var interval = TimeSpan.FromSeconds(TorchInfluxDbConfig.Instance.WriteIntervalSecs);
            _writeClient = new InfluxDbWriteClient(_endpoints, interval);

            // static api for other plugins
            TorchInfluxDbWriter.WriteEndpoints = _endpoints;
            TorchInfluxDbWriter.WriteClient = _writeClient;

            if (TorchInfluxDbConfig.Instance.Enable)
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
            TorchInfluxDbConfig.Instance.PropertyChanged += (_, _) =>
            {
                OnConfigUpdated();
            };

            OnConfigUpdated();
        }

        public void ReloadConfig()
        {
            Log.Info("config reloaded");

            var configFilePath = this.MakeConfigFilePath();
            _config?.Dispose();
            _config = Persistent<TorchInfluxDbConfig>.Load(configFilePath);
            TorchInfluxDbConfig.Instance = _config.Data;
            OnConfigUpdated();
        }

        void OnConfigUpdated()
        {
            _loggingConfigurator.Configure(TorchInfluxDbConfig.Instance);
            UpdateInfluxdbAuth();

            if (TorchInfluxDbConfig.Instance.Enable != TorchInfluxDbWriter.Enabled)
            {
                TorchInfluxDbWriter.Enabled = TorchInfluxDbConfig.Instance.Enable;
                _writeClient.SetRunning(TorchInfluxDbConfig.Instance.Enable);

                Log.Info($"Writing enabled: {TorchInfluxDbConfig.Instance.Enable}");
            }
        }

        void UpdateInfluxdbAuth()
        {
            _auth.HostUrl = TorchInfluxDbConfig.Instance.HostUrl;
            _auth.Organization = TorchInfluxDbConfig.Instance.Organization;
            _auth.Bucket = TorchInfluxDbConfig.Instance.Bucket;
            _auth.Username = TorchInfluxDbConfig.Instance.Username;
            _auth.Password = TorchInfluxDbConfig.Instance.Password;
        }

        public override void Update()
        {
            if (!_runOnce)
            {
                _runOnce = true;
                MyAPIGateway.Utilities.RegisterMessageHandler(TorchInfluxDbModdingApi.Id, OnMessage);
            }
        }

        void OnMessage(object message)
        {
            if (message is not string line) return;

            _writeClient.Queue(line);
        }

        void OnGameUnloading()
        {
            Log.Info("Unloading...");

            _config.Dispose();
            _writeClient?.StopWriting();
            _writeClient?.Flush();
            _endpoints?.Dispose();

            MyAPIGateway.Utilities.RegisterMessageHandler(TorchInfluxDbModdingApi.Id, OnMessage);

            Log.Info("Unloaded");
        }
    }
}