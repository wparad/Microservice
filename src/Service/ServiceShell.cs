using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using Nancy.Hosting.Self;

namespace Service
{
    public class ServiceShell
    {
        public event EventHandler<EventArgs> AfterStartEvent;
        public event EventHandler<EventArgs> BeforeStopEvent; 

        private NancyHost _serviceHost;
        public void Start()
        {
            try
            {
                var port = 80;
                Log(string.Format("Service Started listening on port: {0}", port));
                _serviceHost = new NancyHost(new HostConfiguration { RewriteLocalhost = true, UrlReservations = { CreateAutomatically = true } }, new[]
                {
                    new Uri(string.Format("http://{0}:{1}", "127.0.0.1", port)),
                    new Uri(string.Format("http://{0}:{1}", "localhost", port)),
                    new Uri(string.Format("http://{0}:{1}", Dns.GetHostName(), port))

                });
                var task = Task.Factory.StartNew(() => _serviceHost.Start());
            }
            catch
            {
                Log(String.Format("Problem starting service."));
                throw;
            }
            finally { if (AfterStartEvent != null) { try { AfterStartEvent(_serviceHost, EventArgs.Empty); } catch {}} }
        }

        public void Stop()
        {
            if (BeforeStopEvent != null) { try { BeforeStopEvent(_serviceHost, EventArgs.Empty); } catch {}}
            if (_serviceHost != null)
            {
                _serviceHost.Stop();
                _serviceHost = null;
            }
        }

        private static void Log(String str)
        {
            var logStr = String.Format("{0} {1}\n", DateTime.Now.ToString("o"), str);
            Console.WriteLine(logStr);

            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "InventoryManager", "logs");
            if(!Directory.Exists(logDirectory)) { Directory.CreateDirectory(logDirectory); }
            var windowsServiceLog = Path.Combine(logDirectory, "windows_service_log.txt");
            File.AppendAllText(windowsServiceLog, logStr);
        }
    }
}
