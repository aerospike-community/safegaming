using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameSimulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameSimulator.Tests.SettingsSimTests.TestSettings;

namespace GameSimulator.Tests
{
    [TestClass()]
    public class SettingsSimTests
    {
        public class TestSettings : PlayerCommon.Settings
        {
            public TestSettings(string appJsonFile = "appsettings.json", int testFuncInvoke = 0)
            : base(appJsonFile)
            {
                if(testFuncInvoke == 1)
                    PlayerCommon.Settings.AddFuncPathAction("TestSettings:testSettingsCls:instanceTestSettings",
                        (config, path, propName, propType, prop, propParent) =>
                        {
                            Assert.IsNotNull(config);
                            Assert.IsNotNull(propName);
                            Assert.IsNotNull(propType);
                            Assert.IsNotNull(propParent);
                            Assert.IsInstanceOfType(propParent, typeof(TestSettingsCls1));
                            Assert.AreEqual(typeof(TestSettingsCls), propType);
                            Assert.AreEqual("instanceTestSettings", propName);
                            Assert.IsNull(prop);

                            return (prop, InvokePathActions.Ignore);
                        });
                else if (testFuncInvoke == 2)
                    PlayerCommon.Settings.AddFuncPathAction("TestSettings:testSettingsCls:instanceTestSettings",
                        (config, path, propName, propType, prop, propParent) =>
                        {
                            Assert.IsNotNull(config);
                            Assert.IsNotNull(propName);
                            Assert.IsNotNull(propType);
                            Assert.IsNotNull(propParent);
                            Assert.IsInstanceOfType(propParent, typeof(TestSettingsCls1));
                            Assert.AreEqual(typeof(TestSettingsCls), propType);
                            Assert.AreEqual("instanceTestSettings", propName);
                            Assert.IsNull(prop);

                            var newInstance = new TestSettingsCls()
                            {
                                invoke2 = "Invoke2"
                            };                            

                            return (newInstance, InvokePathActions.Update);                            
                        });
                else if (testFuncInvoke == 3)
                    PlayerCommon.Settings.AddFuncPathAction("TestSettings:testSettingsCls:instanceTestSettings",
                        (config, path, propName, propType, prop, propParent) =>
                        {
                            Assert.IsNotNull(config);
                            Assert.IsNotNull(propName);
                            Assert.IsNotNull(propType);
                            Assert.AreEqual(typeof(TestSettingsCls), propType);
                            Assert.AreEqual("instanceTestSettings", propName);

                            if (prop is not null)
                            {
                                Assert.IsNull(propParent);

                                Assert.IsInstanceOfType(prop, typeof(TestSettingsCls));
                                Assert.AreEqual("Invoke3", ((TestSettingsCls)prop).invoke3);
                                Assert.IsNull(((TestSettingsCls)prop).invoke2);
                                Assert.IsNull(((TestSettingsCls)prop).invoke1);
                                Assert.IsNull(((TestSettingsCls)prop).invoke4);
                            }
                            else
                            {
                                Assert.IsNotNull(propParent);
                                Assert.IsInstanceOfType(propParent, typeof(TestSettingsCls1));

                                var newInstance = new TestSettingsCls()
                                {
                                    invoke3 = "Invoke3"
                                };

                                return (newInstance, InvokePathActions.ContinueAndUseValue);
                            }
                            return (prop, InvokePathActions.ContinueAndUseValue);
                        });


                PlayerCommon.Settings.GetSetting(this.ConfigurationBuilderFile,
                                                    ref this.instanceTestSettings,
                                                    "TestSettings",
                                                    this);
            }

            public class TestSettingsCls1
            {                
                public string stringFld;
                public int intFld;
                public decimal decimalFld;
                public double doubleFld;
                public long longFld;
                public List<int> listInts;
                public List<string> stringList;
                public TestSettingsCls instanceTestSettings;
            }

            public class TestSettingsCls
            {
                public string stringFld;
                public int intFld;
                public decimal decimalFld;
                public double doubleFld;
                public long longFld;
                public List<int> listInts;
                public List<string> stringList;
                public DateTime datetimeFld;
                public DateTimeOffset datetimeOffsetFld;
                public TimeSpan timeSpanFld;
                public DateTimeKind dateTimeKindFld = DateTimeKind.Unspecified;

                public TestSettingsCls1 testSettingsCls;

                public bool boolFld;
                public string invoke1;
                public string invoke2;
                public string invoke3;
                public string invoke4;
            }

            public TestSettingsCls instanceTestSettings;

        }

        [TestMethod()]
        public void SettingsSimTest()
        {
            var testAppSettingSim = new SettingsSim("appSettings1.json");

            Assert.IsNotNull(testAppSettingSim);
            Assert.IsTrue(testAppSettingSim.IgnoreFaults);

            var testAppSetting2 = new TestSettings("appSettings2.json");

            Assert.AreEqual(-1, testAppSetting2.MaxDegreeOfParallelism);
            Assert.AreEqual(-1, testAppSetting2.WorkerThreads);
            Assert.AreEqual(1000, testAppSetting2.CompletionPortThreads);
            Assert.IsFalse(testAppSetting2.IgnoreFaults);
            Assert.IsFalse(testAppSetting2.TimeEvents);
            Assert.AreEqual(100, testAppSetting2.WarnMaxMSLatencyDBExceeded);
            Assert.IsTrue(testAppSetting2.EnableHistogram);
            Assert.IsNull(testAppSetting2.HGRMFile);
            Assert.AreEqual(3,testAppSetting2.HGPrecision);
            Assert.AreEqual(1L,testAppSetting2.HGLowestTickValue);
            Assert.AreEqual(100000000L, testAppSetting2.HGHighestTickValue);
            Assert.AreEqual(5, testAppSetting2.HGReportPercentileTicksPerHalfDistance);
            Assert.AreEqual("Milliseconds", testAppSetting2.HGReportTickToUnitRatio);
            Assert.AreEqual(HdrHistogram.OutputScalingFactor.TimeStampToMilliseconds, testAppSetting2.HGReportUnitRatio);
            Assert.IsNull(testAppSetting2.TimingCSVFile);
            Assert.IsNull(testAppSetting2.TimingJsonFile);
            Assert.AreEqual("yyyy-MM-ddTHH:mm:ss.ffffzzz", testAppSetting2.TimeStampFormatString);
            Assert.AreEqual("yyyy-MM-ddTHH:mm:ss.ffff", testAppSetting2.TimeZoneFormatWoZone);
            Assert.IsNull(testAppSetting2.DBConnectionString);

            Assert.IsNotNull(testAppSettingSim);
            Assert.AreEqual("abcdef", testAppSetting2.instanceTestSettings.stringFld);
            Assert.AreEqual(1, testAppSetting2.instanceTestSettings.intFld);
            Assert.AreEqual(12.34m, testAppSetting2.instanceTestSettings.decimalFld);
            Assert.AreEqual(56.78d, testAppSetting2.instanceTestSettings.doubleFld);
            Assert.AreEqual(123456L, testAppSetting2.instanceTestSettings.longFld) ;
            CollectionAssert.AreEquivalent(new List<int>() { 1, 2, 3, 4, 5 }, testAppSetting2.instanceTestSettings.listInts);            
            CollectionAssert.AreEquivalent(new List<string>() { "a", "b", "c"}, testAppSetting2.instanceTestSettings.stringList);
            Assert.AreEqual(DateTime.Parse("2023-01-01 14:20:23.456"), testAppSetting2.instanceTestSettings.datetimeFld);
            Assert.AreEqual(DateTimeOffset.Parse("2023-01-01 14:20:23.456+07"), testAppSetting2.instanceTestSettings.datetimeOffsetFld);
            Assert.AreEqual(TimeSpan.Parse("14:20:23.456"), testAppSetting2.instanceTestSettings.timeSpanFld);
            Assert.AreEqual(DateTimeKind.Local, testAppSetting2.instanceTestSettings.dateTimeKindFld);

            Assert.IsNotNull(testAppSetting2.instanceTestSettings.testSettingsCls);
            Assert.AreEqual("ghijk", testAppSetting2.instanceTestSettings.testSettingsCls.stringFld);
            Assert.AreEqual(2, testAppSetting2.instanceTestSettings.testSettingsCls.intFld);
            Assert.AreEqual(567.890m, testAppSetting2.instanceTestSettings.testSettingsCls.decimalFld);
            Assert.AreEqual(910.123d, testAppSetting2.instanceTestSettings.testSettingsCls.doubleFld);
            Assert.AreEqual(7890L, testAppSetting2.instanceTestSettings.testSettingsCls.longFld);
            CollectionAssert.AreEquivalent(new List<int>() { 6, 7, 8, 9 }, testAppSetting2.instanceTestSettings.testSettingsCls.listInts);
            CollectionAssert.AreEquivalent(new List<string>() { "d", "e", "f" }, testAppSetting2.instanceTestSettings.testSettingsCls.stringList);
            
            Assert.IsNotNull(testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings);
            Assert.AreEqual("lmnop", testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.stringFld);
            Assert.AreEqual(3, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.intFld);
            Assert.AreEqual(890.123m, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.decimalFld);
            Assert.AreEqual(101.234d, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.doubleFld);
            Assert.AreEqual(89012, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.longFld);
            CollectionAssert.AreEquivalent(new List<int>() { 9, 0, 1, 0 }, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.listInts);
            CollectionAssert.AreEquivalent(new List<string>() { "g", "h", "i" }, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.stringList);

            Assert.IsNull(testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.testSettingsCls);

            Assert.IsTrue(testAppSetting2.instanceTestSettings.boolFld);

            Assert.AreEqual(1, PlayerCommon.Settings.NotFoundSettingClassProps.Count);
            Assert.AreEqual("TestSettings:NotValidProperty", PlayerCommon.Settings.NotFoundSettingClassProps.First());

            PlayerCommon.Settings.RemoveNotFoundSettingClassProps(new List<string>() { "TestSettings:" });
            Assert.AreEqual(0, PlayerCommon.Settings.NotFoundSettingClassProps.Count);

            Assert.IsNull(testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke1);
            Assert.IsNull(testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke2);
            Assert.IsNull(testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke3);
            Assert.IsNull(testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke4);

            var testAppSetting2Invoke1 = new TestSettings("appSettings2.json", 1);

            Assert.IsNull(testAppSetting2Invoke1.instanceTestSettings.testSettingsCls.instanceTestSettings);
            
            var testAppSetting2Invoke2 = new TestSettings("appSettings2.json", 2);

            Assert.IsNotNull(testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings);

            Assert.IsNull(testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke1);
            Assert.AreEqual("Invoke2", testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke2);
            Assert.IsNull(testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke3);
            Assert.IsNull(testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke4);

            Assert.IsNull(testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.stringFld);
            Assert.AreEqual(0, testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.intFld);
            Assert.AreEqual(0m, testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.decimalFld);
            Assert.AreEqual(0d, testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.doubleFld);
            Assert.AreEqual(0, testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.longFld);
            Assert.IsNull(testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.listInts);
            Assert.IsNull(testAppSetting2Invoke2.instanceTestSettings.testSettingsCls.instanceTestSettings.stringList);

            PlayerCommon.Settings.AddFuncPathAction("TestSettings:testSettingsCls:instanceTestSettings:doubleFld",
                                    (config, path, propName, propType, propValue, parent) =>
                                    {
                                        Assert.AreEqual(0, config.GetChildren().Count());
                                        Assert.IsInstanceOfType(propValue, typeof(string));
                                        Assert.AreEqual(typeof(double), propType);
                                        Assert.IsInstanceOfType(parent, typeof(TestSettingsCls));
                                        Assert.AreEqual("101.234", propValue);
                                        return (123.123d, PlayerCommon.Settings.InvokePathActions.Update);
                                    });
            PlayerCommon.Settings.AddFuncPathAction(typeof(decimal),
                                    (config, path, propName, propType, propValue, parent) =>
                                    {
                                        Assert.AreEqual(0, config.GetChildren().Count());
                                        Assert.IsInstanceOfType(propValue, typeof(string));
                                        Assert.AreEqual(typeof(decimal), propType);
                                        Assert.IsNotNull(path);
                                        return (null, PlayerCommon.Settings.InvokePathActions.Continue);
                                    });
            PlayerCommon.Settings.AddPathSaveObj("TestSettings:testSettingsCls:instanceTestSettings");

            var testAppSetting2Invoke3 = new TestSettings("appSettings2.json", 3);

            Assert.IsNotNull(testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings);

            Assert.IsNull(testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke1);
            Assert.AreEqual("Invoke3", testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke3);
            Assert.IsNull(testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke2);
            Assert.IsNull(testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.invoke4);

            Assert.AreEqual("lmnop", testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.stringFld);
            Assert.AreEqual(3, testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.intFld);
            Assert.AreEqual(890.123m, testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.decimalFld);
            Assert.AreEqual(123.123d, testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.doubleFld);
            Assert.AreEqual(89012, testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.longFld);
            CollectionAssert.AreEquivalent(new List<int>() { 9, 0, 1, 0 }, testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.listInts);
            CollectionAssert.AreEquivalent(new List<string>() { "g", "h", "i" }, testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings.stringList);

            var cachedObj = PlayerCommon.Settings.GetPathSaveObj("TestSettings:testSettingsCls:instanceTestSettings");

            Assert.IsNotNull(cachedObj);
            Assert.ReferenceEquals(testAppSetting2Invoke3.instanceTestSettings.testSettingsCls.instanceTestSettings, cachedObj);

            var vType = typeof(string);
            object vTest = "abcdefg";
            var value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vTest = null;
            value = PlayerCommon.Settings.SettingConvertValue(vType, (string) vTest);

            Assert.IsNull(value);

            vTest = "<ignore>";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsNull(value);

            vTest = "#Ignore this";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsNull(value);

            vTest = string.Empty;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsNull(value);

            vTest = "<empty>";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(string.Empty, value);

            vTest = "<default>";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(PlayerCommon.Settings.ConfigNullValue, value);

            vTest = "<null>";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(PlayerCommon.Settings.ConfigNullValue, value);

            vTest = "<NULL>";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(PlayerCommon.Settings.ConfigNullValue, value);

            vType = typeof(int);
            vTest = 123;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vType = typeof(long);
            vTest = 123L;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vType = typeof(decimal);
            vTest = 123.23m;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vType = typeof(double);
            vTest = 123.34d;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vType = typeof(long);
            vTest = 123L;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vType = typeof(Int16);
            vTest = (Int16) 123;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vType = typeof(bool);
            vTest = true;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vType = typeof(DateTime);
            vTest = DateTime.Now;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest.ToString(), value.ToString());

            vTest = "Now";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);

            vType = typeof(DateTimeOffset);
            vTest = DateTimeOffset.Now;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest.ToString(), value.ToString());

            vTest = "now";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);

            vType = typeof(bool);
            vTest = true;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);

            vType = typeof(DateTimeKind);
            vTest = DateTimeKind.Utc;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(vTest, value);            

            vType = typeof(TimeSpan);
            vTest = 5000;
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual((double) (int)vTest, ((TimeSpan) value).TotalMilliseconds);

            vTest = "now";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            
            vTest = "5 secs";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(5d, ((TimeSpan)value).TotalSeconds);

            vTest = "5secs";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(5d, ((TimeSpan)value).TotalSeconds);

            vTest = "5 days";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(5d, ((TimeSpan)value).TotalDays);

            vTest = "5 hours";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(5d, ((TimeSpan)value).TotalHours);

            vTest = "5 ms";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(5d, ((TimeSpan)value).TotalMilliseconds);

            vTest = "1000 ns";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(1000d, ((TimeSpan)value).TotalNanoseconds);

            vTest = "5 ticks";
            value = PlayerCommon.Settings.SettingConvertValue(vType, vTest.ToString());

            Assert.IsInstanceOfType(value, vType);
            Assert.AreEqual(5L, ((TimeSpan)value).Ticks);
        }
    }
}