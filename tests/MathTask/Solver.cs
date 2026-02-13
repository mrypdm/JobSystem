using System.Runtime.CompilerServices;

namespace MathTask;

/// <summary>
/// Solver for Job queue
/// </summary>
public class Solver(Estimator estimator, Job[] jobs)
{
    /// <summary>
    /// Creating timeline of <see cref="jobs"/> execution with metrics
    /// </summary>
    public (SortedList<JobEvent, Job> Events, List<Metric> Metrics) CreateTimeline()
    {
        var jobEvents = CreateQueue();
        var waitForStart = new Queue<Job>();
        var metrics = new List<Metric>();

        for (var index = 0; index != jobEvents.Count; ++index)
        {
            var (jobEvent, currentJob) = jobEvents.ElementAt(index);
            var time = jobEvent.Time;

            if (jobEvent.Type == JobEventType.Finish)
            {
                estimator.FinishJob(currentJob);
            }

            if (jobEvent.Type == JobEventType.Create)
            {
                estimator.CreateJob();
                waitForStart.Enqueue(currentJob);
            }

            // We do not prioritize tasks based on resource consumption because the current implementation of
            // task distribution is a simple message queue using Kafka
            if (waitForStart.TryPeek(out var jobToRun) && estimator.CanRunJob(jobToRun))
            {
                _ = waitForStart.Dequeue();
                jobEvents.Add(JobEvent.Start(time), jobToRun);
                jobEvents.Add(JobEvent.Finish(time + jobToRun.Timeout), jobToRun);
                estimator.StartJob(jobToRun);
            }

            metrics.Add(estimator.GetStat(time));
        }

        return (jobEvents, metrics);
    }

    /// <summary>
    /// Creates initial queue for Jobs
    /// </summary>
    private SortedList<JobEvent, Job> CreateQueue()
    {
        var jobEvents = new SortedList<JobEvent, Job>();
        foreach (var job in jobs)
        {
            jobEvents.Add(JobEvent.Create(job.CreatedAt), job);
        }

        return jobEvents;
    }
}
