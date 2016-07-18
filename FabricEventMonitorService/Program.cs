using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using SceneSkope.ServiceFabric.Seq;

namespace FabricEventMonitorService
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                var seqEventListener = SeqEventListener.Initialise();

                ServiceRuntime.RegisterServiceAsync("FabricEventMonitorServiceType",
                    context => new FabricEventMonitorService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(FabricEventMonitorService).Name);

                GC.KeepAlive(seqEventListener);
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
