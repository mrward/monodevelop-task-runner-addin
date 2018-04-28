//
// Based on: https://github.com/madskristensen/TaskRunnerTemplate/blob/master/src/TaskRunnerExtension/TaskRunner/TaskRunnerProvider.cs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace MonoDevelop.TestTaskRunner
{
	[TaskRunnerExport ("myconfig.json")]
	class TaskRunnerProvider : ITaskRunner
	{
		List<ITaskRunnerOption> options = null;

		/// <summary>
		///  This is where buttons from the VSCommandTable.vsct file are registered.
		///  The string parameter is any argument that must be passed on to the task when it's enabled.
		/// </summary>
		public List<ITaskRunnerOption> Options
		{
			get {
				if (options == null) {
					options = new List<ITaskRunnerOption> ();
					options.Add (new TaskRunnerOption ("Verbose", 123, Guid.Parse ("{8B200612-65E7-402F-B1EC-373F3AB4196F}"), false, " (verbose)"));
				}

				return options;
			}
		}

		public async Task<ITaskRunnerConfig> ParseConfig (ITaskRunnerCommandContext context, string configPath)
		{
			return await Task.Run (() => {
				ITaskRunnerNode hierarchy = LoadHierarchy (configPath);
				return new TaskRunnerConfig (hierarchy);
			});
		}

		/// <summary>
		/// Construct any task hierarchy that you need.
		/// </summary>
		ITaskRunnerNode LoadHierarchy (string configPath)
		{
			string cwd = Path.GetDirectoryName (configPath);

			var root = new TaskRunnerNode ("My Config");

			root.Children.Add (new TaskRunnerNode ("Task 1", true) {
				Description = "Executes Task 1.",
				Command = new TaskRunnerCommand (cwd, "bash", "-c \"echo Task 1\"")
			});

			root.Children.Add (new TaskRunnerNode ("Task 2", true) {
				Description = "Executes Task 2.",
				Command = new TaskRunnerCommand (cwd, "bash", "-c \"echo Task 2\"")
			});

			root.Children.Add (new TaskRunnerNode ("Task 3", true) {
				Description = "Executes Task 3.",
				Command = new TaskRunnerCommand (cwd, "bash", "-c \"echo Task 3\"")
			});

			return root;
		}
	}
}
