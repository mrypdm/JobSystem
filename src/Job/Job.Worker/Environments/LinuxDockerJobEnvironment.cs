using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Job.Worker.Models;
using Job.Worker.Options;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Environments;

/// <summary>
/// Job environment for Linux Docker
/// </summary>
public class LinuxDockerJobEnvironment(JobEnvironmentOptions options, ILogger<LinuxDockerJobEnvironment> logger)
    : IJobEnvironment
{
    /// <inheritdoc />
    public void PrepareEnvironment(RunJobModel jobModel)
    {
        ArgumentNullException.ThrowIfNull(jobModel);
        if (!string.IsNullOrWhiteSpace(jobModel.Directory))
        {
            throw new InvalidOperationException(
                $"Environment for Job has been already initialized at '{jobModel.Directory}'");
        }
        if (string.IsNullOrWhiteSpace(jobModel.Script))
        {
            throw new InvalidOperationException($"Cannot run Job '{jobModel.Id}' with empty script");
        }

        jobModel.Directory = Path.Combine(options.JobsDirectory, jobModel.Id.ToString()).Replace("\\", "/");
        if (Directory.Exists(jobModel.Directory))
        {
            Directory.Delete(jobModel.Directory, recursive: true);
        }

        var dockerFile = File.ReadAllText(Constants.DockerTemplateFileName)
            .Replace(Constants.JobIdTemplate, jobModel.Id.ToString())
            .Replace(Constants.JobCpuLimitTemplate, options.CpuUsage.ToString(CultureInfo.InvariantCulture))
            .Replace(Constants.JobRamLimitTemplate, options.MemoryUsage.ToString(CultureInfo.InvariantCulture))
            .Replace(Constants.JobDirectoryTemplate, jobModel.Directory);

        Directory.CreateDirectory(jobModel.Directory);
        File.WriteAllText(Path.Combine(jobModel.Directory, Constants.DockerFileName), dockerFile);
        File.Create(Path.Combine(jobModel.Directory, Constants.StdOutFileName)).Close();
        File.Create(Path.Combine(jobModel.Directory, Constants.StdErrFileName)).Close();

        var scriptFile = Path.Combine(jobModel.Directory, Constants.ScriptFileName);
        using var file = File.OpenWrite(scriptFile);
        using var base64Stream = new CryptoStream(file, new FromBase64Transform(), CryptoStreamMode.Write);
        using var streamWriter = new StreamWriter(base64Stream);
        streamWriter.Write(jobModel.Script);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            File.SetUnixFileMode(scriptFile,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
            File.SetUnixFileMode(Path.Combine(jobModel.Directory, Constants.StdOutFileName),
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherWrite);
            File.SetUnixFileMode(Path.Combine(jobModel.Directory, Constants.StdErrFileName),
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherWrite);
        }

        logger.LogInformation("Environment [{JobEnvironment}] prepared", jobModel.Directory);
    }

    /// <inheritdoc />
    public void ClearEnvironment(RunJobModel jobModel)
    {
        ArgumentNullException.ThrowIfNull(jobModel);

        if (string.IsNullOrWhiteSpace(jobModel.Directory))
        {
            return;
        }

        Directory.Delete(jobModel.Directory, true);
        logger.LogInformation("Environment [{JobEnvironment}] cleared", jobModel.Directory);
    }

    /// <inheritdoc />
    public JobEnvironmentOptions GetEnvironmentOptions()
    {
        return options;
    }
}
