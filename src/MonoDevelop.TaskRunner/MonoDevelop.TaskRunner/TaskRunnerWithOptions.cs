//
// TaskRunnerWithOptions.cs
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
using Microsoft.VisualStudio.TaskRunnerExplorer;
using System.Text;

namespace MonoDevelop.TaskRunner
{
	class TaskRunnerWithOptions
	{
		public TaskRunnerWithOptions (ITaskRunnerNode taskRunner, IEnumerable<ITaskRunnerOption> options)
		{
			TaskRunner = taskRunner;
			Options = options;
		}

		public ITaskRunnerNode TaskRunner { get; }
		public IEnumerable<ITaskRunnerOption> Options { get; }

		public void ApplyOptionsToCommand ()
		{
			TaskRunner.Command.Options = BuildOptions ();
		}

		string BuildOptions ()
		{
			if (Options == null)
				return null;

			var optionsBuilder = new StringBuilder ();
			foreach (ITaskRunnerOption option in Options) {
				if (option.Checked) {
					optionsBuilder.Append (option.Command);
					optionsBuilder.Append (' ');
				}
			}
			return optionsBuilder.ToString ().TrimEnd ();
		}
	}
}
