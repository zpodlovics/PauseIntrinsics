(Inspired by https://github.com/giltene/GilExamples/blob/master/SpinWaitTest/JEPdraft.md)

# .NET API Intrinsics Pause

## Summary

Add an Intrinsics Pause API that would allow .NET code to indicate that a spin wait loop is being 
executed.

## Goals

Provide an Intrinsics Pause API that would allow .NET code to indicate to the runtime that it is 
in a spin wait loop.

## Non-Goals

It is NOT a goal to look at performance intrinsics beyond spin wait loops. Other performance 
related intrinsics are outside the scope of this proposal. Architecture neutral (eg.: 
Thread.SpinWait()) or normalized spin wait loops are also outside of the scope of this 
proposal (eg.: YieldProcessorNormalized / OptimalMaxSpinWaitsPerSpinIteration).

## Motivation

Some hardware platforms benefit from software indication that a spin wait loop is in progress.

Possible benefits:

A) The reaction time of a spin wait loop construct may be improved when a spin wait hinting
is used due to various factors, reducing thread-to-thread latencies in spinning wait situations.

B) The power consumed by the core or hardware thread involved in the spin wait loop construct
may be reduced, benefiting overall power consumption of a program, and possibly allowing other
cores or hardware threads to execute at faster speeds within the same power consumption envelope. 

Please note just like any other instruction latency the PAUSE instruction may vary depending 
on processor architectures [8][9]:
· Intel® (Core i5-5200U) processor on Broadwell architecture: 9 cycles;
· Intel® (Core i7-6500U) processor on Skylake architecture: 140 cycles;
· Intel® processor on Cascade Lake architecture: 40 cycles.
· AMD® (Ryzen 7 3700X) processor on Zen2 architecture: 65 cycles.

While long term spinning is often discouraged as a general user-mode programming practice,
short term spinning prior to blocking is a common practice (both inside and outside of the .NET).
Furthermore, as core-rich computing platforms are commonly available, many performance and/or
latency sensitive applications use a pattern that dedicates a spinning thread to a latency
critical function [1], and may involve long term spinning as well.  

As a practical example and use case, current x86 processors support a PAUSE instruction that
can be used to indicate spinning behavior. Using a PAUSE instruction demonstrably reduces
thread-to-thread round trips. Due to it's benefits and commonly recommended use, the x86 PAUSE
instruction is commonly used in kernel spin locks, in POSIX libraries that perform heuristic
spins prior to blocking, and even by the .NET itself (mm_pause). However, due to the inability 
to hint or indicate that a .NET loop is spinning, it's benefits are not available to regular 
.NET code.

I also ported the benchmark to .NET. In simple tests [2] performed on a E5-2660 v1, measuring 
the round trip latency behavior between two threads that communicate by spinning on a volatile 
field using different spining methods (eg.: BusySpin, SpinWait(1), MemoryBarrier(), Sse2.Pause()).

In [3] the round-trip latencies were demonstrably reduced by 18-20 nsec across a wide percentile 
spectrum (from the 10%'ile to the 99.9%'ile). This reduction can represent an improvement as 
high as 35%-50% in best-case thread-to-thread communication latency. This reduction can represent
an improvement as high as 35%-50% in best-case thread-to-thread communication latency.

Eg.: when two spinning threads execute on two hardware threads that share a physical CPU
core and an L1 data cache. See example latency measurement results comparing the reaction
latency of a spin loop that includes an intrinsified Pause() call (intrinsified as
a PAUSE instruction) to the same loop executed without using a PAUSE instruction [4], along
with the measurements of the it takes to perform an actual Stopwatch.GetTimestamp() call to
measure time.

## Description

I would like to propose to add a method to the .NET Sse2 Intrinsics which would indicate 
to the runtime that a spin loop is being performed: e.g. Sse2.Pause().

## Alternatives

DllImport can be used to spin loop with a spin-loop-hinting CPU instruction, but the
DllImport-boundary crossing overhead tends to be larger than the benefit provided by
the instruction, at least where latency is concerned. 

.NET pattern machine could attempt to have the JIT compilers deduce spin-wait-loop 
situations and code and choose to automatically include a spin-loop-hinting CPU 
instructions with no .NET code hints required. We expect that the complexity of 
automatically and reliably detecting spinning situations, coupled with questions 
about potential tradeoffs in using the hints on some platform to delay the availability 
of viable implementations significantly.

## Testing

It seems that given the very small footprint of Sse2.Pause() Intrinsics API, testing 
of an intrinsified x86 implementation in .NET will also be straightforward. I would 
expect testing to focus on confirming both the code generation correctness and latency
benefits of using an intrinsic implementation of Sse2.Pause().

## Risks and Assumptions

An intrinsic x86 implementation will involve modifications to multiple .NET components and
exposing a new Sse2.Pause Intrinsics API and as such they carry some risks, but no more 
than other simple intrinsics added to the .NET.

[1] LMAX Disruptor .NET implementation [https://github.com/disruptor-net/Disruptor-net]
[2] Pause intrinsics latency and throughput benchmarks (C#) [https://github.com/zpodlovics/pauseintrinsics]
[3] Pause intrinsics latency and throughput benchmarks (Java) [https://github.com/giltene/GilExamples/tree/master/SpinWaitTest]
[4] Chart depicting onSpinWait() intrinsification impact [https://github.com/giltene/GilExamples/blob/master/SpinWaitTest/SpinLoopLatency_E5-2697v2_sharedCore.png]    
[5] .NET prototype Sse2.Pause intrinsics implementation branch [https://github.com/zpodlovics/runtime/tree/sse2pause]
[6] Implementations on other platforms (other than x86) may choose to use the same instructions as [linux cpu_relax](https://git.kernel.org/pub/scm/linux/kernel/git/stable/linux.git/tree/arch/x86/um/asm/processor.h?h=v5.10.23#n30) and/or [plasma_spin](https://github.com/gstrauss/plasma/blob/master/plasma_spin.h)
[7] https://software.intel.com/content/www/us/en/develop/articles/benefitting-power-and-performance-sleep-loops.html
[8] https://uops.info/paper.html
[9] https://uops.info/table.html?search=PAUSE&cb_lat=on&cb_tp=on&cb_uops=on&cb_ports=on&cb_SNB=on&cb_IVB=on&cb_HSW=on&cb_BDW=on&cb_SKL=on&cb_SKX=on&cb_KBL=on&cb_CFL=on&cb_CNL=on&cb_CLX=on&cb_ICL=on&cb_ZENp=on&cb_ZEN2=on&cb_measurements=on&cb_iaca30=on&cb_doc=on&cb_base=on&cb_sse=on&cb_others=on

[thread_spinwait_result]:https://raw.github.com/zpodlovics/pauseintrinsics/main/measurements/SpinWait_Histogram.png "Example Thread.SpinWait(1) Results on E5-2660v1"
[runtime_onspinwait_result]:https://raw.github.com/giltene/GilExamples/master/SpinWaitTest/SpinLoopLatency_E5-2697v2_sharedCore.png "Example Runtime.onSpinWait() Results on E5-2697v2"