#load "./DockerImage.cake"

public class DockerBuildMetadata
{
    public string ComposeFilePath { get; set; }

    public List<DockerImage> Images { get; set; }
}