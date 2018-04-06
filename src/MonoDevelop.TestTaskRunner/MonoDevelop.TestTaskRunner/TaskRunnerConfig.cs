//
// Based on: https://github.com/madskristensen/TaskRunnerTemplate/blob/master/src/TaskRunnerExtension/TaskRunner/TaskRunnerConfig.cs
//

using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;

namespace MonoDevelop.TestTaskRunner
{
	class TaskRunnerConfig : ITaskRunnerConfig
	{
		public TaskRunnerConfig(ITaskRunnerNode hierarchy)
		{
			TaskHierarchy = hierarchy;
		}

		public ITaskRunnerNode TaskHierarchy { get; private set; }

		public void Dispose()
		{
		}

		public string LoadBindings(string configPath)
		{
			string bindingPath = configPath + ".bindings";

			if (File.Exists (bindingPath)) {
				foreach (var line in File.ReadAllLines (bindingPath)) {
					if (line.StartsWith ("///<binding"))
						return line.TrimStart ('/').Trim ();
				}
			}

			return "<binding />";
		}

		public bool SaveBindings (string configPath, string bindingsXml)
		{
			string bindingPath = configPath + ".bindings";

			try {
				var sb = new StringBuilder ();

				if (File.Exists(bindingPath)) {
					var lines = File.ReadAllLines(bindingPath);

					foreach (var line in lines) {
						if (!line.TrimStart ().StartsWith ("///<binding", StringComparison.OrdinalIgnoreCase))
							sb.AppendLine (line);
					}
				}

				if (bindingsXml != "<binding />")
					sb.Insert(0, "///" + bindingsXml);

				if (sb.Length == 0) {
					//ProjectHelpers.DeleteFileFromProject (bindingPath);
				} else {
					File.WriteAllText(bindingPath, sb.ToString(), Encoding.UTF8);
					//ProjectHelpers.AddNestedFile (configPath, bindingPath);
				}

				return true;
			} catch (Exception ex) {
				LoggingService.LogError ("SavingBindings error.", ex);
				return false;
			}
		}
	}
}
