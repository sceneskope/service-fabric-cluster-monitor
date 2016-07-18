using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace FabricEventMonitorService
{
    internal sealed class FabricEventMonitorService : StatelessService
    {
        const string ListenerName = "FabricClusterMonitor";

        public FabricEventMonitorService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using (var session = new TraceEventSession(ListenerName))
            {
                session.Source.Dynamic.All += Dynamic_All;
                session.EnableProvider("Microsoft-ServiceFabric", providerLevel: TraceEventLevel.Verbose, matchAnyKeywords: 0x4000000000000000);
                session.EnableProvider("Microsoft-ServiceFabric-Actors", providerLevel: TraceEventLevel.Verbose);
                session.EnableProvider("Microsoft-ServiceFabric-Services", providerLevel: TraceEventLevel.Verbose);
                try
                {
                    await Task.Factory.StartNew(session.Source.Process, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                finally
                {
                    session.Source.StopProcessing();
                }
            }
        }

        private static MessageTemplate template = CreateTemplate();
        private static MessageTemplate CreateTemplate()
        {
            var parser = new MessageTemplateParser();
            var parsed = parser.Parse("{Message}");
            return parsed;
        }

        private static string ConvertPropertyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            else
            {
                if (char.IsLower(name[0]))
                {
                    var array = name.ToCharArray();
                    array[0] = char.ToUpperInvariant(array[0]);
                    return new string(array);
                }
                else
                {
                    return name;
                }
            }
        }

        private void Dynamic_All(TraceEvent obj)
        {
            try
            {
                var level = ConvertLevel(obj.Level);
                var properties = obj.PayloadNames.Select((name, index) => new LogEventProperty(ConvertPropertyName(name), new ScalarValue(obj.PayloadValue(index))));
                var allProperties = properties.Concat(new[] {
                    new LogEventProperty("Source", new ScalarValue("FabricEventMonitorService")),
                    new LogEventProperty("Message", new ScalarValue(obj.FormattedMessage))
                });

                var logEvent = new LogEvent(new DateTimeOffset(obj.TimeStamp), level, null, template, allProperties);
                Log.Write(logEvent);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error writing {msg}", obj);
            }
        }

        private static LogEventLevel ConvertLevel(TraceEventLevel level)
        {
            switch (level)
            {
                case TraceEventLevel.Critical: return LogEventLevel.Fatal;
                case TraceEventLevel.Error: return LogEventLevel.Error;
                case TraceEventLevel.Warning: return LogEventLevel.Warning;
                case TraceEventLevel.Informational: return LogEventLevel.Information;
                default:
                return LogEventLevel.Verbose;
            }

        }
    }
}