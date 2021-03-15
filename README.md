(See draft Sse2.Pause() here: [https://github.com/zpodlovics/pauseintrinsics/blob/main/ApiDraft.md])
(Inspired by https://github.com/giltene/GilExamples/tree/master/SpinWaitTest)

# Pause Intrinsics

A simple thread-to-thread communication latency test that measures and reports on the
behavior of thread-to-thread ping-pong latencies when spinning using a shared volatile
field, aling with the impact of using a Sse2.Pause() call on that latency behavior.

This test can be used to measure and document the impact of Sse2.Pause() behavior
on thread-to-thread communication latencies. E.g. when the two threads are pinned to
the two hardware threads of a shared x86 core (with a shared L1), this test will
demonstrate an estimate the best case thread-to-thread latencies possible on the
platform, if the latency of measuring time with Stopwatch.GetTimestamp() is discounted
(nanoTime latency can be separtely estimated across the percentile spectrum using
the PauseIntrinsics.GetTimestamp.Benchmark.Cli test in this project).

### Example .NET results plot (two threads on a shared core on a Xeon E5-2660v1): 
![thread_spinwait_result] 

### Example Java results plot (two threads on a shared core on a Xeon E5-2697v2): 
![runtime_onspinwait_result] 

### Running:

This test is obviously intended to be run on machines with 2 or more vcores (tests on single vcore machines will
produce understandably outrageously long runtimes).
 
(If needed) Prepare the PauseIntrinsics.sln by running (.NET SDK 5.x Required):
 
    % ./build.sh

The simplest way to run SpinWait benchmark is:

    % dotnet tests/PauseIntrinsics.SpinWait.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.SpinWait.Benchmark.Cli.dll

The simplest way to run MemoryFence / Pause (assuming HAVE_PAUSE_INTRINSICS is defined for build.sh) benchmark is:

    % dotnet tests/PauseIntrinsics.Pause.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.Pause.Benchmark.Cli.dll

The simplest way to run BenchmarkDotNet benchmark for the various waiting methods is:

    % dotnet tests/PauseIntrinsics.BenchmarkDotnet.Cli/bin/Release/net5.0/PauseIntrinsics.BenchmarkDotnet.Cli.dll -f "*" -m -d

Since the test is intended to highlight the benefits of an intrinsic Sse2.Pause, using a prototype .NET that that intrinsifies 
Sse2.Pause() a PAUSE instruction, you can compare the output of:

    % dotnet tests/PauseIntrinsics.Pause.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.Pause.Benchmark.Cli.dll > Pause.hgrm

and 
    
    % dotnet tests/PauseIntrinsics.SpinWait.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.SpinWait.Benchmark.Cli.dll > SpinWait.hgrm

By plotting them both with [HdrHistogram's online percentile plotter] (http://hdrhistogram.github.io/HdrHistogram/plotFiles.html)

On moden x86-64 sockets, comparisions seem to show an 18-20nsec difference in the round trip latency.  

For consistent measurement, it is recommended that this test be executed while binding the process to specific cores. 
E.g. on a Linux system, the following command can be used:

    % taskset -c 0,1 dotnet tests/PauseIntrinsics.Pause.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.Pause.Benchmark.Cli.dll > Pause.hgrm
    
To place the spinning threads on the same core. (the choice of cores 0 and 1 is specific to a 48 vcore system where 
cores 0 and 1 represent two hyper-threads on a common core. You will want to identify a matching pair on your specific 
system. You can use lstopo or hwloc linux tools to identify the machine pairs.). You can also improve the measurement
to execute it with high(er) priority eg.:

    % nice -20 taskset -c 0,1 dotnet tests/PauseIntrinsics.Pause.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.Pause.Benchmark.Cli.dll > Pause.hgrm
 
### Plotting results:
 
SpinHintTrst outputs a percentile histogram distribution in [HdrHistogram](http://hdrhistogram.org)'s common
.hgrm format. This output can/shuld be redirected to an .hgrm file (e.g. vanilla.hgrm),
which can then be directly plotted using tools like [HdrHistogram's online percentile plotter] (http://hdrhistogram.github.io/HdrHistogram/plotFiles.html)

 
### Prototype intrinsics implementations

A prototype .NET implementation that implements Sse2.Pause as a PAUSE instruction on x86-64 is available. 

Relevant repository could be found here: 
- Runtime: [https://github.com/zpodlovics/runtime/tree/sse2pause]  

    Note: These full implementations are included for x86. Implementations on other platforms may choose to 
    use the same instructions as [linux cpu_relax](https://git.kernel.org/pub/scm/linux/kernel/git/stable/linux.git/tree/arch/x86/um/asm/processor.h?h=v5.10.23#n30) and/or [plasma_spin](https://github.com/gstrauss/plasma/blob/master/plasma_spin.h)

A downloadable working .NET SDK is work in progress.

#### Additional tests

This package includes some additional tests that can be used to explore the impact of Sse2.Pause()
behavior:

To ping pong latency test with busy wait:

    % dotnet tests/PauseIntrinsics.BusyWait.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.BusyWait.Benchmark.Cli.dll

To test spinwait pure ping pong throughput test with no latency measurement overhead:

    % dotnet tests/PauseIntrinsics.SpinWait.Throughput.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.SpinWait.Throughput.Benchmark.Cli.dll

To test pause pure ping pong throughput test with no latency measurement overhead:

    % dotnet tests/PauseIntrinsics.Pause.Throughput.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.Pause.Throughput.Benchmark.Cli.dll

To document the latency of measure time with Stopwatch.GetTimestamp() (so that it can be discounted when 
observing ping pong latecies in the latency measuring tests):

    % dotnet tests/PauseIntrinsics.GetTimestamp.Benchmark.Cli/bin/Release/net5.0/PauseIntrinsics.GetTimestamp.Benchmark.Cli.dll

[thread_spinwait_result]:https://raw.github.com/zpodlovics/pauseintrinsics/main/measurements/SpinWait_Histogram.png "Example Thread.SpinWait(1) Results on E5-2660v1"

[runtime_onspinwait_result]:https://raw.github.com/giltene/GilExamples/master/SpinWaitTest/SpinLoopLatency_E5-2697v2_sharedCore.png "Example Runtime.onSpinWait() Results on E5-2697v2"

