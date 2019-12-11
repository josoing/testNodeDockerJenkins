#module nuget:?package=Cake.DotNetTool.Module&version=0.3.1
#tool dotnet:?package=GitVersion.Tool&version=5.1.1
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Docker&version=0.9.6"
#addin "Cake.FileHelpers"
#load "./DockerBuildContext.cake"
#load "./CommonHelper.cake"

using Cake.Docker;
using Newtonsoft.Json;
using System.Net.Http;
using System.Linq;

var target = Argument("target", "BuildImages");
var environment = Argument<string>("environment", "QA");
var manifestPath = Argument("manifest", "DockerBuildManifest.json");

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SETUP Docker Build Configuration
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Setup<DockerBuildContext>(context => 
{
    var dockerBuildContext = CommonHelper.ConfigureManifest<DockerBuildContext>(context, manifestPath);    
    Information("Docker build context configuration is created by using manifest file.");
    Information("Starting running task: {0}", target);
    
    // Sending output about deployment environment if target is deploy.
    if (target.Equals("deploy", StringComparison.OrdinalIgnoreCase))
    {
        Information("Deployment enviroment is {0}", environment);
    }

    // Setting password via argument on local build.
    if(context.BuildSystem().IsLocalBuild)
    {
        if (!HasArgument("DockerRegistryPassword"))
        {
            throw new CakeException("Please provide password for docker registry by using DockerRegistryPassword argument.");
        }
        dockerBuildContext.Registry.Password = Argument<string>("DockerRegistryPassword");
    }
    // Setting password via environment variable on build server.
    else
    {
        if (!HasEnvironmentVariable("DOCKER_REGISTRY_USERNAME"))
        {
            throw new CakeException("Please provide docker registry password by using DOCKER_REGISTRY_USERNAME environment variable.");
        }

        if (!HasEnvironmentVariable("DOCKER_REGISTRY_PASSWORD"))
        {
            throw new CakeException("Please provide docker registry password by using DOCKER_REGISTRY_PASSWORD environment variable.");
        }

        dockerBuildContext.Registry.Username = EnvironmentVariable("DOCKER_REGISTRY_USERNAME");
        dockerBuildContext.Registry.Password = EnvironmentVariable("DOCKER_REGISTRY_PASSWORD");
    }

    Information("{0} will be used as Docker Registry for pulling/pushing images by user {1} and Password {2}",
        dockerBuildContext.Registry.Url,
        dockerBuildContext.Registry.Username,
        dockerBuildContext.Registry.Password);

    Information("Compose file at path {0} is going to be used for creating {1} images",
        dockerBuildContext.Build.ComposeFilePath,
        string.Join(",", dockerBuildContext.Build.Images.Select(s => s.Service).ToList()));

    return dockerBuildContext;
});


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Tasks for Docker Build
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Task("SetupVersioning")
    .Does<DockerBuildContext>(context => 
    {
        var versionOutput = GitVersion(new GitVersionSettings
        {
            NoFetch = true,
            OutputType = GitVersionOutput.Json
        });

        context.Tags.Add(versionOutput.LegacySemVer);

        Information("Tags that are prepared for image versioning: {0}", string.Join(",", context.Tags));
    });

Task("LoginDockerRegistry")
    .Does<DockerBuildContext>(context => 
    {
        Information("Trying to login Docker registry. Url : {0}, Username: {1}", context.Registry.Url, context.Registry.Username);

        DockerLogin(context.Registry.Username, context.Registry.Password, context.Registry.Url);

        Information("Successfully logged in to docker registry. Server: {0}", context.Registry.Url);
    });

Task("DockerComposeBuild")
    .Does<DockerBuildContext>(context => 
    {
        var composeFilePath = MakeAbsolute(Directory(context.Build.ComposeFilePath));
        Information("Docker images are going to be built by using {0} compose file", composeFilePath);
        DockerComposeBuild(new DockerComposeBuildSettings()
        {
            Files = new string[] { context.Build.ComposeFilePath },
            EnvironmentVariables = new Dictionary<string, string> { { "VERSION_TAG", "dev" } }
        });
    });

Task("TagDockerImages")
    .Does<DockerBuildContext>(context => 
    {
        var versionOutput = GitVersion(new GitVersionSettings
        {
            NoFetch = true,
            OutputType = GitVersionOutput.Json
        });

        var images = context.Build.Images;
        foreach (var image in images)
        {
            var imageReference = string.Format("{0}/{1}/{2}:{3}", context.Registry.Url, context.Registry.Username, image.Repository, "dev");
            if (versionOutput.BranchName.Equals("master", StringComparison.OrdinalIgnoreCase))
            {
                
                image.Tags.Add("latest");
                image.Tags.AddRange(context.Tags);

                Information("Master branch is detected, Semantic versioning is applied for {0} image. Applied Tags: {1}", image.Repository, string.Join(",", image.Tags));     
            }
            else
            {
                image.Tags.AddRange(context.Tags);

                Information("Branch is not master, branch name convention is applied for {0} image. Applied Tags: {1}", image.Repository, string.Join(",", image.Tags));     
            }

            foreach (var tag in image.Tags)
            {
                var registryReference = string.Format("{0}/{1}/{2}:{3}", context.Registry.Url, context.Registry.Username, image.Repository, tag);
                DockerTag(imageReference, registryReference);
            }
        }        
    });

Task("PushDockerImages")
    .WithCriteria(context => !context.BuildSystem().IsLocalBuild, "Pushing docker images are available only in build server.")
    .Does<DockerBuildContext>(context => 
    {
        var imagesToPush = context.Build.Images;
        foreach (var image in imagesToPush)
        {
            foreach (var tag in image.Tags)
            {
                var imageReference = string.Format("{0}/{1}/{2}:{3}", context.Registry.Url, context.Registry.Username, image.Repository, tag);
                Information("Docker Image {0} is about to be pushed to {1} with Tag: {2}",imageReference, context.Registry.Url, tag);           
                DockerPush(imageReference);
            }
        }
    });

Task("RemoveLocalDockerImages")
    .ContinueOnError()
    .Does<DockerBuildContext>(context => 
    {

        Information("Deleting not tagged images from local docker repository.");
        var deleteNotTaggedImagesDockerCommand = new ProcessArgumentBuilder()       
                    .Append(string.Format("docker rmi $(docker images -aq --filter \"dangling=true\")"));
        if (IsRunningOnUnix())
        {
            StartProcess("sudo", new ProcessSettings { Arguments = deleteNotTaggedImagesDockerCommand});
        }
        else
        {
            StartProcess("powershell", new ProcessSettings { Arguments = deleteNotTaggedImagesDockerCommand});
        }

        Information("Deleting created local docker images.");
        var deleteLocalImageDockerCommand = new ProcessArgumentBuilder()
                    .Append(string.Format("docker rmi $(docker images --filter=reference=\"{0}/{1}/*\" -aq) -f", context.Registry.Url, context.Registry.Username));
        if (IsRunningOnUnix())
        {
            StartProcess("sudo", new ProcessSettings { Arguments = deleteLocalImageDockerCommand});
        }
        else
        {
            StartProcess("powershell", new ProcessSettings { Arguments = deleteLocalImageDockerCommand});
        }
    });

Task("ModifyEnviromentFile")
    .Does<DockerBuildContext>(context => 
    {
        var environmentExistsInManifest = context.Deploy.ContainsKey(environment);
        if (!environmentExistsInManifest)
        {
            throw new CakeException($"Unable to find {environment} environment in manifest.");
        }

        var deploymentMetadata = context.Deploy[environment];
        if (string.IsNullOrWhiteSpace(deploymentMetadata.EnvFilePath))
        {
            throw new CakeException($"Enviroment file path is not provided for {environment}");
        }

        var environmentFile = FilePath.FromString(deploymentMetadata.EnvFilePath);
        ReplaceTextInFiles(deploymentMetadata.EnvFilePath, "{VERSION_TAG}", context.Tags.First());

        Information("Version tag is in {0}, is replaced with {1}", environmentFile.FullPath, context.Tags.FirstOrDefault());
    });

Task("BuildImages")
    .IsDependentOn("SetupVersioning")
    .IsDependentOn("LoginDockerRegistry")
    .IsDependentOn("DockerComposeBuild")
    .IsDependentOn("TagDockerImages")
    .IsDependentOn("PushDockerImages")
    .IsDependentOn("RemoveLocalDockerImages");


Task("Deploy")
    .IsDependentOn("SetupVersioning")
    .IsDependentOn("LoginDockerRegistry")
    .IsDependentOn("ModifyEnviromentFile");

RunTarget(target);