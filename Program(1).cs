using System;
using System.Linq;
using System.ServiceProcess;
using DevIntegration.WcfService;
using Topshelf;
using Contracts;

namespace Service.Main
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("Running with args: {0}", args.Length < 1 ? "Empty" : args.Aggregate((seq, next) => string.Format("{0}, {1}", seq, next)));
            using (var serviceName = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "Service Name" || s.ServiceName == "ServiceName"))
            {
#if DEBUG              
				if (serviceName != null && serviceName.Status == ServiceControllerStatus.Running) { serviceName.Stop(); }
#endif
            }

            HostFactory.Run(config =>
            {
                config.SetServiceName("ServiceName");
                config.SetDisplayName("Service Name");
                config.SetDescription("Service");

                config.Service<ServiceShell<IService, Service>>(service =>
                {
                    service.ConstructUsing(() => new ServiceShell<IService, Service>());
                    service.WhenStarted(s =>
                    {
                        s.Start();
                        var service = ServiceClient<IService>.ServiceCreator("localhost");
                        service.ValidateServer();
                    });
                    service.WhenStopped(s =>
                    {
                        var service = ServiceClient<IService>.ServiceCreator("localhost");
                        service.GetDeployments().Where(d => service.GetDeployment(d).TaskStatus == ExitCode.Running).ToList().ForEach(d => service.Cancel(d));
                        s.Stop();
                    });
                });

                config.StartAutomatically();
                config.RunAsLocalSystem();

                config.EnableServiceRecovery(rc =>
                {
                    // restart the service after 1 minute
                    rc.RestartService(0).RestartService(0).RestartService(0);
                    // set the reset interval to one day
                    rc.SetResetPeriod(0);
                });
            });
        }
    }
}
