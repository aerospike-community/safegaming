using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Faker;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using static MongoDB.Driver.WriteConcern;
using PlayerCommon;

namespace GameSimulator
{
    partial class GameSimulatorSettings
    {                
        public MongoDBSettings Mongodb;
    }

    public sealed class MGWriteConcern
    {
        public MGWriteConcern()
        { }

        public string UseConst;
        public int? timeout;
        public bool? journal;
        public bool? fsync;
        public string WValue;

        public WriteConcern CreateWriteConcern()
        {
            if (!string.IsNullOrEmpty(this.UseConst))
            {
                var constProp = typeof(WriteConcern).GetProperty(this.UseConst)
                                    ?? throw new ArgumentException($"Invalid \"{this.UseConst}\" as a constant for WriteConcern within DB Connection Setting");

                return constProp.GetValue(null) as WriteConcern;
            }

            bool updated = false;

            Optional<TimeSpan?> wTimeout = default;
            Optional<bool?> journal = default;
            Optional<bool?> fsync = default;
            Optional<WValue> wValue = default;

            if (this.timeout.HasValue)
            {
                wTimeout = new Optional<TimeSpan?>(TimeSpan.FromMilliseconds(this.timeout.Value));
                updated = true;
            }
            if (this.journal.HasValue)
            {
                journal = new Optional<bool?>(this.journal.Value);
                updated = true;
            }
            if (this.fsync.HasValue)
            {
                fsync = new Optional<bool?>(this.fsync.Value);
                updated = true;
            }
            if (!string.IsNullOrEmpty(this.WValue))
            {
                if (int.TryParse(this.WValue, out var value))
                {
                    wValue = new Optional<WValue>(new WCount(value));
                }
                else
                {
                    wValue = new Optional<WValue>(new WMode(this.WValue));
                }
                updated = true;
            }

            return updated ? new WriteConcern(wValue, wTimeout, journal, fsync) : null;
        }
    }


    partial class SettingsSim
    {
        static SettingsSim()
        {
            RemoveFromNotFoundSettings.Add("Aerospike:");

            PlayerCommon.Settings.AddFuncPathAction("GameSimulator:Mongodb:DriverSettings:WriteConcern",
                (IConfiguration config, string propName, Type propType, object propValue, object propInstance)
                    =>
                {
                    if (config.GetChildren().Any())
                    {
                        MGWriteConcern mgWriteConcern = null;

                        PlayerCommon.Settings.GetSetting(config, ref mgWriteConcern, propName);

                        var writeConcern = mgWriteConcern.CreateWriteConcern();
                        return (writeConcern,
                                    writeConcern is null
                                        ? InvokePathActions.Ignore
                                        : InvokePathActions.Update);
                    }
                    else
                        return (null, InvokePathActions.Ignore);

                });
            PlayerCommon.Settings.AddFuncPathAction("GameSimulator:Mongodb:DriverSettings:ReadConcern",
                (IConfiguration config, string propName, Type propType, object propValue, object propInstance)
                    =>
                {
                    if (config.GetChildren().Any())
                    {
                        var constValue = config.GetChildren().FirstOrDefault(c => c.Key == "Level")?.Value;

                        if (!string.IsNullOrEmpty(constValue))
                        {
                            var constProp = typeof(ReadConcern).GetProperty(constValue)
                                                ?? throw new ArgumentException($"Invalid \"{constValue}\" as a constant for ReadConcern within DB Connection Setting");

                            return (constProp.GetValue(null) as ReadConcern,
                                        InvokePathActions.Update);
                        }
                    }
                    
                    return (null, InvokePathActions.Ignore);
                });
        }
    }
}
