﻿using System.Threading.Tasks;
using ECommon.Extensions;
using ECommon.Scheduling;
using ENode.Configurations;

namespace ENode.Commanding.Impl
{
    public class DefaultCommandDispatcher : ICommandDispatcher
    {
        private readonly TaskFactory _taskFactory;
        private readonly ICommandExecutor _commandExecutor;

        public int ExecuteCommandCountOfOneTask { get; private set; }
        public int TaskMaxDeadlineMilliseconds { get; private set; }

        public DefaultCommandDispatcher(ICommandExecutor commandExecutor)
        {
            var setting = ENodeConfiguration.Instance.Setting;
            ExecuteCommandCountOfOneTask = setting.ExecuteCommandCountOfOneTask;
            TaskMaxDeadlineMilliseconds = setting.TaskMaxDeadlineMilliseconds;
            _taskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(setting.CommandProcessorParallelThreadCount));
            _commandExecutor = commandExecutor;
        }

        public void RegisterCommandForExecution(ProcessingCommand command)
        {
            _taskFactory.StartNew(() => _commandExecutor.ExecuteCommand(command));
        }
        public void RegisterMailboxForExecution(CommandMailbox mailbox)
        {
            _taskFactory.StartNew(() => TryRunMailbox(mailbox));
        }
        public void RegisterMailboxForDelayExecution(CommandMailbox mailbox, int delayMilliseconds)
        {
            _taskFactory.StartDelayedTask(delayMilliseconds, () => TryRunMailbox(mailbox));
        }

        private void TryRunMailbox(CommandMailbox mailbox)
        {
            if (mailbox.MarkAsRunning())
            {
                _taskFactory.StartNew(mailbox.Run);
            }
        }
    }
}
