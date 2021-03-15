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
// https://github.com/giltene/GilExamples/blob/master/SpinWaitTest/src/main/java/NanoTimeLatency.java
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime;
using HdrHistogram;

namespace PauseIntrinsics.GetTimestamp.Benchmark.Cli
{
    class Program
    {
        private const long WarmupIterations = 500_000L;
        private const long Iterations = 100_000_000L;
        private static readonly LongHistogram LatencyHistogram = new LongHistogram(3600_000_000_000L, 2);

        private static void CollectGetTimestampLatencies(long iterations)
        {
            for (long count = 0; count < iterations; count++)
            {
                long prevTime = Stopwatch.GetTimestamp();
                long currentTime = Stopwatch.GetTimestamp();
                LatencyHistogram.RecordValue(currentTime - prevTime);
            }
        }
        
        static void Main(string[] args)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            try
            {                
                CollectGetTimestampLatencies(WarmupIterations);
                LatencyHistogram.Reset();

                Thread.Sleep(500);
                Console.WriteLine("Warmup done. Starting Stopwatch.GetTimestamp() latency measurement.");

                long start = Stopwatch.GetTimestamp();
                CollectGetTimestampLatencies(Iterations);
                long stop = Stopwatch.GetTimestamp();

                long duration = stop - start;
                
                Console.WriteLine("duration = " + duration);
                Console.WriteLine("ns per op = " + duration / (Iterations * 1.0));
                Console.WriteLine("op/sec = " +
                                  (Iterations * 1000L * 1000L * 1000L) / duration);
                Console.WriteLine("\nStopwatch.GetTimestamp() latency histogram:\n");
                LatencyHistogram.OutputPercentileDistribution(Console.Out, 5, 1.0);

                Console.WriteLine("50%'ile:   " + LatencyHistogram.GetValueAtPercentile(50.0) + "ns");
                Console.WriteLine("90%'ile:   " + LatencyHistogram.GetValueAtPercentile(90.0) + "ns");
                Console.WriteLine("99%'ile:   " + LatencyHistogram.GetValueAtPercentile(99.0) + "ns");
                Console.WriteLine("99.9%'ile: " + LatencyHistogram.GetValueAtPercentile(99.9) + "ns");                
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("Stopwatch.GetTimestamp interrupted.");
            }
        }
    }
}
