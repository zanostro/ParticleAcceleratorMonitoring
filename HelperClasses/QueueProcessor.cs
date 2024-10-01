using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParticleAcceleratorMonitoring
{


    internal class QueueProcessor<T>
    {
        /*
        A generic class that manages and processes tasks in a thread-safe way. 

        Main Features:
        - `singleQueue` and `multiQueue`: Queues for storing tasks (single elements or arrays).
        - Locks (`_queueLock` and `_taskLock`): Ensure safe access to queues and tasks in multithreaded environments.
        - `TaskCompletionSource<bool>`: Signals when all tasks in the queue are processed. You can wait for it to complete using `WaitForCompletionAsync()`.

        Use Case:
        - Ideal for scenarios where tasks need to be queued and processed one by one or in batches, 
          with thread-safety and the ability to wait for completion.
        */

        private readonly Action<T> singleTask;
        private readonly Action<T[]> multiTask;

        private readonly Queue<T> singleQueue = new Queue<T>();
        private readonly Queue<T[]> multiQueue = new Queue<T[]>();

        private readonly object _queueLock = new object();
        private bool processingQueue = false;

        private readonly object _taskLock;

        // TaskCompletionSource to signal when queue processing is complete
        private TaskCompletionSource<bool> processingCompleteTcs = new TaskCompletionSource<bool>();


        public QueueProcessor(Action<T> task, object _lock)
        {
            this._taskLock = _lock;
            this.singleTask = task;
            this.processingCompleteTcs.SetResult(true); // Initially set to true (no tasks)
        }

        
        public QueueProcessor(Action<T[]> task, object _lock)
        {
            this._taskLock = _lock;
            this.multiTask = task;
            this.processingCompleteTcs.SetResult(true); // Initially set to true (no tasks)
        }

        // wait for 
        public Task WaitForCompletionAsync()
        {
            return processingCompleteTcs.Task;
        }


        private void ProcessSingleQueue()
        {
            while (true)
            {
                T element = default(T);

                lock (_queueLock)
                {
                    if (singleQueue.Count > 0)
                    {
                        element = singleQueue.Dequeue();
                    }
                    else
                    {
                        processingQueue = false;
                        processingCompleteTcs.TrySetResult(true); // Signal completion
                        break;
                    }
                }

                lock (_taskLock)
                {
                    singleTask(element); // Call the single-argument task
                }
            }
        }

        
        private void ProcessMultiQueue()
        {
            while (true)
            {
                T[] args = null;

                lock (_queueLock)
                {
                    if (multiQueue.Count > 0)
                    {
                        args = multiQueue.Dequeue();
                    }
                    else
                    {
                        processingQueue = false;
                        processingCompleteTcs.TrySetResult(true); // Signal completion
                        break;
                    }
                }

                lock (_taskLock)
                {
                    multiTask(args); // Call the multi-argument task
                }
            }
        }

        public void AddToQueue(T element)
        {
            if (singleTask == null) throw new InvalidOperationException("Single-task is not defined!");

            lock (_queueLock)
            {
                singleQueue.Enqueue(element);
            }

            processingCompleteTcs = new TaskCompletionSource<bool>(); // reset the TCS (processing not complete)

            // Process the queue if not already processing
            if (!processingQueue)
            {
                processingQueue = true;
                ProcessSingleQueue();
            }
        }

        public void AddToQueue(params T[] args)
        {
            if (multiTask == null) throw new InvalidOperationException("Multi-task is not defined!");

            lock (_queueLock)
            {
                multiQueue.Enqueue(args);
            }

            processingCompleteTcs = new TaskCompletionSource<bool>(); // reset the TCS (processing not complete)

            // Process the queue if not already processing
            if (!processingQueue)
            {
                processingQueue = true;
                ProcessMultiQueue();
            }
        }
    }
}
