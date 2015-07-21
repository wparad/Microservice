using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace Log4NetExtensions
{
	public class TcpAppender : AppenderSkeleton
	{
        public IPAddress RemoteAddress { get; set; }
	    public int RemotePort { get; set; }
	    public Encoding Encoding { get; set; }
	    protected TcpClient Client { get; set; }
        override protected bool RequiresLayout { get { return true; } }

        public TcpAppender()
	    {
            Encoding = Encoding.Default;
            RemoteAddress = IPAddress.Loopback;
	    }

		public override void ActivateOptions()
		{
			base.ActivateOptions();

            if (RemoteAddress == null) { throw new Exception("Remote address of the location must be specified."); }

            if (RemotePort < IPEndPoint.MinPort || RemotePort > IPEndPoint.MaxPort) 
			{
				throw SystemInfo.CreateArgumentOutOfRangeException("value", RemotePort, 
                    string.Format("The value specified is less than {0} or greater than {1}.",
                    IPEndPoint.MinPort.ToString(NumberFormatInfo.InvariantInfo), IPEndPoint.MaxPort.ToString(NumberFormatInfo.InvariantInfo)));
			}

            Client = new TcpClient();
            try { Client.Connect(RemoteAddress, RemotePort); }
			catch (Exception ex) { ErrorHandler.Error("Could not initialize the TcpClient.", ex, ErrorCode.GenericFailure); }
		}

		protected override void Append(LoggingEvent loggingEvent) 
		{
			try 
		    {
                //The remote can close the connection, in that case it should be reopened.
		        if (!Client.Connected)
		        {
		            Client.Close();
                    Client = new TcpClient();
                    Client.Connect(RemoteAddress, RemotePort);
		        }

		        var buffer = Encoding.GetBytes(RenderLoggingEvent(loggingEvent).ToCharArray());
			    Client.GetStream().Write(buffer, 0, buffer.Length);
			} 
			catch (Exception ex) 
			{
				ErrorHandler.Error(string.Format("Unable to send logging event to remote host {0} on port {1}.",
                    RemoteAddress, RemotePort), ex,  ErrorCode.WriteFailure);
			}
		}

        protected override void OnClose()
        {
            if (Client != null)
            {
                Client.Close();
                Client = null;
            }
            base.OnClose();
        }
	}
}
