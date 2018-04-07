//
// TaskRunnerConfigurationLocator.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Collections;

namespace MonoDevelop.TaskRunner
{
	class TaskRunnerConfigurationLocator
	{
		List<TaskRunnerInformation> tasks = new List<TaskRunnerInformation> ();
		List<GroupedTaskRunnerInformation> groupedTasks = new List<GroupedTaskRunnerInformation> ();
		TaskRunnerProvider taskRunnerProvider;
		Solution solution;

		public TaskRunnerConfigurationLocator (TaskRunnerProvider taskRunnerProvider, Solution solution)
		{
			this.taskRunnerProvider = taskRunnerProvider;
			this.solution = solution;
		}

		public IEnumerable<TaskRunnerInformation> Tasks {
			get { return tasks; }
		}

		public IEnumerable<GroupedTaskRunnerInformation> GroupedTasks {
			get { return groupedTasks; }
		}

		public Task FindTasks ()
		{
			return Task.Run (FindTaskInternal);
		}

		async Task FindTaskInternal ()
		{
			foreach (var project in solution.GetAllProjects ()) {
				var foundProjectTasks = await FindTasks (project);
				if (foundProjectTasks != null) {
					tasks.AddRange (foundProjectTasks);

					var info = new GroupedTaskRunnerInformation (project, foundProjectTasks);
					groupedTasks.Add (info);
				}
			}

			groupedTasks.Sort (GroupedTaskRunnerInformationComparer.Instance);

			var foundTasks = await FindTasks (solution);

			if (foundTasks != null) {
				tasks.AddRange (foundTasks);

				var info = new GroupedTaskRunnerInformation (solution, foundTasks);
				groupedTasks.Insert (0, info);
			}
		}

		async Task<List<TaskRunnerInformation>> FindTasks (IWorkspaceFileObject workspaceFileObject)
		{
			List<TaskRunnerInformation> foundTasks = null;

			foreach (FilePath configFile in Directory.EnumerateFiles (workspaceFileObject.BaseDirectory)) {
				ITaskRunner runner = taskRunnerProvider.GetTaskRunner (configFile);
				if (runner != null) {
					ITaskRunnerConfig config = await runner.ParseConfig (null, configFile);
					var info = new TaskRunnerInformation (workspaceFileObject, config, runner.Options, configFile);
					if (foundTasks == null) {
						foundTasks = new List<TaskRunnerInformation> ();
					}
					foundTasks.Add (info);
				}
			}

			return foundTasks;
		}
	}
}
