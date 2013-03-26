using System;
using System.Threading;

namespace EffectiveAsyncResult
{
	public class AsyncResult<T> : AsyncResult
	{
		private T Result { get; set; }

		public AsyncResult(AsyncCallback cb, object state)
			: base(cb, state)
		{
		}

		public void Complete(bool didCompleteSynchronously, T result)
		{
			Result = result;
			Thread.MemoryBarrier();
			Complete(didCompleteSynchronously);
		}

		public new T End()
		{
			base.End();
			return Result;
		}
	}

	public class AsyncResult : IAsyncResult, IDisposable
	{
#if !NET_4_5
		private static class Volatile
		{
			public static T Read<T>(ref T location)
			{
				T t = location;
				Thread.MemoryBarrier();
				return t;
			}
		}
#endif
		// ReSharper disable InconsistentNaming
		private const int FALSE = 0;
		private const int TRUE = 1;
		// ReSharper restore InconsistentNaming
		private readonly AsyncCallback callback;
		private int completed;
		private int completedSynchronously;
		private int eventHasBeenSet;
		private ManualResetEvent asyncWaitHandle;
		protected Exception Exception;

		private bool UpdateEventHasBeenSetIfNeeded()
		{
			return Interlocked.Exchange(ref eventHasBeenSet, TRUE) == FALSE;
		}

		internal AsyncResult(AsyncCallback cb, object state)
			: this(cb, state, false)
		{
		}

		private AsyncResult(AsyncCallback cb, object state, bool completed)
		{
			callback = cb;
			AsyncState = state;
			this.completed = completed ? TRUE : FALSE;
			completedSynchronously = this.completed;
		}

		public object AsyncState { get; private set; }

		public WaitHandle AsyncWaitHandle
		{
			get
			{
				var manualResetEvent = Volatile.Read(ref asyncWaitHandle);
				if (manualResetEvent == null)
				{
					manualResetEvent = new ManualResetEvent(IsCompleted);
					if (Interlocked.CompareExchange(ref asyncWaitHandle, manualResetEvent, null) != null)
					{
						// another thread beat us to creating the event, dispose of this one.
						using (manualResetEvent)
						{
						}
					}
					else
					{
						manualResetEvent = Volatile.Read(ref asyncWaitHandle);
					}
				}
				return manualResetEvent;
			}
		}

		public bool CompletedSynchronously
		{
			get { return Thread.VolatileRead(ref completedSynchronously) != FALSE; }
		}

		public bool IsCompleted
		{
			get { return Thread.VolatileRead(ref completed) != FALSE; }
		}

		public void Dispose()
		{
			using (asyncWaitHandle)
			{
			}
		}

		public void Complete(bool didCompleteSynchronously)
		{
			Thread.VolatileWrite(ref completed, TRUE);
			Thread.VolatileWrite(ref completedSynchronously, didCompleteSynchronously ? TRUE : FALSE);

			SignalCompletion();
		}

		public void Complete(bool didCompleteSynchronously, Exception exception)
		{
			Exception = exception;
			Complete(didCompleteSynchronously);
		}

		private void SignalCompletion()
		{
			var manualResetEvent = Volatile.Read(ref asyncWaitHandle);
			if (manualResetEvent != null && UpdateEventHasBeenSetIfNeeded())
				manualResetEvent.Set();

			ThreadPool.QueueUserWorkItem(InvokeCallback);
		}

		private void InvokeCallback(object state)
		{
			if (callback != null)
			{
				callback(this);
			}
		}

		public void End()
		{
			if (Exception != null) throw Exception;
			if (!IsCompleted) AsyncWaitHandle.WaitOne();
			if (Exception != null) throw Exception;
		}
	}
}
