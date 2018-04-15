//
// TaskRunnerBindingInformation.cs
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
using System.Xml.Linq;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace MonoDevelop.TaskRunner
{
	class TaskRunnerBindingInformation
	{
		TaskRunnerBindEvent bindEvent;
		List<string> taskNames = new List<string> ();

		public TaskRunnerBindingInformation (XAttribute attribute)
		{
			IsValid = Enum.TryParse (attribute.Name.LocalName, true, out bindEvent);
			taskNames = attribute.Value.Split (',').ToList ();
		}

		public TaskRunnerBindingInformation (TaskRunnerBindEvent bindEvent, ITaskRunnerNode task)
		{
			this.bindEvent = bindEvent;
			taskNames.Add (task.Name);
		}

		public bool IsValid { get; private set; } = true;

		public TaskRunnerBindEvent BindEvent {
			get { return bindEvent; }
		}

		public override string ToString ()
		{
			return $"{bindEvent} {string.Join(", ", taskNames)}";
		}

		public IEnumerable<string> GetTasks ()
		{
			return taskNames;
		}

		public bool HasTask (ITaskRunnerNode task)
		{
			return taskNames.Any (name => IsMatch (task, name));
		}

		static bool IsMatch (ITaskRunnerNode task, string name)
		{
			return IsMatch (name, task.Name);
		}

		static bool IsMatch (string name1, string name2)
		{
			return StringComparer.CurrentCultureIgnoreCase.Equals (name1, name2);
		}

		public void ToggleTask (ITaskRunnerNode task)
		{
			int count = taskNames.Count;
			taskNames.RemoveAll (name => IsMatch (task, name));

			if (count == taskNames.Count) {
				taskNames.Add (task.Name);
			}
		}

		public bool AnyTasks ()
		{
			return taskNames.Any ();
		}

		public bool RemoveTask (string task)
		{
			int count = taskNames.Count;

			taskNames.RemoveAll (name => IsMatch (task, name));

			return count != taskNames.Count;
		}
	}
}
