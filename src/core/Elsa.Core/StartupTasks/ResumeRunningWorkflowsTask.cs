using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.Models;
using Elsa.Queries;
using Elsa.Runtime;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    /// <summary>
    /// If there are workflows in the Running state while the server starts, it means the workflow instance never finished execution, e.g. because the workflow host terminated.
    /// This startup task resumes these workflows.
    /// </summary>
    public class ResumeRunningWorkflowsTask : IStartupTask
    {
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly IDistributedLockProvider _distributedLockProvider;

        public ResumeRunningWorkflowsTask(
            IWorkflowInstanceManager workflowInstanceManager,
            IWorkflowScheduler workflowScheduler,
            IDistributedLockProvider distributedLockProvider)
        {
            _workflowInstanceManager = workflowInstanceManager;
            _workflowScheduler = workflowScheduler;
            _distributedLockProvider = distributedLockProvider;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (!await _distributedLockProvider.AcquireLockAsync(GetType().Name, cancellationToken))
                return;

            var instances = await _workflowInstanceManager.ListByStatusAsync(WorkflowStatus.Running, cancellationToken);

            foreach (var instance in instances)
                await _workflowScheduler.ScheduleWorkflowAsync(
                    instance.WorkflowInstanceId,
                    cancellationToken: cancellationToken);
        }
    }
}