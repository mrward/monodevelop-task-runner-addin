//
// Based on: https://github.com/madskristensen/TaskRunnerTemplate/blob/master/src/TaskRunnerExtension/TaskRunner/TaskRunnerProvider.cs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

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
					options.Add (new TaskRunnerOption ("Verbose", Stock.Information, false, " (verbose)"));
				}

				return options;
			}
		}

		public async Task<ITaskRunnerConfig> ParseConfig (ITaskRunnerCommandContext context, string configPath)
		{
			try {
				return await Task.Run (() => {
					TaskRunnerLogger.WriteLine ("TestTaskRunnerProvider.ParseConfig configPath={0}", configPath);
					ITaskRunnerNode hierarchy = LoadHierarchy (configPath);
					return new TaskRunnerConfig (hierarchy);
				});
			} catch (Exception ex) {
				TaskRunnerLogger.WriteLine ("Load failed. {0}", ex.Message);
				return CreateErrorTaskRunnerConfig (ex);
			}
		}

		TaskRunnerConfig CreateErrorTaskRunnerConfig (Exception ex)
		{
			var root = new TaskRunnerErrorNode ("My Config");

			string message = GettextCatalog.GetString ("Failed to load. Please open the Task Runner Explorer Output for more information.");
			root.Children.Add (new TaskRunnerErrorNode (message) {
				Description = "Failed to load."
			});

			return new TaskRunnerConfig (root);
		}

		/// <summary>
		/// Construct any task hierarchy that you need.
		/// </summary>
		ITaskRunnerNode LoadHierarchy (string configPath)
		{
			string cwd = Path.GetDirectoryName (configPath);

			var root = new TaskRunnerNode ("My Config");

			root.Children.Add (new TaskRunnerNode ("TSC watch", true) {
				Description = "Executes Task 1.",
				Command = new TaskRunnerCommand (cwd, "tsc", "-p /Users/matt/Projects/Tests/tsc-test3234234 --watch")
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
