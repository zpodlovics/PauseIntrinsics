// ---------------------------------------------------------------------------
// Copyright (c) 2021, Zoltan Podlovics, KP-Tech Kft. All Rights Reserved.
//
// Licensed under the MIT License. See LICENSE.TXT in the 
// project root for license information.
// ---------------------------------------------------------------------------
// This file incorporates work covered by the following copyright and
// permission notice:
// ---------------------------------------------------------------------------
// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// ---------------------------------------------------------------------------
// Reference:
// https://github.com/giltene/GilExamples/blob/master/SpinWaitTest/src/main/java/SpinWaitTest.java
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime;
using HdrHistogram;

namespace PauseIntrinsics.SpinWait.Benchmark.Cli
{
    class Program
    {
        private const long WarmupPassCount = 5L;
        private const long WarmupIterations = 50_000L;
        private const long Iterations = 20_000_000L;
        private static long _spinData;
        private static readonly LongHistogram LatencyHistogram = new LongHistogram(3600_000_000_000L, 2);
        
        static void Producer(long iterations)
        {
            var prevTime = Stopwatch.GetTimestamp();
            for (long iteration = 0; iteration < iterations; iteration++)
            {
                while ((Thread.VolatileRead(ref _spinData) & 0x1L) == 1L)
                {
                    Thread.SpinWait(1);
                }
                
                long currTime = Stopwatch.GetTimestamp();
                LatencyHistogram.RecordValue(currTime - prevTime);
                prevTime = Stopwatch.GetTimestamp();
                Thread.VolatileWrite(ref _spinData, Thread.VolatileRead(ref _spinData) + 1L); // produce
                // wait                
            }
            
            while ((Thread.VolatileRead(ref _spinData) & 0x1L) == 1L)
            {
            }
            // terminate
            Thread.VolatileWrite(ref _spinData, -3);
        }

        static void Consumer()
        {
            while (Thread.VolatileRead(ref _spinData) >= 0L)
            {
                while ((Thread.VolatileRead(ref _spinData) & 0x1L) == 0L)
                {
                    Thread.SpinWait(1);
                }
                Thread.VolatileWrite(ref _spinData, Thread.VolatileRead(ref _spinData) + 1L); // consume
            }
        }
        
        static void Main(string[] args)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            
            try
            {
                Thread consumer = null;
                Thread producer = null;
                
                for (var i = 0; i < WarmupPassCount; i++)
                {
                    Thread.VolatileWrite(ref _spinData, 0);
                    consumer = new Thread(Consumer) {IsBackground = true};
                    consumer.Start();
                    producer = new Thread(() => Producer(WarmupIterations)) {IsBackground = true};
                    producer.Start();
                    consumer.Join();
                    producer.Join();
                    LatencyHistogram.Reset();
                }
                
                Thread.Sleep(1000);
                Console.WriteLine("# Warmup done. Restarting threads.");

                //return;
                
                Thread.VolatileWrite(ref _spinData, 0);
                consumer = new Thread(Consumer) {IsBackground = true};
                consumer.Start();

                producer = new Thread(() => Producer(Iterations)) {IsBackground = true};

                long start = Stopwatch.GetTimestamp();
                producer.Start();
                producer.Join();
                consumer.Join();

                long stop = Stopwatch.GetTimestamp();
                long duration = stop - start;

                Console.WriteLine("# Round trip latency histogram:");
                LatencyHistogram.OutputPercentileDistribution(Console.Out, 5, 1.0);
                Console.WriteLine("# duration = " + duration);
                Console.WriteLine("# duration (ns) per round trip op = " + duration / (Iterations * 1.0));
                Console.WriteLine("# round trip ops/sec = " +
                                  (Iterations * 1000L * 1000L * 1000L) / duration);

                Console.WriteLine("# 50%'ile:   " + LatencyHistogram.GetValueAtPercentile(50.0) + "ns");
                Console.WriteLine("# 90%'ile:   " + LatencyHistogram.GetValueAtPercentile(90.0) + "ns");
                Console.WriteLine("# 99%'ile:   " + LatencyHistogram.GetValueAtPercentile(99.0) + "ns");
                Console.WriteLine("# 99.9%'ile: " + LatencyHistogram.GetValueAtPercentile(99.9) + "ns");                
                
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("Program interrupted.");
            }
        }        
    }
}
