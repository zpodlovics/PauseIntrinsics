(See draft Sse2.Pause() here: [https://github.com/zpodlovics/pauseintrinsics/blob/main/ApiDraft.md])
(Inspired by https://github.com/giltene/GilExamples/tree/master/SpinWaitTest)

# Pause Intrinsics

A simple thread-to-thread communication latency and throughput tests that measures 
and reports on the behavior of thread-to-thread ping-pong latencies when spinning 
using a shared volatile field, align with the impact of using a Sse2.Pause() call 
on that latency behavior.

This test can be used to measure and document the impact of Sse2.Pause() behavior
on thread-to-thread communication latencies. E.g. when the two threads are pinned 
to the two hardware threads of a shared x86 core (with a shared L1), this test will
demonstrate an estimate the best case thread-to-thread latencies possible on the
platform, if the latency of measuring time with Stopwatch.GetTimestamp() is discounted
(nanoTime latency can be separately estimated across the percentile spectrum using
the PauseIntrinsics.GetTimestamp.Benchmark.Cli test in this project).

### Example .NET results plot (two threads on a shared core on a Xeon E5-2660v1 [2]):
![thread_spinwait_result] 

### Example Java results plot from [1] (two threads on a shared core on a Xeon E5-2697v2): 
![runtime_onspinwait_result] 

### Running:

This test is obviously intended to be run on machines with 2 or more vcores (tests on single 
vcore machines will produce understandably outrageously long runtimes).
 
(If needed) Prepare the PauseIntrinsics.sln by running (.NET SDK 5.x Required):
 
    % ./publish.sh

The simplest way to run SpinWait benchmark is:

    % ./artifacts/PauseIntrinsics.SpinWait.Benchmark.Cli/PauseIntrinsics.SpinWait.Benchmark.Cli

The simplest way to run MemoryFence / Pause (assuming HAVE_PAUSE_INTRINSICS is defined for build.sh) 
benchmark is:

    % ./artifacts/PauseIntrinsics.Pause.Benchmark.Cli/PauseIntrinsics.Pause.Benchmark.Cli

The simplest way to run BenchmarkDotNet benchmark for the various waiting methods is (.NET SDK 5.x required):

    % ./artifacts/PauseIntrinsics.BenchmarkDotnet.Cli/PauseIntrinsics.BenchmarkDotnet.Cli -f "*" -m -d

Since the test is intended to highlight the benefits of an intrinsic Sse2.Pause, using a prototype 
.NET that that intrinsifies Sse2.Pause() a PAUSE instruction, you can compare the output of:

    % ./artifacts/PauseIntrinsics.Pause.Benchmark.Cli/PauseIntrinsics.Pause.Benchmark.Cli > Pause.hgrm

and 
    
    % ./artifacts/PauseIntrinsics.SpinWait.Benchmark.Cli/PauseIntrinsics.SpinWait.Benchmark.Cli > SpinWait.hgrm

By plotting them both with [HdrHistogram's online percentile plotter] 
(http://hdrhistogram.github.io/HdrHistogram/plotFiles.html)

On modern x86-64 sockets, comparisons seem to show an 18-20nsec difference in the round trip 
latency. 

For consistent measurement, it is recommended that this test be executed while binding the 
process to specific cores.  E.g. on a Linux system, the following command can be used:

    % taskset -c 0,1 ./artifacts/PauseIntrinsics.Pause.Benchmark.Cli/PauseIntrinsics.Pause.Benchmark.Cli > Pause.hgrm
    
To place the spinning threads on the same core. (the choice of cores 0 and 1 is specific to a 32 
vcore system where cores 0 and 1 represent two hyper-threads on a common core. You will want to 
identify a matching pair on your specific system. You can use lstopo or hwloc linux tools to 
identify the machine pairs.). You can also improve the measurement to execute it with high(er) 
priority eg.:

    % nice -20 taskset -c 0,1 ./artifacts/PauseIntrinsics.Pause.Benchmark.Cli/PauseIntrinsics.Pause.Benchmark.Cli > Pause.hgrm
 
### Plotting results:
 
PauseIntrinsics outputs a percentile histogram distribution in [HdrHistogram](http://hdrhistogram.org)'s 
common.hgrm format. This output can/should be redirected to an .hgrm file (e.g. SpinWait.hgrm), which 
can then be directly plotted using tools like [HdrHistogram's online percentile plotter] 
(http://hdrhistogram.github.io/HdrHistogram/plotFiles.html)
 
### Prototype intrinsics implementations

A prototype .NET implementation that implements Sse2.Pause as a PAUSE instruction on x86-64 is available. 

Relevant repository could be found here: 
- Runtime: [https://github.com/zpodlovics/runtime/tree/sse2pause]  

Please note: These full implementations are included for x86. Implementations on other platforms may choose to 
use the same instructions as [linux cpu_relax](https://git.kernel.org/pub/scm/linux/kernel/git/stable/linux.git/tree/arch/x86/um/asm/processor.h?h=v5.10.23#n30) and / or [plasma_spin](https://github.com/gstrauss/plasma/blob/master/plasma_spin.h)

A non-official, non-validated, non-compatible proof of concept .NET SDK will be available for benchmarking 
the pause intrinsics in source (spec and patch file for CentOS8 dotnet5 package) form. WARNING: due the 
fixed public api surface an existing api call (Sse2.MemoryFence()) will emit PAUSE instruction instead of 
the MFENCE instruction.

#### Additional tests

This package includes some additional tests that can be used to explore the impact of Sse2.Pause()
behavior:

To ping pong latency test with busy wait:

    % ./artifacts/PauseIntrinsics.BusyWait.Benchmark.Cli/PauseIntrinsics.BusyWait.Benchmark.Cli

To test busy wait pure ping pong throughput test with no latency measurement overhead:

    % ./artifacts/PauseIntrinsics.BusyWait.Throughput.Benchmark.Cli/PauseIntrinsics.BusyWait.Throughput.Benchmark.Cli

To test spin wait pure ping pong throughput test with no latency measurement overhead:

    % ./artifacts/PauseIntrinsics.SpinWait.Throughput.Benchmark.Cli/PauseIntrinsics.SpinWait.Throughput.Benchmark.Cli

To test pause wait pure ping pong throughput test with no latency measurement overhead:

    % ./artifacts/PauseIntrinsics.Pause.Throughput.Benchmark.Cli/PauseIntrinsics.Pause.Throughput.Benchmark.Cli

To document the latency of measure time with Stopwatch.GetTimestamp() (so that it can be discounted 
when  observing ping pong latencies in the latency measuring tests):

    % ./artifacts/PauseIntrinsics.GetTimestamp.Benchmark.Cli/PauseIntrinsics.GetTimestamp.Benchmark.Cli

[1] [https://github.com/giltene/GilExamples/tree/master/SpinWaitTest]
[2] Using CentOS8 4.18.0-240.15.1.el8_3.x86_64 with SMT disabled and using all spectre / meltdown / related mitigations enabled by default.

[thread_spinwait_result]:https://raw.github.com/zpodlovics/pauseintrinsics/main/measurements/SandyBridge_Latency.png "Example Thread.SpinWait(1) Results on E5-2660v1"

[runtime_onspinwait_result]:https://raw.github.com/giltene/GilExamples/master/SpinWaitTest/SpinLoopLatency_E5-2697v2_sharedCore.png "Example Runtime.onSpinWait() Results on E5-2697v2"