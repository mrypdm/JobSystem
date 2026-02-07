namespace Job.Worker.Options;

/// <summary>
/// Options for Docker
/// </summary>
public class DockerOptions
{
    /// <summary>
    /// URL to Docker API
    /// </summary>
    public string Url { get; set; } = "unix:///var/run/docker.sock";
}
