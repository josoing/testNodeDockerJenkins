#load "./DockerRegistry.cake"
#load "./DockerBuildMetadata.cake"
#load "./DockerDeployMetadata.cake"

using System.Collections.Generic;

public class DockerBuildContext
{
    public DockerBuildContext()
    {
        Tags = new List<string>();
    }

    public DockerRegistry Registry {get; set;}

    public DockerBuildMetadata Build {get; set;}

    public Dictionary<string, DockerDeployMetadata> Deploy { get; set; }

    public List<string> Tags {get; set;}
}