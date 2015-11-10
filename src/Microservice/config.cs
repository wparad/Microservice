[DataContract]
public class Config
{
    [DataMember]
    public bool Setting { get; set; }
}
 
<packages>
    <package id="LibGit2Sharp" version="0.21.0.176" />
</packages>

//GET the current configuration
[OperationContract]
[WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "Configuration")]
string Configuration();
 
//Set the current configuration to be a particular sha
[OperationContract]
[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "Configuration")]
void Configuration(string sha);

public class Config
{
    public string ConfigProperty1 { get; set; }
}
 
private static string GlobalGitRepositoryLocation;
private static string LocalGitRepositoryLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SERVICENAME", Path.GetFileName(GlobalGitRepositoryLocation));
/// <summary>
/// Location of the shared git repository
/// </summary>
/// <returns></returns>
private Config GetConfig()
{
	//Run at startup, this will create the global repository, only needs to be run once.
	//Create the repository if it doesn't exist
	Repository.Init(GlobalGitRepositoryLocation, true);
 
	//Clone the repository locally 
	//Run at startup, this will create the instance specific clone on the local machine, should be run on every service start.
	//Don't need to do this more than once the first time
	if (!Repository.IsValid(LocalGitRepositoryLocation)) { Repository.Clone(GlobalGitRepositoryLocation, LocalGitRepositoryLocation, new CloneOptions { IsBare = true }); }
 
	//In application code attempt to use the git repository
	using (var repository = new Repository(LocalGitRepositoryLocation))
	{
		// Before reads and writes can be done, fetches should always be used to ensure the current clone matches what is the server.  
		//If you aren't using pull-requests to merge it is important for this to be up to date on a write.
		//If the fetch fails before a read, the service should continue on with the old version of the repository.
		try{ repository.Fetch("origin"); } catch {}
 
		//Get the latest commit from the master branch.
		var sha = repository.Refs["refs/remotes/origin/master"].TargetIdentifier;
 
		//Read from the config.json file in the repository
		var configFile = repository.Lookup<Commit>(sha)["config.json"];
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

private static void WriteConfig(Config config)
{
	using (var repository = new Repository(LocalGitRepositoryLocation))
	{
		repository.Fetch("origin");
 
		// Create a blob from the content stream
		Blob blob;
		var serializedObject = new JavaScriptSerializer().Serialize(config);
		var contentBytes = Encoding.UTF8.GetBytes(serializedObject);
		using (var memoryStream = new MemoryStream(contentBytes)) { blob = repository.ObjectDatabase.CreateBlob(memoryStream); }
 
		// Put the blob in a tree
		var treeDefinition = new TreeDefinition();
		treeDefinition.Add("config.json", blob, Mode.NonExecutableFile);
		var tree = repository.ObjectDatabase.CreateTree(treeDefinition);
 
		// Committer and author
		var committer = new Signature(string.Format("{0}@{1}", Environment.UserName, Environment.MachineName), "Team DL@Cimpress.com", DateTime.Now);
		var author = committer;
 
		//Create the commit
		var sha = repository.Refs["refs/remotes/origin/master"].TargetIdentifier;
		var parentCommit = repository.Lookup<Commit>(sha);
		var commit = repository.ObjectDatabase.CreateCommit(author, committer, "Updating config.json + More info", tree, new[]{parentCommit}, false);
 
		// Update the HEAD reference to point to the latest commit
		repository.Refs.UpdateTarget(repository.Refs.Head, commit.Id);
 
		var remote = repository.Network.Remotes["origin"];
		var pushRefSpec = "refs/heads/master";
		//Credentials can be used if needed, by hooking in here.
		repository.Network.Push(remote, repository.Refs.Head.CanonicalName, pushRefSpec);
	}
}