//
// TaskRunnerServices.cs
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
using MonoDevelop.Ide.Composition;

namespace MonoDevelop.TaskRunner
{
	static class TaskRunnerServices
	{
		static TaskRunnerWorkspace workspace;
		static TaskRunnerProvider taskRunnerProvider;
		static TaskRunnerExplorerOptions options;
		static TaskRunnerLoggingService loggingService;

		public static TaskRunnerWorkspace Workspace {
			get { return workspace; }
		}

		public static TaskRunnerExplorerOptions Options {
			get { return options; }
		}

		public static bool AutomaticallyRunTasks {
			get { return options.RunTasksAutomatically; }
		}

		public static TaskRunnerLoggingService LoggingService {
			get { return loggingService; }
		}

		static internal void Initialize ()
		{
			try {
				InitializeServices ();
			} catch (Exception ex) {
				Core.LoggingService.LogInfo ("TaskRunnerServices.Initialize error", ex);
			}
		}

		static void InitializeServices ()
		{
			loggingService = new TaskRunnerLoggingService ();
			options = new TaskRunnerExplorerOptions ();

			taskRunnerProvider = CompositionManager.Instance.GetExportedValue<TaskRunnerProvider> ();
			taskRunnerProvider.Initialize ();

			workspace = new TaskRunnerWorkspace (taskRunnerProvider);
		}
	}
}
