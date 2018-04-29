//
// TaskRunnerCommandService.cs
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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.TaskRunner
{
	class TaskRunnerCommandService : ITaskRunnerCommandService
	{
		ProcessAsyncOperation currentOperation;
		OutputProgressMonitor outputProgressMonitor;

		public TaskRunnerCommandService (OutputProgressMonitor outputProgressMonitor)
		{
			this.outputProgressMonitor = outputProgressMonitor;
		}

		public async Task<ITaskRunnerCommandResult> ExecuteCommand (ITaskRunnerCommand command)
		{
			using (var monitor = new TaskRunnerProgressMonitor (outputProgressMonitor)) {
				monitor.Log.WriteLine (command.ToCommandLine ());

				var result = Runtime.ProcessService.StartConsoleProcess (
					command.Executable,
					command.Args,
					command.WorkingDirectory,
					monitor.Console);

				currentOperation = result;

				await result.Task;

				currentOperation = null;

				string message = GettextCatalog.GetString ("Process terminated with code {0}", result.ExitCode);
				monitor.Log.WriteLine (message);

				return new TaskRunnerCommandResult {
					StandardOutput = monitor.GetStandardOutputText (),
					StandardError = monitor.GetStandardErrorText (),
					ExitCode = result.ExitCode
				};
			}
		}

		public void Stop ()
		{
			if (currentOperation != null) {
				currentOperation.Cancel ();
			}
		}
	}
}
