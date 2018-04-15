//
// TaskBindingTreeNode.cs
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
using System.Linq;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;

namespace MonoDevelop.TaskRunner.Gui
{
	class TaskBindingTreeNode
	{
		TaskRunnerInformation task;
		TaskRunnerBindingInformation binding;
		int bindingsCount = 0;
		bool hasChildren;

		public TaskBindingTreeNode (TaskRunnerBindEvent bindEvent)
		{
			BindEvent = bindEvent;
			RefreshName ();
			IsRootNode = true;
		}

		TaskBindingTreeNode (TaskRunnerInformation task, TaskRunnerBindingInformation binding)
		{
			this.task = task;
			this.binding = binding;

			Name = task.Name;
			hasChildren = true;
		}

		TaskBindingTreeNode (TaskRunnerInformation task, TaskRunnerBindingInformation binding, string taskName)
		{
			this.task = task;
			this.binding = binding;

			Name = taskName;
			hasChildren = false;
		}

		void RefreshName ()
		{
			Name = GetName ();
		}

		string GetName ()
		{
			return GetNamePrefix ().ToBoldMarkup () + $" ({bindingsCount})";
		}

		string GetNamePrefix ()
		{
			switch (BindEvent) {
				case TaskRunnerBindEvent.BeforeBuild:
					return GettextCatalog.GetString ("Before Build");
				case TaskRunnerBindEvent.AfterBuild:
					return GettextCatalog.GetString ("After Build");
				case TaskRunnerBindEvent.Clean:
					return GettextCatalog.GetString ("Clean");
				case TaskRunnerBindEvent.ProjectOpened:
					return GettextCatalog.GetString ("Project Open");
				default:
					return GettextCatalog.GetString ("Unknown bind event {0}", (int)BindEvent);
			}
		}

		public TaskRunnerBindEvent BindEvent { get; private set; }
		public string Name { get; private set; }
		public bool IsRootNode { get; private set; }

		public IEnumerable<TaskBindingTreeNode> CreateChildNodes (
			TaskRunnerInformation task,
			TaskRunnerBindingInformation binding)
		{
			if (IsRootNode) {
				bindingsCount += binding.GetTasks ().Count ();
				RefreshName ();
				yield return new TaskBindingTreeNode (task, binding);
			} else if (hasChildren) {
				foreach (string taskName in binding.GetTasks ()) {
					yield return new TaskBindingTreeNode (task, binding, taskName);
				}
			}
		}
	}
}
