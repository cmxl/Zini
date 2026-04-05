using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(ConfigParserBenchmarks).Assembly).Run(args);
