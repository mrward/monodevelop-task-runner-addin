//
// TaskRunnerTreeNode.cs
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

namespace MonoDevelop.TaskRunner.Gui
{
	class TaskRunnerTreeNode
	{
		TaskRunnerInformation taskRunnerInfo;
		ITaskRunnerNode taskRunnerNode;

		public TaskRunnerTreeNode (TaskRunnerInformation task)
		{
			taskRunnerInfo = task;
			Name = task.Name;
		}

		public TaskRunnerTreeNode (ITaskRunnerNode task, bool bold)
		{
			taskRunnerNode = task;
			Name = taskRunnerNode.Name ?? string.Empty;

			if (bold) {
				Name = "<b>" + Name + "</b>";
			}
		}

		public string Name { get; private set; }

		public IEnumerable<TaskRunnerTreeNode> GetChildNodes ()
		{
			if (taskRunnerInfo != null) {
				return GetChildNodes (taskRunnerInfo.TaskHierarchy, bold: true);
			} else if (taskRunnerNode != null) {
				return GetChildNodes (taskRunnerNode);
			}

			return Enumerable.Empty<TaskRunnerTreeNode> ();
		}

		IEnumerable<TaskRunnerTreeNode> GetChildNodes (ITaskRunnerNode task, bool bold = false)
		{
			foreach (var childTask in task.Children) {
				yield return new TaskRunnerTreeNode (childTask, bold);
			}
		}
	}
}
