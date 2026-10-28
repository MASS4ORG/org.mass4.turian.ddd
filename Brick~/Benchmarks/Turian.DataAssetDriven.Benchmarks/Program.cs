if (args.Contains("--frames")) { FrameComparison.Run(args); }
else if (args.Contains("--compare") || BrokerAttribution.Requested(args)) { DispatchComparison.Run(args); }
else { DataAssetEventBenchmarks.Run(); }
