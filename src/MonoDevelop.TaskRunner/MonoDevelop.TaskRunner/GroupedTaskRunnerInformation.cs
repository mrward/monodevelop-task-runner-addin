//
// GroupedTaskRunnerInformation.cs
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
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.TaskRunner
{
	class GroupedTaskRunnerInformation
	{
		List<TaskRunnerInformation> tasks;

		public GroupedTaskRunnerInformation (
			IWorkspaceFileObject workspaceFileObject,
			IEnumerable<TaskRunnerInformation> tasks)
		{
			WorkspaceFileObject = workspaceFileObject;
			this.tasks = tasks.ToList ();
			Name = GetName (workspaceFileObject);
		}

		public GroupedTaskRunnerInformation (IWorkspaceFileObject workspaceFileObject)
			: this (workspaceFileObject, Enumerable.Empty<TaskRunnerInformation> ())
		{
		}

		static string GetName (IWorkspaceFileObject workspaceFileObject)
		{
			if (workspaceFileObject is Solution solution) {
				return GettextCatalog.GetString ("Solution '{0}'", solution.Name);
			}

			return workspaceFileObject.Name;
		}

		public string Name { get; private set; }
		public IWorkspaceFileObject WorkspaceFileObject { get; private set; }

		public IEnumerable<TaskRunnerInformation> Tasks {
			get { return tasks; }
		}

		public IEnumerable<TaskRunnerWithOptions> GetTasks (TaskRunnerBindEvent bindEvent)
		{
			foreach (TaskRunnerInformation task in Tasks) {
				foreach (TaskRunnerBindingInformation binding in task.Bindings) {
					if (binding.BindEvent == bindEvent) {
						foreach (string taskName in binding.GetTasks ()) {
							ITaskRunnerNode node = task.GetInvokableTask (taskName);
							if (node != null) {
								yield return new TaskRunnerWithOptions (node, task.Options);
							}
						}
					}
				}
			}
		}

		public void AddTask (TaskRunnerInformation task)
		{
			tasks.Add (task);
		}
	}
}
