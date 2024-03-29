﻿using Dapper;
using StackExchange.Profiling;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Threading;
using static System.Console;

namespace MiniProfilerSample.net50
{
    internal static class Program
    {
        public static void Main()
        {
            try
            {
                Test();
                WriteLine(MiniProfiler.Current.RenderPlainText());
                TestMultiThreaded();
                WriteLine(MiniProfiler.Current.RenderPlainText());

                if (Debugger.IsAttached)
                    ReadKey();
            }
            catch (Exception ex)
            {
                WriteLine(ex);
            }
        }

        /// <summary>
        /// test the profiling.
        /// </summary>
        public static void Test()
        {
            var mp = MiniProfiler.StartNew("Test");

            using (mp.Step("Level 1"))
            using (var conn = GetConnection())
            {
                conn.Query<long>("select 1");

                using (mp.Step("Level 2"))
                {
                    conn.Query<long>("select 1");
                }

                using (var wc = new WebClient())
                using (mp.CustomTiming("http", "GET https://google.com"))
                {
                    wc.DownloadString("https://google.com");
                }
            }

            mp.Stop();
        }

        public static void TestMultiThreaded()
        {
            var mp = MiniProfiler.StartNew("Locking");

            static void doWork() => Thread.Sleep(new Random().Next(1, 50));

            using (mp.Step("outer"))
            {
                System.Threading.Tasks.Parallel.For(0, 5, i =>
                {
                    doWork();

                    using (mp.Step("step " + i))
                    {
                        doWork();

                        using (mp.Step("sub-step" + i))
                        {
                            doWork();
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        /// <returns>the database connection abstraction</returns>
        public static DbConnection GetConnection()
        {
            DbConnection cnn = new System.Data.SQLite.SQLiteConnection("Data Source=:memory:");

            // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
            // when MiniProfiler.Current is null, this connection will not record any database timings
            if (MiniProfiler.Current != null)
            {
                cnn = new StackExchange.Profiling.Data.ProfiledDbConnection(cnn, MiniProfiler.Current);
            }

            cnn.Open();
            return cnn;
        }
    }
}