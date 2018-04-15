//
// TaskRunnerBindings.cs
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;

namespace MonoDevelop.TaskRunner
{
	//
	// Bindings:
	// <binding ProjectOpen="task1, task2" AfterBuild="task3" />
	//
	class TaskRunnerBindings : IEnumerable<TaskRunnerBindingInformation>
	{
		ITaskRunnerConfig config;
		FilePath configFile;
		List<TaskRunnerBindingInformation> bindings = new List<TaskRunnerBindingInformation> ();

		public TaskRunnerBindings (ITaskRunnerConfig config, FilePath configFile)
		{
			this.config = config;
			this.configFile = configFile;

			if (config != null) {
				Init ();
			}
		}

		void Init ()
		{
			string bindingsXml = config.LoadBindings (configFile);
			if (string.IsNullOrEmpty (bindingsXml))
				return;

			XElement bindingsElement = XElement.Parse (bindingsXml);
			foreach (XAttribute attribute in bindingsElement.Attributes ()) {
				var binding = new TaskRunnerBindingInformation (attribute);
				bindings.Add (binding);
			}
		}

		public bool IsBindingEnabled (TaskRunnerBindEvent bindEvent, ITaskRunnerNode taskRunnerNode)
		{
			TaskRunnerBindingInformation binding = FindBinding (bindEvent);
			if (binding != null) {
				return binding.HasTask (taskRunnerNode);
			}

			return false;
		}

		TaskRunnerBindingInformation FindBinding (TaskRunnerBindEvent bindEvent)
		{
			return bindings.FirstOrDefault (binding => binding.BindEvent == bindEvent);
		}

		public void ToggleBinding (TaskRunnerBindEvent bindEvent, ITaskRunnerNode task)
		{
			if (config == null) {
				return;
			}

			TaskRunnerBindingInformation binding = FindBinding (bindEvent);
			if (binding != null) {
				binding.ToggleTask (task);
				if (!binding.AnyTasks ()) {
					bindings.Remove (binding);
				}
			} else {
				binding = new TaskRunnerBindingInformation (bindEvent, task);
				bindings.Add (binding);
			}

			config.SaveBindings (configFile, ToXml ());
		}

		string ToXml ()
		{
			XElement bindingsElement = XElement.Parse ("<binding />");

			foreach (TaskRunnerBindingInformation binding in bindings) {
				string tasks = string.Join (",", binding.GetTasks ());
				bindingsElement.SetAttributeValue (binding.BindEvent.ToString (), tasks);
			}

			return bindingsElement.ToString ();
		}

		public IEnumerator<TaskRunnerBindingInformation> GetEnumerator ()
		{
			return bindings.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return bindings.GetEnumerator ();
		}
	}
}
