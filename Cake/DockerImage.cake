public class DockerImage 
{
    public DockerImage()
    {
        Tags = new List<string>();
    }

    public string Service {get; set;}

    public string Repository {get; set;}

    public List<string> Tags { get; set; }
}