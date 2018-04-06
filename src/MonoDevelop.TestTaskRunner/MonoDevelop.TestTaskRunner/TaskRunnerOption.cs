//
// Based on: https://github.com/madskristensen/TaskRunnerTemplate/blob/master/src/TaskRunnerExtension/TaskRunner/TaskRunnerOption.cs
//

using System;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace MonoDevelop.TestTaskRunner
{
	class TaskRunnerOption : ITaskRunnerOption
	{
		public TaskRunnerOption (string optionName, uint commandId, Guid commandGroup, bool isEnabled, string command)
		{
			Command = command;
			Id = commandId;
			Guid = commandGroup;
			Name = optionName;
			Enabled = isEnabled;
			Checked = isEnabled;
		}

		public string Command { get; set; }
		public bool Enabled { get; set; }
		public bool Checked { get; set; }
		public Guid Guid { get; }
		public uint Id { get; }
		public string Name { get; }
	}
}
