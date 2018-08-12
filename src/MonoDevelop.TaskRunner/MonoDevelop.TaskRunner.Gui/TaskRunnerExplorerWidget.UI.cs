//
// TaskRunnerExplorerWidget.UI.cs
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

using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.TaskRunner.Gui
{
	partial class TaskRunnerExplorerWidget : Widget
	{
		ComboBox projectsComboBox;
		TreeView tasksTreeView;
		TreeView bindingsTreeView;
		HPaned paned;
		TreeStore tasksTreeStore;
		TaskCellView taskCellView;
		DataField<string> taskRunnerNodeNameField;
		DataField<Image> taskRunnerNodeIconField;
		DataField<TaskRunnerTreeNode> taskRunnerField;
		TreeStore bindingsTreeStore;
		TaskCellView bindingCellView;
		DataField<Image> bindingNodeIconField;
		DataField<string> bindingNodeNameField;
		DataField<TaskBindingTreeNode> bindingNodeField;
		TaskBindingTreeNode beforeBuildBindingNode;
		TaskBindingTreeNode afterBuildBindingNode;
		TaskBindingTreeNode cleanBindingNode;
		TaskBindingTreeNode projectOpenBindingNode;
		Notebook notebook;

		void Build ()
		{
			paned = new HPaned ();

			var tasksVBox = new VBox ();
			tasksVBox.MinWidth = 400;
			paned.Panel1.Content = tasksVBox;

			projectsComboBox = new ComboBox ();
			tasksVBox.PackStart (projectsComboBox);

			tasksTreeView = new TreeView ();
			tasksTreeView.HeadersVisible = false;
			tasksVBox.PackStart (tasksTreeView, true, true);

			taskRunnerField = new DataField<TaskRunnerTreeNode> ();
			taskRunnerNodeIconField = new DataField<Image> ();
			taskRunnerNodeNameField = new DataField<string> ();
			tasksTreeStore = new TreeStore (
				taskRunnerNodeIconField,
				taskRunnerNodeNameField,
				taskRunnerField);
			tasksTreeView.DataSource = tasksTreeStore;

			taskCellView = new TaskCellView {
				ImageField = taskRunnerNodeIconField,
				NameField = taskRunnerNodeNameField
			};

			var column = new ListViewColumn ("Task", taskCellView);
			tasksTreeView.Columns.Add (column);

			notebook = new Notebook ();
			notebook.TabOrientation = NotebookTabOrientation.Top;
			paned.Panel2.Content = notebook;
			paned.Panel2.Resize = true;

			bindingsTreeView = new TreeView ();
			bindingsTreeView.HeadersVisible = false;
			notebook.Add (bindingsTreeView, GettextCatalog.GetString ("Bindings"));

			bindingNodeNameField = new DataField<string> ();
			bindingNodeIconField = new DataField<Image> ();
			bindingNodeField = new DataField<TaskBindingTreeNode> ();
			bindingsTreeStore = new TreeStore (
				bindingNodeIconField,
				bindingNodeNameField,
				bindingNodeField);
			bindingsTreeView.DataSource = bindingsTreeStore;

			bindingCellView = new TaskCellView {
				ImageField = bindingNodeIconField,
				NameField = bindingNodeNameField
			};

			column = new ListViewColumn ("Binding", bindingCellView);
			bindingsTreeView.Columns.Add (column);

			AddBindingsTreeNodes ();

			Content = paned;
		}

		void AddBindingsTreeNodes ()
		{
			beforeBuildBindingNode = AddBindingsTreeNode (TaskRunnerBindEvent.BeforeBuild);
			afterBuildBindingNode = AddBindingsTreeNode (TaskRunnerBindEvent.AfterBuild);
			cleanBindingNode = AddBindingsTreeNode (TaskRunnerBindEvent.Clean);
			projectOpenBindingNode = AddBindingsTreeNode (TaskRunnerBindEvent.ProjectOpened);
		}

		TaskBindingTreeNode AddBindingsTreeNode (TaskRunnerBindEvent bindEvent)
		{
			var node = new TaskBindingTreeNode (bindEvent);

			TreeNavigator navigator = bindingsTreeStore.AddNode ();
			navigator.SetValue (bindingNodeNameField, node.Name);
			navigator.SetValue (bindingNodeField, node);

			return node;
		}
	}
}
