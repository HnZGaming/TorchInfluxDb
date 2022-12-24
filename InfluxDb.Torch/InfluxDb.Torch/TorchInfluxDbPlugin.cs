using System;
using System.ComponentModel;
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
        public TorchInfluxDbConfig Config => _config.Data;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.OnSessionStateChanged(TorchSessionState.Loaded, OnGameLoaded);
            this.OnSessionStateChanged(TorchSessionState.Unloading, OnGameUnloading);

            _loggingConfigurator = new FileLoggingConfigurator("InfluxDbLogFile", new[] { "InfluxDb.*" }, TorchInfluxDbConfig.DefaultLogFilePath);
            _loggingConfigurator.Initialize();

            _auth = new InfluxDbAuth();

            LoadConfigFile();

            if (Config.UseV18)
            {
                _endpoints = new InfluxDbWriteEndpointsV18(_auth);
            }
            else
            {
                _endpoints = new InfluxDbWriteEndpoints(_auth);
            }

            var interval = TimeSpan.FromSeconds(Config.WriteIntervalSecs);
            _writeClient = new InfluxDbWriteClient(_endpoints, interval);

            // static api for other plugins
            TorchInfluxDbWriter.WriteEndpoints = _endpoints;
            TorchInfluxDbWriter.WriteClient = _writeClient;

            ApplyConfig();

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

        void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReloadConfig();
        }

        void OnGameLoaded()
        {
            LoadConfigFile();
            ApplyConfig();
        }

        void LoadConfigFile()
        {
            if (_config?.Data != null)
            {
                _config.Data.PropertyChanged -= OnConfigPropertyChanged;
            }

            _config?.Dispose();
            _config = null;

            var configFilePath = this.MakeFilePath($"{nameof(TorchInfluxDbPlugin)}.cfg");
            _config = Persistent<TorchInfluxDbConfig>.Load(configFilePath);

            if (_config.Data == null)
            {
                throw new InvalidOperationException("config not found");
            }

            _config.Data.PropertyChanged += OnConfigPropertyChanged;

            Log.Info("config loaded");
        }

        void ApplyConfig()
        {
            _loggingConfigurator.Configure(Config);

            _auth.HostUrl = Config.HostUrl;
            _auth.Organization = Config.Organization;
            _auth.Bucket = Config.Bucket;
            _auth.Username = Config.Username;
            _auth.Password = Config.Password;
            _auth.AuthenticationToken = Config.AuthenticationToken;

            if (Config.Enable != TorchInfluxDbWriter.Enabled)
            {
                TorchInfluxDbWriter.Enabled = Config.Enable;
                _writeClient.SetRunning(Config.Enable);

                Log.Info($"Writing enabled: {Config.Enable}");
            }
        }

        public void ReloadConfig()
        {
            LoadConfigFile();
            ApplyConfig();
        }

        public override void Update()
        {
            if (!_runOnce)
            {
                _runOnce = true;
                MyAPIGateway.Utilities.RegisterMessageHandler(TorchInfluxDbModdingApi.Id, OnModdingApiMessageReceived);
            }
        }

        void OnModdingApiMessageReceived(object message)
        {
            if (message is not string line)
            {
                Log.Error($"invalid modding message received: {message}");
                return;
            }

            _writeClient.Queue(line);
            Log.Trace($"modding api line queued: {line}");
        }

        void OnGameUnloading()
        {
            Log.Info("Unloading...");

            _config.Dispose();
            _writeClient?.StopWriting();
            _writeClient?.Flush();
            _endpoints?.Dispose();

            MyAPIGateway.Utilities.RegisterMessageHandler(TorchInfluxDbModdingApi.Id, OnModdingApiMessageReceived);

            Log.Info("Unloaded");
        }
    }
}