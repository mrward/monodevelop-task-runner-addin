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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.TaskRunner.Gui;

namespace MonoDevelop.TaskRunner
{
	class TaskRunnerWorkspace
	{
		TaskRunnerProvider taskRunnerProvider;
		ImmutableList<GroupedTaskRunnerInformation> groupedTasks = ImmutableList<GroupedTaskRunnerInformation>.Empty;
		List<RunningTaskInformation> runningTasks = new List<RunningTaskInformation> ();

		public TaskRunnerWorkspace (TaskRunnerProvider taskRunnerProvider)
		{
			this.taskRunnerProvider = taskRunnerProvider;

			IdeApp.Workspace.SolutionLoaded += SolutionLoaded;
			IdeApp.Workspace.SolutionUnloaded += SolutionUnloaded;
			IdeApp.Workspace.FileAddedToProject += FileAddedToProject;
			FileService.FileRemoved += FileRemoved;
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
				StopRunningTasks ();
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

		public Task<ITaskRunnerCommandResult> RunTask (TaskRunnerWithOptions task)
		{
			return Runtime.RunInMainThread (() => {
				TaskRunnerExplorerPad.Create ();
				return TaskRunnerExplorerPad.Instance.RunTaskAsync (task, clearConsole: false);
			});
		}

		public async Task<BuildResult> RunBuildTasks (GroupedTaskRunnerInformation tasks, TaskRunnerBindEvent bindEvent)
		{
			var buildResult = new BuildResult ();

			foreach (TaskRunnerWithOptions node in tasks.GetTasks (bindEvent)) {
				ITaskRunnerCommandResult result = await TaskRunnerServices.Workspace.RunTask (node);
				if (result.ExitCode != 0) {
					buildResult.AddWarning (node.TaskRunner, result);
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
			ProgressMonitor monitor = null;
			try {
				GroupedTaskRunnerInformation tasks = GetGroupedTask (solution);
				if (tasks != null) {
					monitor = CreateProgressMonitor ();
					await RunBuildTasks (tasks, TaskRunnerBindEvent.ProjectOpened);
				}

				foreach (Project project in solution.GetAllProjects ()) {
					tasks = GetGroupedTask (project);
					if (tasks != null) {
						if (monitor == null) {
							monitor = CreateProgressMonitor ();
						}
						await RunBuildTasks (tasks, TaskRunnerBindEvent.ProjectOpened);
					}
				}
			} finally {
				monitor?.Dispose ();
			}
		}

		ProgressMonitor CreateProgressMonitor ()
		{
			var pad = IdeApp.Workbench.GetPad<TaskRunnerExplorerPad> ();

			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Running tasks for solution..."),
				Stock.StatusSolutionOperation,
				false,
				false,
				false,
				pad,
				true);
		}

		void FileAddedToProject (object sender, ProjectFileEventArgs eventArgs)
		{
			CheckTaskRunnerAvailableForFile (eventArgs).Ignore ();
		}

		async Task CheckTaskRunnerAvailableForFile (ProjectFileEventArgs eventArgs)
		{
			foreach (ProjectFileEventInfo fileEventInfo in eventArgs) {
				ITaskRunner runner = taskRunnerProvider.GetTaskRunner (fileEventInfo.ProjectFile.FilePath);
				if (runner != null) {
					await AddTaskRunner (runner, fileEventInfo.ProjectFile.FilePath, fileEventInfo.Project);
				}
			}
		}

		async Task AddTaskRunner (ITaskRunner runner, FilePath configFile, Project project)
		{
			ITaskRunnerConfig config = await runner.ParseConfig (null, configFile);
			var info = new TaskRunnerInformation (project, config, runner.Options, configFile);

			var groupedTask = GetGroupedTask (project);
			if (groupedTask == null) {
				groupedTask = new GroupedTaskRunnerInformation (project);
				groupedTasks = groupedTasks.Add (groupedTask);
			}

			groupedTask.AddTask (info);

			OnTasksChanged ();
		}

		void FileRemoved (object sender, FileEventArgs eventArgs)
		{
			var files = eventArgs
				.Where (HasTaskRunner)
				.Select (fileEventInfo => fileEventInfo.FileName)
				.ToList ();
			if (!files.Any ())
				return;

			RemoveTaskRunners (files);
		}

		void RemoveTaskRunners (IEnumerable<FilePath> files)
		{
			bool modified = false;

			var groupsToRemove = new List<GroupedTaskRunnerInformation> ();
			foreach (var groupedTask in groupedTasks) {
				var tasksToRemove = new List<TaskRunnerInformation> ();
				foreach (var task in groupedTask.Tasks) {
					if (files.Contains (task.ConfigFile)) {
						tasksToRemove.Add (task);
					}
				}
				if (tasksToRemove.Any ()) {
					modified = true;
					groupedTask.RemoveTasks (tasksToRemove);
					if (!groupedTask.Tasks.Any ()) {
						groupsToRemove.Add (groupedTask);
					}
				}
			}

			if (groupsToRemove.Any ()) {
				groupedTasks = groupedTasks.RemoveAll (groupsToRemove.Contains);
			}

			if (modified) {
				OnTasksChanged ();
			}
		}

		bool HasTaskRunner (FileEventInfo eventInfo)
		{
			return taskRunnerProvider.GetTaskRunner (eventInfo.FileName) != null;
		}

		public void AddRunningTask (RunningTaskInformation runningTask)
		{
			lock (runningTasks) {
				runningTasks.Add (runningTask);
			}
		}

		public void RemoveRunningTask (RunningTaskInformation runningTask)
		{
			lock (runningTasks) {
				runningTasks.Remove (runningTask);
				runningTask.IsRunning = false;
			}
		}

		void StopRunningTasks ()
		{
			lock (runningTasks) {
				foreach (RunningTaskInformation task in runningTasks.ToArray ()) {
					try {
						runningTasks.Remove (task);
						task.IsRunning = false;
						task.Stop ();
					} catch (Exception ex) {
						LoggingService.LogError ("Failed to stop running task.", ex);
					}
				}
			}
		}
	}
}
