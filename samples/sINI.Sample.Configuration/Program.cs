using Microsoft.Extensions.Configuration;
using sINI.Configuration;

var config = new ConfigurationBuilder()
	.AddConfigFile("some.config")
	.Build();

var debugView = config.GetDebugView();
Console.WriteLine(debugView);