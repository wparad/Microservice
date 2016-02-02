using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using LibGit2Sharp;

namespace Service
{
    /// <summary>
    /// Class to serialize, read, and write configuration to a git repository
    /// </summary>
    public class ConfigurationManager
    {
        private string GlobalGitRepositoryLocation { get; set;}
        private string LocalGitRepositoryLocation { get; set;}
        private string GitConfigurationFile { get; set;}
        private string TeamEmailAddress { get; set;}

        private const string ConfigurationRemote = "origin";
        private const string ConfigurationRemoteRef = "refs/remotes/origin/master";
        private const string ConfigurationRef = "refs/heads/master";

        /// <summary>
        /// Create a new Configuration Manager.
        /// </summary>
        /// <param name="globalGitRepositoryLocation">Location of the shared repository.</param>
        /// <param name="applicationData">The name of the service program data directory.</param>
        /// <param name="gitConfigurationFile">The location of the config file in the git repository.</param>
        /// <param name="teamEmailAddress">The git user's email address to associate with new commits.</param>
        public ConfigurationManager(string globalGitRepositoryLocation, string applicationData, string gitConfigurationFile, string teamEmailAddress)
        {
            GlobalGitRepositoryLocation = globalGitRepositoryLocation;

            LocalGitRepositoryLocation = Path.Combine(applicationData, Path.GetFileName(globalGitRepositoryLocation) ?? "configuration.git");
            GitConfigurationFile = gitConfigurationFile;
            TeamEmailAddress = teamEmailAddress;
        }

        /// <summary>
        /// Get the git configuration
        /// </summary>
        /// <returns>A Config object.</returns>
        public Config GetConfig()
        {
            //Run at startup, this will create the global repository, only needs to be run once.
            Repository.Init(GlobalGitRepositoryLocation, true);

            //Run at startup, this will create the instance specific clone on the local machine, should be run on every service start.
            //Don't need to do this more than once the first time
            if (!Repository.IsValid(LocalGitRepositoryLocation)) { Repository.Clone(GlobalGitRepositoryLocation, LocalGitRepositoryLocation, new CloneOptions { IsBare = true }); }

            //In application code attempt to use the git repository
            using (var repository = new Repository(LocalGitRepositoryLocation))
            {
                try { repository.Network.Fetch(repository.Network.Remotes[ConfigurationRemote]); } catch { }
                
                var refOriginMaster = repository.Refs[ConfigurationRemoteRef];
                if (refOriginMaster == null) { return new Config(); }

                var sha = refOriginMaster.TargetIdentifier;

                var configFile = repository.Lookup<Commit>(sha)[GitConfigurationFile];
                if (configFile == null || !(configFile.Target is Blob)) { return new Config(); }

                using (var fileStream = ((Blob)configFile.Target).GetContentStream())
                using (var reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    var result = reader.ReadToEnd();
                    try { return new JavaScriptSerializer().Deserialize<Config>(result); }
                    catch { return new Config(); }
                }
            }
        }

        /// <summary>
        /// Write a config back to the global git repository.
        /// </summary>
        /// <param name="config"></param>
        public void WriteConfig(Config config)
        {
            using (var repository = new Repository(LocalGitRepositoryLocation))
            {
                var remote = repository.Network.Remotes[ConfigurationRemote];
                repository.Network.Fetch(remote);

                // Create a blob from the content stream
                Blob blob;
                var serializedObject = new JavaScriptSerializer().Serialize(config);
                var contentBytes = Encoding.UTF8.GetBytes(serializedObject);
                using (var memoryStream = new MemoryStream(contentBytes)) { blob = repository.ObjectDatabase.CreateBlob(memoryStream); }

                // Put the blob in a tree
                var treeDefinition = new TreeDefinition();
                treeDefinition.Add(GitConfigurationFile, blob, Mode.NonExecutableFile);
                var tree = repository.ObjectDatabase.CreateTree(treeDefinition);

                // Committer and author
                var committer = new Signature(string.Format("{0}@{1}", Environment.UserName, Environment.MachineName), TeamEmailAddress, DateTime.Now);
                var author = committer;

                //Create the commit
                var refOriginMaster = repository.Refs[ConfigurationRemoteRef];
                var parentCommit = refOriginMaster != null ? new[]{repository.Lookup<Commit>(refOriginMaster.TargetIdentifier)} : new Commit[0];
                var commit = repository.ObjectDatabase.CreateCommit(author, committer, "Updating config.json", tree, parentCommit, false);

                // Update the HEAD reference to point to the latest commit
                repository.Refs.UpdateTarget(repository.Refs.Head, commit.Id);

                repository.Network.Push(remote, repository.Refs.Head.CanonicalName, ConfigurationRef);
            }
        }
    }
}