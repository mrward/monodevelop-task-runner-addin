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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gtk;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.TaskRunner.Gui
{
	class TaskRunnerExplorerPad : PadContent
	{
		TaskRunnerExplorerWidget widget;
		Control control;
		Button refreshButton;
		Button clearButton;
		Button stopButton;
		ToggleButton showLogButton;
		HSeparator separator;
		List<ToggleButton> optionButtons = new List<ToggleButton> ();
		DockItemToolbar optionsToolbar;
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

			widget.Dispose ();
		}

		public override Control Control {
			get {
				if (widget == null) {
					widget = new TaskRunnerExplorerWidget ();
					widget.OnRunTask = RunTask;
					widget.OnToggleBinding = ToggleBinding;
					widget.OnTaskRunnerSelected = TaskRunnerSelected;
					widget.OnTaskRunnerOutputViewChanged = TaskRunnerOutputViewChanged;
					widget.AddTasks (TaskRunnerServices.Workspace.GroupedTasks);

					// Ensure any messages that were logged whilst the pad was not available are
					// displayed in the output log view.
					TaskRunnerServices.LoggingService.LogPendingMessages ();

					control = widget.ToGtkWidget ();
				}
				return control;
			}
		}

		public static TaskRunnerExplorerPad Instance {
			get { return instance; }
		}

		protected override void Initialize (IPadWindow window)
		{
			DockItemToolbar toolbar = window.GetToolbar (DockPositionType.Left);
			optionsToolbar = toolbar;

			refreshButton = new Button (new ImageView ("gtk-refresh", IconSize.Menu));
			refreshButton.Clicked += OnButtonRefreshClick;
			refreshButton.TooltipText = GettextCatalog.GetString ("Refresh");
			toolbar.Add (refreshButton);

			toolbar.ShowAll ();

			toolbar = window.GetToolbar (DockPositionType.Right);

			stopButton = new Button (new ImageView (Ide.Gui.Stock.Stop, IconSize.Menu));
			stopButton.Clicked += OnStopButtonClick;
			stopButton.TooltipText = GettextCatalog.GetString ("Stop selected task");
			toolbar.Add (stopButton);

			clearButton = new Button (new ImageView (Ide.Gui.Stock.Broom, IconSize.Menu));
			clearButton.Clicked += OnClearButtonClick;
			clearButton.TooltipText = GettextCatalog.GetString ("Clear Output");
			toolbar.Add (clearButton);

			showLogButton = new ToggleButton ();
			AddIcon (showLogButton, Ide.Gui.Stock.Console);
			showLogButton.Clicked += OnShowLogButtonClick;
			showLogButton.TooltipText = GettextCatalog.GetString ("Show Task Runner Explorer Output");
			toolbar.Add (showLogButton);

			toolbar.ShowAll ();
		}

		void OnButtonRefreshClick (object sender, EventArgs e)
		{
			TaskRunnerServices.Workspace.Refresh ();
		}

		void OnClearButtonClick (object sender, EventArgs e)
		{
			widget.ClearLog ();
		}

		void TasksChanged (object sender, EventArgs e)
		{
			var workspace = (TaskRunnerWorkspace)sender;
			widget.AddTasks (workspace.GroupedTasks);
		}

		void RunTask (ITaskRunnerNode taskRunnerNode, IEnumerable<ITaskRunnerOption> options)
		{
			try {
				var task = new TaskRunnerWithOptions (taskRunnerNode, options);
				RunTaskAsync (task).Ignore ();
			} catch (Exception ex) {
				LoggingService.LogError ("TaskRunnerExplorerPad.RunTask error", ex);
			}
		}

		public async Task<ITaskRunnerCommandResult> RunTaskAsync (TaskRunnerWithOptions task, bool clearConsole = true)
		{
			Runtime.AssertMainThread ();

			RunningTaskInformation runningTask = null;

			try {
				Xwt.NotebookTab tab = widget.GetTaskOutputTab (task.TaskRunner.Name);
				OutputProgressMonitor progressMonitor = widget.GetProgressMonitor (tab, clearConsole);

				task.ApplyOptionsToCommand ();

				var context = new TaskRunnerCommandContext (progressMonitor);
				runningTask = new RunningTaskInformation (context, task);
				TaskRunnerServices.Workspace.AddRunningTask (runningTask);

				widget.OpenTaskOutputTab (tab, runningTask);

				var result = await task.TaskRunner.Invoke (context);
				widget.ShowResult (tab, result);
				return result;
			} finally {
				if (runningTask != null) {
					TaskRunnerServices.Workspace.RemoveRunningTask (runningTask);
					widget.HideRunningStatus (runningTask);
				}
			}
		}

		void ToggleBinding (TaskRunnerInformation taskRunnerInfo, ITaskRunnerNode node, TaskRunnerBindEvent bindEvent)
		{
			try {
				taskRunnerInfo.ToggleBinding (bindEvent, node);
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

		void TaskRunnerSelected (TaskRunnerInformation task)
		{
			RemoveExistingOptionButtons ();
			AddOptionsButton (task);
		}

		void RemoveExistingOptionButtons ()
		{
			if (separator != null) {
				optionsToolbar.Remove (separator);
				separator.Dispose ();
				separator = null;
			}

			foreach (ToggleButton button in optionButtons) {
				button.Clicked -= OptionButtonClicked;
				optionsToolbar.Remove (button);
				button.Dispose ();
			}

			optionButtons.Clear ();
		}

		void AddOptionsButton (TaskRunnerInformation task)
		{
			if (task?.Options == null || !task.Options.Any ())
				return;

			separator = new HSeparator ();
			optionsToolbar.Add (separator);

			foreach (ITaskRunnerOption option in task.Options) {
				var button = new ToggleButton ();
				button.Name = option.Name;
				button.TooltipText = option.GetTooltipText ();
				button.Active = option.Checked;
				AddIcon (button, option.Icon);
				button.Data ["option"] = option;
				button.Clicked += OptionButtonClicked;

				optionButtons.Add (button);
				optionsToolbar.Add (button);
			}

			optionsToolbar.ShowAll ();
		}

		void AddIcon (ToggleButton button, IconId icon)
		{
			if (icon.IsNull)
				icon = Ide.Gui.Stock.Options;

			var hbox = new HBox ();
			hbox.Homogeneous = false;
			hbox.Spacing = 2;
			var imageView = new ImageView (ImageService.GetIcon (icon, IconSize.Menu));
			hbox.PackStart (imageView);
			button.Child = hbox;
		}

		void OptionButtonClicked (object sender, EventArgs e)
		{
			var button = (ToggleButton)sender;
			var option = (ITaskRunnerOption)button.Data ["option"];
			option.Checked = button.Active;
		}

		void OnStopButtonClick (object sender, EventArgs e)
		{
			RunningTaskInformation runningTask = widget.GetRunningTaskFromCurrentTab ();
			if (runningTask != null) {
				runningTask.Stop ();
			}
		}

		void OnShowLogButtonClick (object sender, EventArgs e)
		{
			var button = (ToggleButton)sender;
			if (button.Active) {
				widget.ShowTaskRunnerExplorerLog ();
			} else {
				widget.HideTaskRunnerExplorerLog ();
			}
		}

		public LogView TaskRunnerOutputLogView {
			get { return widget.TaskRunnerOutputLogView; }
		}

		void TaskRunnerOutputViewChanged (bool open)
		{
			showLogButton.Active = open;
		}
	}
}
