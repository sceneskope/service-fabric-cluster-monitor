using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.Serilog.Seq;
using ServiceFabric.Serilog;

namespace FabricEventMonitorService
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                var logger = SeqLogger.DefaultLogger;
                SerilogEventListener.Initialise(logger);


                ServiceRuntime.RegisterServiceAsync("FabricEventMonitorServiceType",
                    context => new FabricEventMonitorService(context, logger)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(FabricEventMonitorService).Name);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
