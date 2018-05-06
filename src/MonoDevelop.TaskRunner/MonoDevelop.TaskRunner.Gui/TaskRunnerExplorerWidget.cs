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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using Xwt;

namespace MonoDevelop.TaskRunner.Gui
{
	partial class TaskRunnerExplorerWidget
	{
		TaskRunnerTreeNode selectedTaskRunnerNode;
		LogView logView;

		public TaskRunnerExplorerWidget ()
		{
			Build ();

			projectsComboBox.SelectionChanged += ProjectComboBoxSelectionChanged;

			tasksTreeView.ButtonPressed += TasksTreeViewButtonPressed;
			tasksTreeView.RowActivated += TasksTreeViewRowActivated;

			bindingsTreeView.ButtonPressed += BindingsTreeViewButtonPressed;
		}

		public Action<ITaskRunnerNode> OnRunTask = node => { };
		public Action<TaskRunnerInformation, ITaskRunnerNode, TaskRunnerBindEvent> OnToggleBinding =
			(info, node, bindEvent) => { };

		public void ClearTasks ()
		{
			selectedTaskRunnerNode = null;
			projectsComboBox.Items.Clear ();
			tasksTreeStore.Clear ();

			logView?.Clear ();
		}

		public void AddTasks (IEnumerable<GroupedTaskRunnerInformation> tasks)
		{
			ClearTasks ();
			ClearBindings ();

			foreach (var task in tasks) {
				projectsComboBox.Items.Add (task, task.Name);
			}

			projectsComboBox.SelectedIndex = 0;
		}

		void ProjectComboBoxSelectionChanged (object sender, EventArgs e)
		{
			tasksTreeStore.Clear ();
			ClearBindings ();

			var groupedTaskRunner = projectsComboBox.SelectedItem as GroupedTaskRunnerInformation;
			if (groupedTaskRunner == null) {
				return;
			}

			foreach (var task in groupedTaskRunner.Tasks) {
				AddTaskNodes (task);
				AddBindingNodes(task);
			}

			tasksTreeView.ExpandAll ();
		}

		void AddTaskNodes (TaskRunnerInformation task)
		{
			var rootNode = new TaskRunnerTreeNode (task);

			TreeNavigator navigator = tasksTreeStore.AddNode ();
			navigator.SetValue (taskRunnerNodeNameField, rootNode.Name);
			navigator.SetValue (taskRunnerField, rootNode);

			AddTaskChildNodes (navigator, rootNode);
		}

		void AddTaskChildNodes (TreeNavigator navigator, TaskRunnerTreeNode node)
		{
			foreach (TaskRunnerTreeNode childNode in node.GetChildNodes ()) {
				TreeNavigator childNavigator = navigator.AddChild ();
				childNavigator.SetValue (taskRunnerNodeNameField, childNode.Name);
				childNavigator.SetValue (taskRunnerField, childNode);

				AddTaskChildNodes (childNavigator, childNode);
				navigator.MoveToParent ();
			}
		}

		void AddBindingNodes (TaskRunnerInformation task)
		{
			foreach (TaskRunnerBindingInformation binding in task.Bindings) {
				AddBindingNodes (task, binding);
			}
		}

		public void RefreshBindings ()
		{
			ClearBindings ();

			var groupedTaskRunner = projectsComboBox.SelectedItem as GroupedTaskRunnerInformation;
			if (groupedTaskRunner == null) {
				return;
			}

			foreach (var task in groupedTaskRunner.Tasks) {
				AddBindingNodes(task);
			}
		}

		void AddBindingNodes (TaskRunnerInformation task, TaskRunnerBindingInformation binding)
		{
			TaskBindingTreeNode parentNode = GetBindingTreeNode (binding.BindEvent);
			if (parentNode == null)
				return;

			TreeNavigator navigator = GetNavigator (parentNode);
			if (navigator == null)
				return;

			AddBindingChildNodes (task, binding, navigator, parentNode);
		}

		TreeNavigator GetNavigator (TaskBindingTreeNode node)
		{
			TreeNavigator navigator = bindingsTreeStore.GetFirstNode ();
			TaskBindingTreeNode currentNode = navigator.GetValue (bindingNodeField);

			while (currentNode != node) {
				if (navigator.MoveNext ()) {
					currentNode = navigator.GetValue (bindingNodeField);
				} else {
					LoggingService.LogError ("Unable to find TreeNavigator for binding tree node {0}", node.Name);
					return null;
				}
			}

			return navigator;
		}

		TaskBindingTreeNode GetBindingTreeNode (TaskRunnerBindEvent bindEvent)
		{
			switch (bindEvent) {
				case TaskRunnerBindEvent.AfterBuild:
					return afterBuildBindingNode;
				case TaskRunnerBindEvent.BeforeBuild:
					return beforeBuildBindingNode;
				case TaskRunnerBindEvent.Clean:
					return cleanBindingNode;
				case TaskRunnerBindEvent.ProjectOpened:
					return projectOpenBindingNode;
				default:
					return null;
			}
		}

		void AddBindingChildNodes (
			TaskRunnerInformation task,
			TaskRunnerBindingInformation binding,
			TreeNavigator navigator,
			TaskBindingTreeNode node)
		{
			foreach (TaskBindingTreeNode childNode in node.CreateChildNodes (task, binding)) {
				TreeNavigator childNavigator = navigator.AddChild ();
				childNavigator.SetValue (bindingNodeNameField, childNode.Name);
				childNavigator.SetValue (bindingNodeField, childNode);

				AddBindingChildNodes (task, binding, childNavigator, childNode);
				navigator.MoveToParent ();
			}

			if (node.IsRootNode) {
				node.RefreshName ();
				navigator.SetValue (bindingNodeNameField, node.Name);
			}
		}

		public OutputProgressMonitor GetProgressMonitor (bool clearConsole = true)
		{
			return logView.GetProgressMonitor (clearConsole);
		}

		public void OpenTaskOutputTab (string name)
		{
			if (taskOutputTab == null) {
				logView = new LogView ();
				logView.ShowAll ();
				var logViewWidget = Toolkit.CurrentEngine.WrapWidget (logView, NativeWidgetSizing.DefaultPreferredSize);
				notebook.Add (logViewWidget, name);
				taskOutputTab = notebook.Tabs [notebook.Tabs.Count - 1];
			}

			taskOutputTab.Label = name;
			notebook.CurrentTab = taskOutputTab;
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

		void ClearBindings ()
		{
			bindingsTreeStore.Clear ();
			AddBindingsTreeNodes ();
		}

		void BindingsTreeViewButtonPressed (object sender, ButtonEventArgs e)
		{
			if (!e.IsContextMenuTrigger)
				return;

			var selectedBindingRunnerNode = GetBindingTreeNode (e.Position);
			if (selectedBindingRunnerNode == null)
				return;

			if (!selectedBindingRunnerNode.IsTaskNameNode)
				return;

			var commands = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/TaskRunnerExplorerPad/BindingContextMenu");
			IdeApp.CommandService.ShowContextMenu (bindingsTreeView.ToGtkWidget (), (int)e.X, (int)e.Y, commands, this);
		}

		TaskBindingTreeNode GetBindingTreeNode (TreePosition position)
		{
			if (position == null)
				return null;

			TreeNavigator navigator = bindingsTreeStore.GetNavigatorAt (position);
			if (navigator == null)
				return null;

			return navigator.GetValue (bindingNodeField);
		}

		TaskBindingTreeNode GetBindingTreeNode (Point position)
		{
			TreePosition treePosition = bindingsTreeView.GetRowAtPosition (position);
			return GetBindingTreeNode (treePosition);
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		void OnUpdateDeleteCommand (CommandInfo info)
		{
			info.Text = GettextCatalog.GetString ("Remove");
		}

		[CommandHandler (EditCommands.Delete)]
		void RemoveBinding ()
		{
			TreeNavigator navigator = bindingsTreeStore.GetNavigatorAt (bindingsTreeView.SelectedRow);
			if (navigator == null) {
				return;
			}

			TaskBindingTreeNode bindingNode = navigator.GetValue (bindingNodeField);
			if (bindingNode == null) {
				return;
			}

			bindingNode.RemoveBinding ();
			navigator.Remove ();

			TaskBindingTreeNode parentNode = GetBindingTreeNode (bindingNode.BindEvent);
			if (parentNode == null) {
				return;
			}

			navigator = GetNavigator (parentNode);
			if (navigator != null) {
				parentNode.RefreshName ();
				navigator.SetValue (bindingNodeNameField, parentNode.Name);
				if (!parentNode.AnyBindings ()) {
					navigator.RemoveChildren ();
				}
			}
		}

		[CommandUpdateHandler (TaskRunnerCommands.MoveBindingUp)]
		void UpdateMoveBindingUp (CommandInfo info)
		{
			TaskBindingTreeNode bindingNode = GetSelectedBindingNode ();
			info.Enabled = bindingNode?.CanMoveUp () == true;
		}

		TaskBindingTreeNode GetSelectedBindingNode ()
		{
			TreeNavigator navigator = bindingsTreeStore.GetNavigatorAt (bindingsTreeView.SelectedRow);
			if (navigator != null) {
				return navigator.GetValue (bindingNodeField);
			}
			return null;
		}

		[CommandUpdateHandler (TaskRunnerCommands.MoveBindingDown)]
		void UpdateMoveBindingDown (CommandInfo info)
		{
			TaskBindingTreeNode bindingNode = GetSelectedBindingNode ();
			info.Enabled = bindingNode?.CanMoveDown () == true;
		}

		[CommandHandler (TaskRunnerCommands.MoveBindingUp)]
		void MoveBindingUp ()
		{
			TaskBindingTreeNode bindingNode = GetSelectedBindingNode ();
			if (bindingNode?.MoveUp () == true) {
				TreeNavigator navigator = bindingsTreeStore.GetNavigatorAt (bindingsTreeView.SelectedRow);
				navigator.MovePrevious ();

				TaskBindingTreeNode otherBindingNode = navigator.GetValue (bindingNodeField);
				navigator.SetValue (bindingNodeNameField, bindingNode.Name);
				navigator.SetValue (bindingNodeField, bindingNode);

				navigator.MoveNext ();
				navigator.SetValue (bindingNodeNameField, otherBindingNode.Name);
				navigator.SetValue (bindingNodeField, otherBindingNode);
			}
		}

		[CommandHandler (TaskRunnerCommands.MoveBindingDown)]
		void MoveBindingDown ()
		{
			TaskBindingTreeNode bindingNode = GetSelectedBindingNode ();
			if (bindingNode?.MoveDown () == true) {
				TreeNavigator navigator = bindingsTreeStore.GetNavigatorAt (bindingsTreeView.SelectedRow);
				navigator.MoveNext ();

				TaskBindingTreeNode otherBindingNode = navigator.GetValue (bindingNodeField);
				navigator.SetValue (bindingNodeNameField, bindingNode.Name);
				navigator.SetValue (bindingNodeField, bindingNode);

				navigator.MovePrevious ();
				navigator.SetValue (bindingNodeNameField, otherBindingNode.Name);
				navigator.SetValue (bindingNodeField, otherBindingNode);
			}
		}
	}
}
