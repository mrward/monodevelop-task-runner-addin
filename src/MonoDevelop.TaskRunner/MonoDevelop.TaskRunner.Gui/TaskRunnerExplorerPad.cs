//
// TaskRunnerExplorerPad.cs
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
using System.Threading.Tasks;
using Gtk;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.TaskRunner.Gui
{
	class TaskRunnerExplorerPad : PadContent
	{
		TaskRunnerExplorerWidget widget;
		Button refreshButton;
		static TaskRunnerExplorerPad instance;

		public TaskRunnerExplorerPad ()
		{
			instance = this;
			TaskRunnerServices.Workspace.TasksChanged += TasksChanged;
		}

		public override void Dispose ()
		{
			TaskRunnerServices.Workspace.TasksChanged -= TasksChanged;

			base.Dispose ();
		}

		public override Control Control {
			get {
				widget = new TaskRunnerExplorerWidget ();
				widget.OnRunTask = RunTask;
				widget.OnToggleBinding = ToggleBinding;
				widget.AddTasks (TaskRunnerServices.Workspace.GroupedTasks);
				return widget.ToGtkWidget ();
			}
		}

		public static TaskRunnerExplorerPad Instance {
			get { return instance; }
		}

		protected override void Initialize (IPadWindow window)
		{
			DockItemToolbar toolbar = window.GetToolbar (DockPositionType.Left);

			refreshButton = new Button (new ImageView ("gtk-refresh", IconSize.Menu));
			refreshButton.Clicked += OnButtonRefreshClick;
			refreshButton.TooltipText = GettextCatalog.GetString ("Refresh");
			toolbar.Add (refreshButton);

			toolbar.ShowAll ();
		}

		void OnButtonRefreshClick (object sender, EventArgs e)
		{
			TaskRunnerServices.Workspace.Refresh ();
		}

		void TasksChanged (object sender, EventArgs e)
		{
			var workspace = (TaskRunnerWorkspace)sender;
			widget.AddTasks (workspace.GroupedTasks);
		}

		void RunTask (ITaskRunnerNode taskRunnerNode)
		{
			try {
				RunTaskAsync (taskRunnerNode).Ignore ();
			} catch (Exception ex) {
				LoggingService.LogInfo ("TaskRunnerExplorerPad.RunTask error", ex);
			}
		}

		public Task<ITaskRunnerCommandResult> RunTaskAsync (ITaskRunnerNode taskRunnerNode, bool clearConsole = true)
		{
			Runtime.AssertMainThread ();

			widget.OpenTaskOutputTab (taskRunnerNode.Name);

			OutputProgressMonitor progressMonitor = widget.GetProgressMonitor (clearConsole);

			var context = new TaskRunnerCommandContext (progressMonitor);
			return taskRunnerNode.Invoke (context);
		}

		void ToggleBinding (TaskRunnerInformation taskRunnerInfo, ITaskRunnerNode node, TaskRunnerBindEvent bindEvent)
		{
			try {
				taskRunnerInfo.ToggleBinding (bindEvent, node);
				widget.RefreshBindings ();
			} catch (Exception ex) {
				LoggingService.LogError ("Toggle binding failed.", ex);
				MessageService.ShowError (GettextCatalog.GetString ("Unable to change binding."), ex);
			}
		}

		public static void Create ()
		{
			Runtime.AssertMainThread ();

			if (instance == null) {
				var pad = IdeApp.Workbench.GetPad<TaskRunnerExplorerPad> ();
				pad.BringToFront (false);
			}
		}
	}
}
