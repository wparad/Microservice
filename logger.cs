var layout = new RedisLayout();
var appender = new RedisAppender{ RemoteAddress = "redis.service.com", RemotePort = 6379, Layout = layout }; //RemoteAddress should be specific to datacenter
layout.ActivateOptions();
appender.ActivateOptions();
log4net.Config.BasicConfigurator.Configure(appender);

public class MessageTypeV1
{
       public string Message { get; set; }
       public Info Info { get; set; }
}
 
public class Info
{
       public string Field1 { get; set; }
       public int Field2 { get; set; }
}
 
var logger = LogManager.GetLogger(typeof(MyServiceName));
logger.Info(new MessageTypeV1
{
       //Please pass the type field in here, or dynamically add it in your TimberWinRLayout.cs file.  If this is not added, you will lose messages from ElasticSearch.
       type = typeof(MessageTypeV1),
       Message = "This is example message",
       Info = new Info{ Field1 = "A", Field2 = 2 },
});


using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using log4net.Core;
using log4net.Layout;
 
namespace Service.Logging
{
    [JsonObject]
    public class RedisJsonLog
    {
        [JsonProperty]
        public string Team { get; set; }
 
        [JsonProperty]
        public string Environment { get; set; }
 
        [JsonProperty]
        public string Host { get; set; }
 
        [JsonProperty("@timestamp")] //Elasticsearch special field
        public string TimeStamp { get; set; }
 
        [JsonProperty("type")] //Elasticsearch special field
        public string Type { get; set; }
 
        [JsonProperty]
        public object Data { get; set; }

        [JsonProperty]
        public object ExceptionData { get; set; }
 
        [JsonProperty]
        public string Level { get; set; }
 
        [JsonProperty]
        public string Application { get; set; }
 
        [JsonProperty]
        public string Executable { get; set; }
 
        [JsonProperty]
        public IDictionary Properties { get; set; }
    }
 
    public class RedisLayout : LayoutSkeleton
    {
        public JsonSerializer JsonSerializer = new JsonSerializer { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
 
        public override void ActivateOptions() { }
 
        //Do not write out exception message after the json to the writer
        public override bool IgnoresException { get { return false; } }
 
        public override void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            JsonSerializer.Serialize(writer, new RedisJsonLog
            {
                Team = "Team",
                Host = Dns.GetHostEntry("").HostName,
                TimeStamp = DateTimeOffset.Now.ToString("o"),
                Type = loggingEvent.LoggerName,
                Data = loggingEvent.MessageObject,
                ExceptionData = loggingEvent.ExceptionObject,
                Level = loggingEvent.Level.DisplayName,
                Application = loggingEvent.Domain,
                Executable = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).FullName,
                Properties = loggingEvent.GetProperties()
            });
        }
    }
}

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Redis;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
 
namespace Logging.RemoteLogging
{
    public class RedisAppender : AppenderSkeleton
    {
        override protected bool RequiresLayout { get { return true; } }
 
        public string RemoteAddress {get; set;}
        public int RemotePort { get; set; }
        private string RemoteUrl { get { return string.Format("{0}:{1}", RemoteAddress, RemotePort); } }
 
        private ConnectionMultiplexer Client { get; set; }
        private object connectionMultiplexerLockObject = new object();
        private Task<ConnectionMultiplexer> GetConnection()
        {
            
        }
 
        public override void ActivateOptions()
        {
            base.ActivateOptions();
 
            if (RemoteAddress == null) { throw new ArgumentException("Remote address of the location must be specified."); }
 
            if (RemotePort < IPEndPoint.MinPort || RemotePort > IPEndPoint.MaxPort) 
            {
                    throw SystemInfo.CreateArgumentOutOfRangeException("value", RemotePort, 
                        string.Format("The value specified is less than {0} or greater than {1}.",
                            IPEndPoint.MinPort.ToString(NumberFormatInfo.InvariantInfo), IPEndPoint.MaxPort.ToString(NumberFormatInfo.InvariantInfo)));
            {
        }
 
        protected override void Append(LoggingEvent loggingEvent)
        {   
		Task.Run(() =>
		{            
			lock(connectionMultiplexerLockObject)
			{
				if (Client == null || !Client.IsConnected)
				{
					CloseConnectionMultiplexer(Client, true);
					Client = ConnectionMultiplexer.Connect(RemoteUrl);			
				}
				Client.GetDatabase().ListRightPush("logstash", new RedisValue[] { RenderLoggingEvent(loggingEvent) }));
			}
		}).ContinueWith(t => 
                    ErrorHandler.Error(string.Format("Unable to send logging event to Redis host ({0}).", RemoteUrl), t.Exception, ErrorCode.WriteFailure),
                    TaskContinuationOptions.OnlyOnFaulted);
        }
 
        protected override void OnClose()
        {
		CloseConnectionMultiplexer(Client, false);
            	base.OnClose();
        }
 
        private void CloseConnectionMultiplexer(ConnectionMultiplexer connectionMultiplexer, bool allowFinish)
        {
            	if (connectionMultiplexer != null) { return; }

		connectionMultiplexer.CloseAsync(allowFinish).ContinueWith(t =>
	            ErrorHandler.Error(string.Format("Unable to successfully close the connection to the Redis host ({0}).", RemoteUrl), exception, ErrorCode.WriteFailure), TaskContinuationOptions.OnlyOnFaulted)
        }
    }
}