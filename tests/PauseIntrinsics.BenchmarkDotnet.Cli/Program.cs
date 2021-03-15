// ---------------------------------------------------------------------------
// Copyright (c) 2021, Zoltan Podlovics, KP-Tech Kft. All Rights Reserved.
//
// Licensed under the MIT License. See LICENSE.TXT in the 
// project root for license information.
// ---------------------------------------------------------------------------

using System.Runtime;
using BenchmarkDotNet.Running;

namespace PauseIntrinsics.BenchmarkDotNet.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            
            var types = new[] {typeof(Benchmark)};
            BenchmarkSwitcher.FromTypes(types).Run(args);
        }
    }
}