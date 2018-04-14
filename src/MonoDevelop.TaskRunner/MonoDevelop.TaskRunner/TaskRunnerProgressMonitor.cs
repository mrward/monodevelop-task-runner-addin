﻿//
// TaskRunnerProgressMonitor.cs
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

using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System.Text;

namespace MonoDevelop.TaskRunner
{
	class TaskRunnerProgressMonitor : OutputProgressMonitor
	{
		OutputProgressMonitor outputProgressMonitor;
		StringBuilder standardOutput = new StringBuilder ();
		StringBuilder standardError = new StringBuilder ();

		public TaskRunnerProgressMonitor ()
		{
			outputProgressMonitor = CreateProgressMonitor ();
			AddFollowerMonitor (outputProgressMonitor);
		}

		public override OperationConsole Console {
			get { return outputProgressMonitor.Console; }
		}

		OutputProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Task Runner Output"),
				Stock.Console,
				false,
				true
			);
		}

		protected override void OnWriteLog (string message)
		{
			standardOutput.Append (message);
			base.OnWriteLog (message);
		}

		protected override void OnWriteErrorLog (string message)
		{
			standardError.Append (message);
			base.OnWriteErrorLog (message);
		}

		public string GetStandardErrorText ()
		{
			return standardError.ToString ();
		}

		public string GetStandardOutputText ()
		{
			return standardOutput.ToString ();
		}
	}
}