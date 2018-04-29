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

namespace MonoDevelop.TaskRunner.Gui
{
	partial class TaskRunnerExplorerWidget : Widget
	{
		ComboBox projectsComboBox;
		TreeView tasksTreeView;
		TreeView bindingsTreeView;
		HPaned paned;
		TreeStore tasksTreeStore;
		DataField<string> taskRunnerNodeNameField;
		DataField<TaskRunnerTreeNode> taskRunnerField;
		TreeStore bindingsTreeStore;
		DataField<string> bindingNodeNameField;
		DataField<TaskBindingTreeNode> bindingNodeField;
		TaskBindingTreeNode beforeBuildBindingNode;
		TaskBindingTreeNode afterBuildBindingNode;
		TaskBindingTreeNode cleanBindingNode;
		TaskBindingTreeNode projectOpenBindingNode;
		Notebook notebook;
		NotebookTab taskOutputTab;

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
			taskRunnerNodeNameField = new DataField<string> ();
			tasksTreeStore = new TreeStore (taskRunnerNodeNameField, taskRunnerField);
			tasksTreeView.DataSource = tasksTreeStore;

			var column = new ListViewColumn ();
			var textCellView = new TextCellView ();
			textCellView.MarkupField = taskRunnerNodeNameField;
			column.Views.Add (textCellView);
			tasksTreeView.Columns.Add (column);

			notebook = new Notebook ();
			notebook.TabOrientation = NotebookTabOrientation.Top;
			paned.Panel2.Content = notebook;
			paned.Panel2.Resize = true;

			bindingsTreeView = new TreeView ();
			bindingsTreeView.HeadersVisible = false;
			notebook.Add (bindingsTreeView, GettextCatalog.GetString ("Bindings"));

			bindingNodeNameField = new DataField<string> ();
			bindingNodeField = new DataField<TaskBindingTreeNode> ();
			bindingsTreeStore = new TreeStore (bindingNodeNameField, bindingNodeField);
			bindingsTreeView.DataSource = bindingsTreeStore;

			column = new ListViewColumn ();
			textCellView = new TextCellView ();
			textCellView.MarkupField = bindingNodeNameField;
			column.Views.Add (textCellView);
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
