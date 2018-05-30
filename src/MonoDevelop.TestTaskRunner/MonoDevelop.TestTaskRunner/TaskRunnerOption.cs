//
// Based on: https://github.com/madskristensen/TaskRunnerTemplate/blob/master/src/TaskRunnerExtension/TaskRunner/TaskRunnerOption.cs
//

using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;

namespace MonoDevelop.TestTaskRunner
{
	class TaskRunnerOption : ITaskRunnerOption
	{
		public TaskRunnerOption (string optionName, IconId icon, bool isEnabled, string command)
		{
			Command = command;
			Icon = icon;
			Name = optionName;
			Enabled = isEnabled;
			Checked = isEnabled;
		}

		public string Command { get; set; }
		public bool Enabled { get; set; }
		public bool Checked { get; set; }

		public IconId Icon { get; }
		public string Name { get; }
		public string Description { get; set; }
	}
}
