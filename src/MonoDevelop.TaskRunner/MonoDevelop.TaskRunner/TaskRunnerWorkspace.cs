//
// TaskRunnerWorkspace.cs
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.TaskRunner.Gui;

namespace MonoDevelop.TaskRunner
{
	class TaskRunnerWorkspace
	{
		TaskRunnerProvider taskRunnerProvider;
		ImmutableList<GroupedTaskRunnerInformation> groupedTasks = ImmutableList<GroupedTaskRunnerInformation>.Empty;

		public TaskRunnerWorkspace (TaskRunnerProvider taskRunnerProvider)
		{
			this.taskRunnerProvider = taskRunnerProvider;

			IdeApp.Workspace.SolutionLoaded += SolutionLoaded;
			IdeApp.Workspace.SolutionUnloaded += SolutionUnloaded;
		}

		public event EventHandler TasksChanged;

		void OnTasksChanged ()
		{
			Runtime.RunInMainThread (() => {
				TasksChanged?.Invoke (this, EventArgs.Empty);
			});
		}

		public IEnumerable<GroupedTaskRunnerInformation> GroupedTasks {
			get { return groupedTasks; }
		}

		void SolutionLoaded (object sender, SolutionEventArgs e)
		{
			try {
				OnSolutionLoaded (e.Solution).Ignore ();
			} catch (Exception ex) {
				LoggingService.LogError ("TaskRunnerWorkspace SolutionLoaded error", ex);
			}
		}

		public GroupedTaskRunnerInformation GetGroupedTask (IWorkspaceFileObject workspaceFileObject)
		{
			return groupedTasks.FirstOrDefault (task => task.WorkspaceFileObject == workspaceFileObject);
		}

		public void Refresh ()
		{
			try {
				Task.Run (RefreshInternal).Ignore ();
			} catch (Exception ex) {
				LoggingService.LogError ("TaskRunnerWorkspace SolutionLoaded error", ex);
			}
		}

		async Task RefreshInternal ()
		{
			groupedTasks = groupedTasks.Clear ();

			foreach (var solution in IdeApp.Workspace.GetAllSolutions ()) {
				await FindTasks (solution, raiseTasksChangedEvent: false);
			}

			OnTasksChanged ();
		}

		void SolutionUnloaded (object sender, SolutionEventArgs e)
		{
			try {
				RemoveTasks (e.Solution);
			} catch (Exception ex) {
				LoggingService.LogError ("TaskRunnerWorkspace SolutionUnloaded error", ex);
			}
		}

		async Task OnSolutionLoaded (Solution solution)
		{
			await FindTasks (solution).ConfigureAwait (false);

			if (TaskRunnerServices.AutomaticallyRunTasks) {
				await RunProjectOpenTasksAsync (solution).ConfigureAwait (false);
			}
		}

		async Task FindTasks (Solution solution, bool raiseTasksChangedEvent = true)
		{
			var locator = new TaskRunnerConfigurationLocator (taskRunnerProvider, solution);
			await locator.FindTasks ();

			if (locator.GroupedTasks.Any ()) {
				groupedTasks = groupedTasks.AddRange (locator.GroupedTasks);

				if (raiseTasksChangedEvent) {
					OnTasksChanged ();
				}
			}
		}

		void RemoveTasks (Solution solution)
		{
			// Should really be removing tasks for the solution only and
			// handling projects that are still open in another solution.
			// For now just refresh the tasks.
			Refresh ();
		}

		public Task<ITaskRunnerCommandResult> RunTask (ITaskRunnerNode node)
		{
			return Runtime.RunInMainThread (() => {
				TaskRunnerExplorerPad.Create ();
				return TaskRunnerExplorerPad.Instance.RunTaskAsync (node, clearConsole: false);
			});
		}

		public async Task<BuildResult> RunBuildTasks (GroupedTaskRunnerInformation tasks, TaskRunnerBindEvent bindEvent)
		{
			var buildResult = new BuildResult ();

			foreach (ITaskRunnerNode node in tasks.GetTasks (bindEvent)) {
				ITaskRunnerCommandResult result = await TaskRunnerServices.Workspace.RunTask (node);
				if (result.ExitCode != 0) {
					buildResult.AddWarning (node, result);
				}
			}

			return buildResult;
		}

		Task RunProjectOpenTasksAsync (Solution solution)
		{
			return Task.Run (() => {
				return RunProjectOpenTasks (solution);
			});
		}

		async Task RunProjectOpenTasks (Solution solution)
		{
			GroupedTaskRunnerInformation tasks = GetGroupedTask (solution);
			if (tasks != null) {
				await RunBuildTasks (tasks, TaskRunnerBindEvent.ProjectOpened);
			}

			foreach (Project project in solution.GetAllProjects ()) {
				tasks = GetGroupedTask (project);
				if (tasks != null) {
					await RunBuildTasks (tasks, TaskRunnerBindEvent.ProjectOpened);
				}
			}
		}
	}
}
