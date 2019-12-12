#tool dotnet:?package=GitVersion.Tool&version=5.1.1
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Json&version=3.0.1"

using Cake.Core.IO;
using Cake.Json;

public static class CommonHelper 
{
    public static T ConfigureManifest<T>(ICakeContext context, string path) where T : class
    {
        var filePath = FilePath.FromString(path);
        if(!context.FileExists(path))
        {
            throw new CakeException(
                string.Format("Unable to find manifest file on {0}. Either create a file on that path or provide where manifest file path by using manifest argument",filePath.FullPath));
        }

        return context.DeserializeJsonFromFile<T>(path);
    }

    public static GitVersion GetGitVersioningDetails(ICakeContext context)
    {
        return context.GitVersion(new GitVersionSettings()
        {
            NoFetch = true,
            OutputType = GitVersionOutput.Json
        });
    }
}