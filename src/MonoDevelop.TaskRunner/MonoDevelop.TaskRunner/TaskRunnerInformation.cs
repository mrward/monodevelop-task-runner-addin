//
// TaskRunnerInformation.cs
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
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.TaskRunner
{
	class TaskRunnerInformation
	{
		public TaskRunnerInformation (
			IWorkspaceFileObject workspaceFileObject,
			ITaskRunnerConfig config,
			IEnumerable<ITaskRunnerOption> options,
			FilePath configFile)
		{
			WorkspaceFileObject = workspaceFileObject;
			Config = config;
			Options = options;
			ConfigFile = configFile;

			Name = configFile.FileName;
			Bindings = new TaskRunnerBindings (Config, configFile);
		}

		public string Name { get; private set; }
		public IWorkspaceFileObject WorkspaceFileObject { get; set; }
		public FilePath ConfigFile { get; private set; }
		public IEnumerable<ITaskRunnerOption> Options { get; private set; }
		public ITaskRunnerConfig Config { get; private set; }
		public TaskRunnerBindings Bindings { get; private set; }

		public ITaskRunnerNode TaskHierarchy => Config?.TaskHierarchy;

		public bool IsBindingEnabled (TaskRunnerBindEvent bindEvent, ITaskRunnerNode taskRunnerNode)
		{
			return Bindings.IsBindingEnabled (bindEvent, taskRunnerNode);
		}

		public void ToggleBinding (TaskRunnerBindEvent bindEvent, ITaskRunnerNode taskRunnerNode)
		{
			Bindings.ToggleBinding (bindEvent, taskRunnerNode);
		}

		public bool RemoveBinding (TaskRunnerBindEvent bindEvent, string name)
		{
			return Bindings.RemoveBinding (bindEvent, name);
		}
	}
}
