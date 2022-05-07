using Sandbox.ModAPI;

namespace InfluxDb.Torch
{
    public static class TorchInfluxDbModdingApi
    {
        public static readonly int Id = "InfluxDb v2.2".GetHashCode();

        public static void Write(string line)
        {
            MyAPIGateway.Utilities.SendModMessage(Id, line);
        }
    }
}