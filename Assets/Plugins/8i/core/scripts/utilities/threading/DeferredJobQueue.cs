using System.Collections.Generic;
using System.Threading;

namespace HVR
{
    public class DeferredJobQueue
    {
        public static DeferredJobQueue instance;
        public DeferredJobQueue()
        {
            instance = this;
        }

        Mutex queueLock = new Mutex();
        public List<Job> jobQueue = new List<Job>();
        public List<Job> otherJobQueue = new List<Job>();

        public void AddJob(Job job)
        {
            queueLock.WaitOne();
            jobQueue.Add(job);
            queueLock.ReleaseMutex();
        }

        public void Update()
        {
            // Copy list to temp, clear old
            queueLock.WaitOne();
            otherJobQueue.Clear();
            List<Job> processQueue = jobQueue;
            jobQueue = otherJobQueue;
            otherJobQueue = processQueue;
            jobQueue.Clear();
            queueLock.ReleaseMutex();

            // Process Jobs in order
            for (var i = 0; i < processQueue.Count; ++i)
            {
                Job job = processQueue[i];
                job.Run();
            }
        }
    }
}
