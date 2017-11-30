// ReSharper disable RedundantNameQualifier
namespace WpfAnalyzers.Benchmarks.Benchmarks
{
    public class WPF0033Benchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new WpfAnalyzers.WPF0033UseAttachedPropertyBrowsableForTypeAttribute());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void RunOnWpfAnalyzersProject()
        {
            Benchmark.Run();
        }
    }
}