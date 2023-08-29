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

        public WriteConcern CreateWriteConcern(WriteConcern defaultValues)
        {
            if (!string.IsNullOrEmpty(this.UseConst))
            {
                var constProp = typeof(WriteConcern).GetProperty(this.UseConst)
                                    ?? throw new ArgumentException($"Invalid \"{this.UseConst}\" as a constant for WriteConcern within DB Connection Setting");

                return constProp.GetValue(null) as WriteConcern;
            }

            bool updated = false;

            Optional<TimeSpan?> wTimeout = defaultValues is null ? default : defaultValues.WTimeout;
            Optional<bool?> journal = defaultValues is null ? default : defaultValues.Journal;
            Optional<bool?> fsync = defaultValues is null ? default : defaultValues.FSync;
            Optional<WValue> wValue = defaultValues is null ? default : defaultValues.W;

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

            if (updated)
                return new WriteConcern(wValue, wTimeout, journal, fsync);

            if (defaultValues is null)
                return null;

            return defaultValues;
        }
    }

    partial class SettingsSim
    {
        static SettingsSim()
        {
            RemoveFromNotFoundSettings.Add("Aerospike:");

            PlayerCommon.Settings.AddFuncPathAction(typeof(WriteConcern),
                (IConfiguration config, string path, string propName, Type propType, object propValue, object propParent)
                    =>
                {
                    if (config.GetChildren().Any())
                    {
                        MGWriteConcern mgWriteConcern = null;

                        PlayerCommon.Settings.GetSetting(config, ref mgWriteConcern, propName, propParent);

                        var writeConcern = mgWriteConcern.CreateWriteConcern(Settings.GetPathSaveObj("GameSimulator:Mongodb:DriverSettings:WriteConcern") as WriteConcern
                                                                                ?? propValue as WriteConcern);
                        return (writeConcern,
                                    writeConcern is null
                                        ? InvokePathActions.Ignore
                                        : InvokePathActions.Update);
                    }
                    else
                        return (null, InvokePathActions.Ignore);

                });
            PlayerCommon.Settings.AddFuncPathAction(typeof(ReadConcern),
                (IConfiguration config, string path, string propName, Type propType, object propValue, object propParent)
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
            PlayerCommon.Settings.AddPathSaveObj("GameSimulator:Mongodb:DriverSettings:WriteConcern");

        }
    }
}
