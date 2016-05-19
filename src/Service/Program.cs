using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using Topshelf;

namespace Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var ServiceName = "ServiceName";
            Console.WriteLine("Running with args: {0}", args.Length < 1 ? "Empty" : args.Aggregate((seq, next) => string.Format("{0}, {1}", seq, next)));
            using (var serviceName = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(ServiceName)))
            {
#if DEBUG              
				if (serviceName != null && serviceName.Status == ServiceControllerStatus.Running) { serviceName.Stop(); }
#endif
            }

            /*
                var layout = new RedisLayout();
                var appender = new RedisAppender { RemoteAddress = "redis.service.com", RemotePort = 6379, Layout = layout }; //RemoteAddress should be specific to datacenter
                layout.ActivateOptions();
                appender.ActivateOptions();
                log4net.Config.BasicConfigurator.Configure(appender);

                var logger = LogManager.GetLogger(typeof(MyServiceName));
                logger.Info(new MessageType
                {
                       Type = typeof(MessageType),
                       Message = "This is example message",
                       Info = new Info{ Field1 = "A", Field2 = 2 },
                });
            */

            HostFactory.Run(config =>
            {
                config.SetServiceName(ServiceName);
                config.SetDisplayName("Service Display Name");
                config.SetDescription("Service Description");

                config.Service<ServiceShell>(serviceConfiguration =>
                {
                    serviceConfiguration.ConstructUsing(() => new ServiceShell());
                    serviceConfiguration.WhenStarted(s =>
                    {
                        s.Start();
                        Console.WriteLine("Service has started.");
                    });
                    serviceConfiguration.WhenStopped(s =>
                    {
                        Console.WriteLine("Service about to stop");
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
            //System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(-1));
        }
    }
}
