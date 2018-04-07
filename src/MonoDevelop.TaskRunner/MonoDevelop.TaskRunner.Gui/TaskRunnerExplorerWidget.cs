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
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.TaskRunner.Gui
{
	partial class TaskRunnerExplorerWidget
	{
		public TaskRunnerExplorerWidget ()
		{
			Build ();

			projectsComboBox.SelectionChanged += ProjectComboBoxSelectionChanged;
		}

		public void ClearTasks ()
		{
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
	}
}
