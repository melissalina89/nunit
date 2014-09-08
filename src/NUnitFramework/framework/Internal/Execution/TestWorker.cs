﻿// ***********************************************************************
// Copyright (c) 2012 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

#if !NUNITLITE
using System;
using System.Threading;

namespace NUnit.Framework.Internal.Execution
{
    /// <summary>
    /// A TestWorker pulls work items from a queue
    /// and executes them.
    /// </summary>
    public class TestWorker
    {
        static Logger log = InternalTrace.GetLogger("TestWorker");

        private WorkItemQueue _readyQueue;
        private Thread _workerThread;

        private int _workItemCount = 0;

        private bool _running;

        /// <summary>
        /// Event signaled immediately before executing a WorkItem
        /// </summary>
        public event EventHandler Busy;

        /// <summary>
        /// Event signaled immediately after executing a WorkItem
        /// </summary>
        public event EventHandler Idle;

        /// <summary>
        /// Construct a new TestWorker.
        /// </summary>
        /// <param name="queue">The queue from which to pull work items</param>
        /// <param name="name">The name of this worker</param>
        /// <param name="apartmentState">The apartment state to use for running tests</param>
        public TestWorker(WorkItemQueue queue, string name, ApartmentState apartmentState)
        {
            _readyQueue = queue;

            _workerThread = new Thread(new ThreadStart(TestWorkerThreadProc));
            _workerThread.Name = name;
            _workerThread.SetApartmentState(apartmentState);
        }

        /// <summary>
        /// The name of this worker - also used for the thread
        /// </summary>
        public string Name { get { return _workerThread.Name; } }

        /// <summary>
        /// Indicates whether the worker thread is running
        /// </summary>
        public bool IsAlive { get { return _workerThread.IsAlive; } }

        /// <summary>
        /// Our ThreadProc, which pulls and runs tests in a loop
        /// </summary>
        void TestWorkerThreadProc()
        {
            log.Info("{0} starting ", _workerThread.Name);

            _running = true;

            try
            {
                while (_running)
                {
                    var workItem = _readyQueue.Dequeue();
                    if (workItem == null)
                        break;

                    log.Info("{0} executing {1}", _workerThread.Name, workItem.Test.Name);

                    if (Busy != null) Busy(this, EventArgs.Empty);
                    workItem.Execute();
                    if (Idle != null) Idle(this, EventArgs.Empty);

                    ++_workItemCount;
                }
            }
            finally
            {
                log.Info("{0} stopping - {1} WorkItems processed.", _workerThread.Name, _workItemCount);
            }
        }

        /// <summary>
        /// Start processing work items.
        /// </summary>
        public void Start()
        {
            _workerThread.Start();
        }

        /// <summary>
        /// Stop the thread, either immediately or after finishing the current WorkItem
        /// </summary>
        public void Cancel()
        {
            _running = false;

            if (_workerThread != null && _workerThread.IsAlive)
                ThreadUtility.Kill(_workerThread);
        }
    }
}
#endif