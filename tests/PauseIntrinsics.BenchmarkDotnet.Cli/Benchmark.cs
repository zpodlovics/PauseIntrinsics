// ---------------------------------------------------------------------------
// Copyright (c) 2021, Zoltan Podlovics, KP-Tech Kft. All Rights Reserved.
//
// Licensed under the MIT License. See LICENSE.TXT in the 
// project root for license information.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace PauseIntrinsics.BenchmarkDotNet.Cli
{
    public class Benchmark
    {
        public Benchmark()
        {
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void StaticBusySpin()
        {
        }        
        
        [Benchmark]
        public void BusySpin()
        {
            StaticBusySpin();
        }
        [Benchmark]
        public void GetTimestamp()
        {
            Stopwatch.GetTimestamp();
        }
        [Benchmark]
        public void SpinWait1()
        {
            Thread.SpinWait(1);
        }
        [Benchmark]
        public void MemoryFence()
        {
            Sse2.MemoryFence();
        }        
    }
}