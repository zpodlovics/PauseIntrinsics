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
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;


namespace PauseIntrinsics.Pause.Throughput.Benchmark.Cli
{
    class Program
    {
        private const long WarmupPassCount = 5L;
        private const long WarmupIterations = 50_000L;
        private const long Iterations = 20_000_000L;
        private static long _spinData;
        private static long _totalSpins;
        
        static void Producer(long iterations)
        {
            long spins = 0L;
            for (long iteration = 0; iteration < iterations; iteration++)
            {
                while ((Thread.VolatileRead(ref _spinData) & 0x1L) == 1L)
                {
#if HAVE_PAUSE_INTRINSICS                    
                    Sse2.Pause();
#else
                    Sse2.MemoryFence();
#endif
                    spins++;
                }
                Thread.VolatileWrite(ref _spinData, Thread.VolatileRead(ref _spinData) + 1L); // produce
                // wait                
            }
            
            while ((Thread.VolatileRead(ref _spinData) & 0x1L) == 1L)
            {
            }
            // terminate
            Thread.VolatileWrite(ref _spinData, -3);  // produce
            Thread.VolatileWrite(ref _totalSpins, Thread.VolatileRead(ref _totalSpins) + spins); // produce
        }

        static void Consumer()
        {
            while (Thread.VolatileRead(ref _spinData) >= 0L)
            {
                while ((Thread.VolatileRead(ref _spinData) & 0x1L) == 0L)
                {
#if HAVE_PAUSE_INTRINSICS                    
                    Sse2.Pause();
#else
                    Sse2.MemoryFence();
#endif
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

                Console.WriteLine("# of iterations in producer = " + Iterations);
                Console.WriteLine("# of total spins in producer = " + _totalSpins);
                Console.WriteLine("# of producer spins per iteration = " + (1.0 * _totalSpins)/ Iterations);                
                Console.WriteLine("# duration = " + duration);
                Console.WriteLine("# duration (ns) per round trip op = " + duration / (Iterations * 1.0));
                Console.WriteLine("# round trip ops/sec = " +
                                  (Iterations * 1000L * 1000L * 1000L) / duration);
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("Program interrupted.");
            }
        }        
    }
}
