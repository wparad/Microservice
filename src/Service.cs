using System;
using System.Linq;
using System.ServiceProcess;
using Contracts;

namespace Service
{
    static class Service : IService
    {

        /// <summary>
        /// This should be the package name of the service package being installed. Using this convention configuration can be read from the file system.
        /// An alternative strategy would be to pass into the service a parameter that could be the configuration file location.
        /// </summary>
        private static readonly string PackageName = Assembly.GetExecutingAssembly().GetName().Name;

        /// <summary>
        /// The main configuration should always be stored in the program data directory. Consider this directory the global source of truth that should never be changed by the service.
        /// </summary>
        private static readonly string ProgramDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), PackageName);

        /// <summary>
        /// Temporary path to store temp files.
        /// </summary>
        private static readonly string TempDirectory = Path.Combine(ProgramDataDirectory, "tmp");
        /// <summary>
        /// This is the local service read/write location, use this for any caching that is needed
        /// </summary>
        private static readonly string LocalApplicationDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PackageName);
        static void ConfigSetup(string[] args)
        {
            var globalGitRepositoryLocation = GetGitPathFromConfigFile(Path.Combine(ProgramDataDirectory, "environment.config.json"));

            var manager = new ConfigurationManager(globalGitRepositoryLocation , LocalApplicationDataDirectory, "config.json", "Team DL@Company.com");
            var config = manager.GetConfig();
            manager.WriteConfig(new Config { ConfigProperty1 = "Value1", ConfigProperty2 = true, ConfigProperty3 = 3});
        }

        private static string GetGitPathFromConfigFile(string configFileLocation)
        {
            const string configurationRepository = "configuration.git";
            if (!File.Exists(configFileLocation)) {return Path.Combine(ProgramDataDirectory, configurationRepository);}
            var location = Convert.ToString(new JavaScriptSerializer().Deserialize<IDictionary<string, object>>(File.ReadAllText(configFileLocation))["git"]);
            return !string.IsNullOrEmpty(location) ? location : Path.Combine(TempDirectory, configurationRepository);
        }
    }
}