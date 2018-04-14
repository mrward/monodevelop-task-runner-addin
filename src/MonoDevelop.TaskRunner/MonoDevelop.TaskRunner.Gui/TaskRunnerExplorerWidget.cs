//
// TaskRunnerExplorerWidget.cs
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
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using Xwt;
using Xwt.Backends;

namespace MonoDevelop.TaskRunner.Gui
{
	partial class TaskRunnerExplorerWidget
	{
		TaskRunnerTreeNode selectedTaskRunnerNode;

		public TaskRunnerExplorerWidget ()
		{
			Build ();

			projectsComboBox.SelectionChanged += ProjectComboBoxSelectionChanged;

			tasksTreeView.ButtonPressed += TasksTreeViewButtonPressed;
			tasksTreeView.RowActivated += TasksTreeViewRowActivated;
		}

		public Action<ITaskRunnerNode> OnRunTask = node => { };
		public Action<TaskRunnerInformation, ITaskRunnerNode, TaskRunnerBindEvent> OnToggleBinding =
			(info, node, bindEvent) => { };

		public void ClearTasks ()
		{
			selectedTaskRunnerNode = null;
			projectsComboBox.Items.Clear ();
			tasksTreeStore.Clear ();
		}

		public void AddTasks (IEnumerable<GroupedTaskRunnerInformation> tasks)
		{
			ClearTasks ();

			foreach (var task in tasks) {
				projectsComboBox.Items.Add (task, task.Name);
			}

			projectsComboBox.SelectedIndex = 0;
		}

		void ProjectComboBoxSelectionChanged (object sender, EventArgs e)
		{
			tasksTreeStore.Clear ();

			var groupedTaskRunner = projectsComboBox.SelectedItem as GroupedTaskRunnerInformation;
			if (groupedTaskRunner == null) {
				return;
			}

			foreach (var task in groupedTaskRunner.Tasks) {
				var rootNode = new TaskRunnerTreeNode (task);

				TreeNavigator navigator = tasksTreeStore.AddNode ();
				navigator.SetValue (taskRunnerNodeNameField, rootNode.Name);
				navigator.SetValue (taskRunnerField, rootNode);

				AddChildNodes (navigator, rootNode);
			}

			tasksTreeView.ExpandAll ();
		}

		public void OpenTaskOutputTab (string name)
		{
			if (taskOutputTab == null) {
				outputView = new RichTextView ();
				var scrollView = new ScrollView ();
				scrollView.HorizontalScrollPolicy = ScrollPolicy.Automatic;
				scrollView.Content = outputView;
				notebook.Add (scrollView, name);
				taskOutputTab = notebook.Tabs [notebook.Tabs.Count - 1];
			}

			taskOutputTab.Label = name;
			notebook.CurrentTab = taskOutputTab;
		}

		void AddChildNodes (TreeNavigator navigator, TaskRunnerTreeNode node)
		{
			foreach (var childNode in node.GetChildNodes ()) {
				TreeNavigator childNavigator = navigator.AddChild ();
				childNavigator.SetValue (taskRunnerNodeNameField, childNode.Name);
				childNavigator.SetValue (taskRunnerField, childNode);

				AddChildNodes (childNavigator, childNode);
				navigator.MoveToParent ();
			}
		}

		void TasksTreeViewRowActivated (object sender, TreeViewRowEventArgs e)
		{
			selectedTaskRunnerNode = GetTaskRunnerTreeNode (e.Position);
			RunTask ();
		}

		bool CanRunSelectedTask ()
		{
			return selectedTaskRunnerNode?.IsInvokable == true;
		}

		TaskRunnerTreeNode GetTaskRunnerTreeNode (TreePosition position)
		{
			if (position == null)
				return null;

			TreeNavigator navigator = tasksTreeStore.GetNavigatorAt (position);
			if (navigator == null)
				return null;

			return navigator.GetValue (taskRunnerField);
		}

		TaskRunnerTreeNode GetTaskRunnerTreeNode (Point position)
		{
			TreePosition treePosition = tasksTreeView.GetRowAtPosition (position);
			return GetTaskRunnerTreeNode (treePosition);
		}

		void TasksTreeViewButtonPressed (object sender, ButtonEventArgs e)
		{
			if (!e.IsContextMenuTrigger)
				return;

			selectedTaskRunnerNode = GetTaskRunnerTreeNode (e.Position);
			if (selectedTaskRunnerNode == null)
				return;

			var commands = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/TaskRunnerExplorerPad/TaskContextMenu");
			IdeApp.CommandService.ShowContextMenu (tasksTreeView.ToGtkWidget (), (int)e.X, (int)e.Y, commands, this);
		}

		[CommandUpdateHandler (TaskRunnerCommands.RunTask)]
		void OnUpdateRun (CommandInfo info)
		{
			info.Enabled = CanRunSelectedTask ();
		}

		[CommandHandler (TaskRunnerCommands.RunTask)]
		void RunTask ()
		{
			if (CanRunSelectedTask ()) {
				OnRunTask (selectedTaskRunnerNode.TaskRunner);
			}
		}

		internal void WriteOutput (string text)
		{
			var backend = outputView.GetBackend () as IRichTextViewBackend;
			IRichTextBuffer buffer = backend.CurrentBuffer;
			if (buffer == null) {
				buffer = backend.CreateBuffer ();
				backend.SetBuffer (buffer);
			}
			buffer.EmitText (text + Environment.NewLine, RichTextInlineStyle.Normal);
		}

		[CommandUpdateHandler (TaskRunnerCommands.ToggleAfterBuildBinding)]
		void OnUpdateToggleAfterBuildBinding (CommandInfo info)
		{
			OnUpdateToggleBinding (info, TaskRunnerBindEvent.AfterBuild);
		}

		[CommandUpdateHandler (TaskRunnerCommands.ToggleBeforeBuildBinding)]
		void OnUpdateToggleBeforeBuildBinding (CommandInfo info)
		{
			OnUpdateToggleBinding (info, TaskRunnerBindEvent.BeforeBuild);
		}

		[CommandUpdateHandler (TaskRunnerCommands.ToggleCleanBinding)]
		void OnUpdateToggleCleanBinding (CommandInfo info)
		{
			OnUpdateToggleBinding (info, TaskRunnerBindEvent.Clean);
		}

		[CommandUpdateHandler (TaskRunnerCommands.ToggleProjectOpenBinding)]
		void OnUpdateToggleProjectOpenBinding (CommandInfo info)
		{
			OnUpdateToggleBinding (info, TaskRunnerBindEvent.ProjectOpened);
		}

		void OnUpdateToggleBinding (CommandInfo info, TaskRunnerBindEvent bindEvent)
		{
			info.Enabled = CanRunSelectedTask ();
			if (info.Enabled) {
				info.Checked = selectedTaskRunnerNode.IsBindingEnabled (bindEvent);
			}
		}

		[CommandHandler (TaskRunnerCommands.ToggleAfterBuildBinding)]
		void ToggleAfterBuildBinding ()
		{
			ToggleBinding (TaskRunnerBindEvent.AfterBuild);
		}

		[CommandHandler (TaskRunnerCommands.ToggleBeforeBuildBinding)]
		void ToggleBeforeBuildBinding ()
		{
			ToggleBinding (TaskRunnerBindEvent.BeforeBuild);
		}

		[CommandHandler (TaskRunnerCommands.ToggleCleanBinding)]
		void ToggleCleanBinding ()
		{
			ToggleBinding (TaskRunnerBindEvent.Clean);
		}

		[CommandHandler (TaskRunnerCommands.ToggleProjectOpenBinding)]
		void ToggleProjectOpenBinding ()
		{
			ToggleBinding (TaskRunnerBindEvent.ProjectOpened);
		}

		void ToggleBinding (TaskRunnerBindEvent bindEvent)
		{
			if (CanRunSelectedTask ()) {
				OnToggleBinding (selectedTaskRunnerNode.TaskInfo, selectedTaskRunnerNode.TaskRunner, bindEvent);
			}
		}
	}
}
