using System;

// Stub attributes to satisfy Benchmark runner requirements when VS diagnoser package is unavailable.
// These attributes are no-op and allow the project to compile and the runner to detect the expected names.
namespace Microsoft.VSDiagnostics
{
 [AttributeUsage(AttributeTargets.Class)]
 public sealed class CPUUsageDiagnoserAttribute : Attribute { }

 [AttributeUsage(AttributeTargets.Class)]
 public sealed class DotNetObjectAllocDiagnoserAttribute : Attribute { }
}
