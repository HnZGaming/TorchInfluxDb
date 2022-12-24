using System.Xml.Serialization;
using Torch;
using Torch.Views;
using Utils.Torch;

namespace InfluxDb.Torch
{
    public sealed class TorchInfluxDbConfig : ViewModel, FileLoggingConfigurator.IConfig
    {
        const string OperationGroupName = "Operation";
        const string CredentialsGroupName = "Credentials";
        const string CredentialsV18GroupName = "Credentials (v1.8)";

        public const string DefaultLogFilePath = "Logs/InfluxDb-${shortdate}.log";

        bool _enable;
        string _hostUrl = "http://localhost:8086";
        string _bucket = "test";
        string _organization = "";
        string _authenticationToken = "";
        float _writeIntervalSecs = 10;
        string _logFilePath = DefaultLogFilePath;
        bool _suppressWpfOutput;
        bool _enableLoggingTrace;
        bool _enableLoggingDebug;
        string _username;
        string _password;
        bool _useV18;

        [XmlElement(nameof(Enable))]
        [Display(Order = 0, Name = "Enable", GroupName = OperationGroupName)]
        public bool Enable
        {
            get => _enable;
            set => SetValue(ref _enable, value);
        }

        [XmlElement(nameof(HostUrl))]
        [Display(Order = 2, Name = "Host URL", GroupName = CredentialsGroupName)]
        public string HostUrl
        {
            get => _hostUrl;
            set => SetValue(ref _hostUrl, value);
        }

        [XmlElement(nameof(Organization))]
        [Display(Order = 3, Name = "Organization", GroupName = CredentialsGroupName)]
        public string Organization
        {
            get => _organization;
            set => SetValue(ref _organization, value);
        }

        [XmlElement(nameof(Bucket))]
        [Display(Order = 4, Name = "Bucket Name", GroupName = CredentialsGroupName)]
        public string Bucket
        {
            get => _bucket;
            set => SetValue(ref _bucket, value);
        }

        [XmlElement(nameof(AuthenticationToken))]
        [Display(Order = 5, Name = "Authentication Token (Optional)", GroupName = CredentialsGroupName)]
        public string AuthenticationToken
        {
            get => _authenticationToken;
            set => SetValue(ref _authenticationToken, value);
        }

        [XmlElement(nameof(UseV18))]
        [Display(Order = 2, Name = "Use v1.8 (NEED RESTART)", GroupName = CredentialsV18GroupName)]
        public bool UseV18
        {
            get => _useV18;
            set => SetValue(ref _useV18, value);
        }

        [XmlElement(nameof(Username))]
        [Display(Order = 3, Name = "Username (v1.8)", GroupName = CredentialsV18GroupName)]
        public string Username
        {
            get => _username;
            set => SetValue(ref _username, value);
        }

        [XmlElement(nameof(Password))]
        [Display(Order = 5, Name = "Password (v1.8)", GroupName = CredentialsV18GroupName)]
        public string Password
        {
            get => _password;
            set => SetValue(ref _password, value);
        }

        [XmlElement(nameof(WriteIntervalSecs))]
        [Display(Order = 7, Name = "Throttle Interval (Seconds)", GroupName = OperationGroupName)]
        public float WriteIntervalSecs
        {
            get => _writeIntervalSecs;
            set => SetValue(ref _writeIntervalSecs, value);
        }

        [XmlElement(nameof(LogFilePath))]
        [Display(Order = 8, Name = "Log File Path", GroupName = OperationGroupName)]
        public string LogFilePath
        {
            get => _logFilePath;
            set => SetValue(ref _logFilePath, value);
        }

        [XmlElement(nameof(SuppressWpfOutput))]
        [Display(Order = 9, Name = "Suppress Console Output", GroupName = OperationGroupName)]
        public bool SuppressWpfOutput
        {
            get => _suppressWpfOutput;
            set => SetValue(ref _suppressWpfOutput, value);
        }

        [XmlElement(nameof(EnableLoggingTrace))]
        [Display(Order = 10, Name = "Enable Logging Trace", GroupName = OperationGroupName)]
        public bool EnableLoggingTrace
        {
            get => _enableLoggingTrace;
            set => SetValue(ref _enableLoggingTrace, value);
        }

        [XmlElement(nameof(EnableLoggingDebug))]
        [Display(Order = 10, Name = "Enable Logging Debug", GroupName = OperationGroupName)]
        public bool EnableLoggingDebug
        {
            get => _enableLoggingDebug;
            set => SetValue(ref _enableLoggingDebug, value);
        }
    }
}