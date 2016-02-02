using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using DevIntegration.WcfService;

namespace Service
{
    public class ServiceShell <TContract, TService> where TService : TContract
    {
        public event EventHandler<EventArgs> AfterStartEvent;
        public event EventHandler<EventArgs> BeforeStopEvent; 

        private ServiceHost _serviceHost;
        public void Start()
        {
            try
            {
                var addressUri = typeof(TContract).GetAttributeValue((ZServiceAttribute a) => a.EndpointAddress);
                var port = typeof(TContract).GetAttributeValue((ZServiceAttribute a) => a.Port);

                var endpointAddress = string.Format("http://{0}:{1}/{2}", Environment.MachineName, port, addressUri);
                var @namespace = typeof(TContract).GetAttributeValue((ServiceContractAttribute a) => a.Namespace);
                _serviceHost = new ServiceHost(typeof(TService), new Uri(endpointAddress));

                _serviceHost.AddServiceEndpoint(typeof(TContract), new BasicHttpBinding
                {
                    Namespace = @namespace,
                    MaxBufferPoolSize = int.MaxValue,
                    MaxBufferSize = int.MaxValue,
                    MaxReceivedMessageSize = int.MaxValue,
                    ReaderQuotas =
                    {
                        MaxArrayLength = int.MaxValue,
                        MaxBytesPerRead = int.MaxValue,
                        MaxDepth = int.MaxValue,
                        MaxNameTableCharCount = int.MaxValue,
                        MaxStringContentLength = int.MaxValue
                    }
                }, "");
                _serviceHost.AddServiceEndpoint(typeof(TContract), new WebHttpBinding
                {
                    Namespace = @namespace,
                    MaxBufferPoolSize = int.MaxValue,
                    MaxBufferSize = int.MaxValue,
                    MaxReceivedMessageSize = int.MaxValue,
                    ReaderQuotas =
                    {
                        MaxArrayLength = int.MaxValue,
                        MaxBytesPerRead = int.MaxValue,
                        MaxDepth = int.MaxValue,
                        MaxNameTableCharCount = int.MaxValue,
                        MaxStringContentLength = int.MaxValue
                    }
                }, "api").Behaviors.Add(new WebHttpBehavior
                {
                    HelpEnabled = true,
                    DefaultOutgoingResponseFormat = WebMessageFormat.Json,
                    DefaultOutgoingRequestFormat = WebMessageFormat.Json
                });
                _serviceHost.Description.Behaviors.OfType<ServiceDebugBehavior>().ToList().ForEach(b => b.IncludeExceptionDetailInFaults = true);
                _serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });

                _serviceHost.Open();
            }
            catch
            {
                Log(String.Format("Problem starting service: {0} : {1}", typeof(TService), typeof(TContract)));
                throw;
            }
            finally { if (AfterStartEvent != null) { try { AfterStartEvent(_serviceHost, EventArgs.Empty); } catch {}} }
        }

        public void Stop()
        {
            if (BeforeStopEvent != null) { try { BeforeStopEvent(_serviceHost, EventArgs.Empty); } catch {}}
            if (_serviceHost != null)
            {
                _serviceHost.Close();
                _serviceHost = null;
            }
        }

        private static void Log(String str)
        {
            var logStr = String.Format("{0} {1}\n", DateTime.Now.ToString("o"), str);
            Console.WriteLine(logStr);

            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                typeof (TContract).GetAttributeValue((ZServiceAttribute a) => a.Directory));
            if(!Directory.Exists(logDirectory)) { Directory.CreateDirectory(logDirectory); }
            var windowsServiceLog = Path.Combine(logDirectory, "windows_service_log.txt");
            File.AppendAllText(windowsServiceLog, logStr);
        }
    }
}
