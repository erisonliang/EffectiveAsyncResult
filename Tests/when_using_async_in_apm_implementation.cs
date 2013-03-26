using System;
using System.Threading;
using EffectiveAsyncResult;
using Xunit;

namespace Tests
{
	public class when_using_async_in_apm_implementation
	{
		internal sealed class LongTask
		{
			private Int32 m_ms; // Milliseconds;

			public LongTask(Int32 seconds)
			{
				m_ms = seconds * 1000;
			}

			// Synchronous version of time-consuming method
			public DateTime DoTask()
			{
				Thread.Sleep(m_ms); // Simulate time-consuming task
				return DateTime.Now; // Indicate when task completed 
			}

			// Asynchronous version of time-consuming method (Begin part)
			public IAsyncResult BeginDoTask(AsyncCallback callback, Object state)
			{
				// Create IAsyncResult object identifying the 
				// asynchronous operation 
				AsyncResult<DateTime> ar = new AsyncResult<DateTime>(callback, state);
				// Use a thread pool thread to perform the operation
				ThreadPool.QueueUserWorkItem(DoTaskHelper, ar);
				return ar; // Return the IAsyncResult to the caller 
			}

			// Asynchronous version of time-consuming method (End part)
			public DateTime EndDoTask(IAsyncResult asyncResult)
			{
				// We know that the IAsyncResult is really an 
				// AsyncResult<DateTime> object 
				AsyncResult<DateTime> ar = (AsyncResult<DateTime>)asyncResult;
				// Wait for operation to complete, then return result or 
				// throw exception 
				return ar.End();
			}

			// Asynchronous version of time-consuming method (private part 
			// to set completion result/exception) 
			private void DoTaskHelper(Object asyncResult)
			{
				// We know that it's really an AsyncResult<DateTime> object 
				AsyncResult<DateTime> ar = (AsyncResult<DateTime>)asyncResult;
				try
				{
					// Perform the operation; if sucessful set the result
					DateTime dt = DoTask();
					ar.Complete(false, dt);
				}
				catch (Exception e)
				{
					// If operation fails, set the exception
					ar.Complete(false, e);
				}
			}
		}

		[Fact]
		public void then_long_task_is_not_completed_directly_after_begin()
		{
			const int seconds = 2;

			LongTask lt = new LongTask(seconds); // Prove that the Wait-until-done technique works
			IAsyncResult ar = lt.BeginDoTask(null, null);
			ar = lt.BeginDoTask(null, null);
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			Assert.False(ar.IsCompleted);
		}

		[Fact]
		public void then_long_task_is_completed_at_correct_time()
		{
			const int seconds = 2;

			LongTask lt = new LongTask(seconds); // Prove that the Wait-until-done technique works
			IAsyncResult ar = lt.BeginDoTask(null, null);
			ar = lt.BeginDoTask(null, null);
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			while (!ar.IsCompleted)
			{
				Thread.Sleep(1000);
			}
			Assert.InRange(stopwatch.Elapsed.TotalSeconds, seconds, int.MaxValue);
		}
	}
}