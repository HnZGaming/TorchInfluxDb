using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using InfluxDb.Client;
using InfluxDb.Client.Orm;
using InfluxDb.Client.Read;
using InfluxDb.Client.Write;
using NUnit.Framework;

namespace InfluxDb.Test
{
    [TestFixture]
    public class Tests
    {
        InfluxDbWriteEndpoints _writeEndpoints;
        InfluxDbReadEndpoints _readEndpoints;

        [SetUp]
        public void SetUp()
        {
            // InfluxDB instance must be running for this test suite
            if (!Process.GetProcessesByName("influxd").Any())
            {
                throw new Exception("InfluxDB instance not running");
            }

            var auth = new InfluxDbAuth
            {
                HostUrl = "http://localhost:8086",
                Bucket = "gaalsien", // This bucket must be present in the DB instance
                Organization = "whatever",
                AuthenticationToken = null,
            };

            _writeEndpoints = new InfluxDbWriteEndpoints(auth);
            _readEndpoints = new InfluxDbReadEndpoints(auth);
        }

        [TearDown]
        public void TearDown()
        {
            _writeEndpoints.Dispose();
            _readEndpoints.Dispose();
        }

        [Test]
        public void WriteEndpoints_Write()
        {
            var lines = new List<string>
            {
                // just random data
                "players_churn,player_name=ryo0ka online_time=1",
                "players_churn,player_name=sama online_time=1",
            };

            // Will throw if anything went wrong
            _writeEndpoints.WriteAsync(lines).Wait();
        }

        [Test]
        public void ReadEndpoints_Read()
        {
            var query = "select online_time from players_churn group by player_name";

            // Will throw if anything went wrong
            var result = _readEndpoints.QueryQlAsync(query).Result;

            // Debugging
            foreach (var series in result)
            {
                Console.WriteLine(series);
                foreach (var ormObj in InfluxDbOrmFactory.Create<MyOrmObj>(series))
                {
                    Console.WriteLine(ormObj);
                }
            }
        }
    }
}