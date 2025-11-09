namespace Job.Worker.Environments;

/// <summary>
/// Constants for Job environment
/// </summary>
public static class Constants
{
    /// <summary>
    /// Name of stdout log file
    /// </summary>
    public const string StdOutFileName = "stdout.txt";

    /// <summary>
    /// Name of stderr log file
    /// </summary>
    public const string StdErrFileName = "stderr.txt";

    /// <summary>
    /// Name of stderr log file
    /// </summary>
    public const string JobResultsFileName = "results.zip";

    /// <summary>
    /// Name of script file
    /// </summary>
    public const string ScriptFileName = "run.sh";

    /// <summary>
    /// Name of docker file
    /// </summary>
    public const string DockerFileName = "docker-compose.yaml";

    /// <summary>
    /// Name of docker template file
    /// </summary>
    public const string DockerTemplateFileName = "job.template";

    /// <summary>
    /// Template placeholder for Job Id
    /// </summary>
    public const string JobIdTemplate = "<JOB_ID>";

    /// <summary>
    /// Template placeholder for Job CPU limit
    /// </summary>
    public const string JobCpuLimitTemplate = "<JOB_CPU>";

    /// <summary>
    /// Template placeholder for Job RAM limit
    /// </summary>
    public const string JobRamLimitTemplate = "<JOB_MEMORY>";

    /// <summary>
    /// Template placeholder for Job directory
    /// </summary>
    public const string JobDirectoryTemplate = "<JOB_DIR>";
}
