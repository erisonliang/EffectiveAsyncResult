using System;
using System.Threading;
using EffectiveAsyncResult;
using PRI.ProductivityExtensions.ReflectionExtensions;
using Xunit;

namespace Tests
{
	public class when_asyncressult_completes_on_different_thread : IDisposable
	{
		private AsyncResult<int> asyncResult;
		private int callbackWasInvoked;
		private const int ExpectedResult = 42;

		public when_asyncressult_completes_on_different_thread()
		{
			asyncResult = new AsyncResult<int>(ar => Thread.VolatileWrite(ref callbackWasInvoked, 1), null);
			ThreadPool.QueueUserWorkItem(_ => asyncResult.Complete(true, ExpectedResult));
			// give the thread-pool thread time to invoke the callback
			Thread.Sleep(100);
		}

		[Fact]
		public void then_completion_causes_callback_invocation()
		{
			Assert.Equal(1, callbackWasInvoked);
		}

		[Fact]
		public void then_result_is_correct()
		{
			Assert.Equal(ExpectedResult, asyncResult.End());
		}

		[Fact]
		public void then_unobserved_waithandle_field_is_null()
		{
			var value = asyncResult.GetPrivateFieldValue<WaitHandle>("asyncWaitHandle");
			Assert.Null(value);
		}

		public void Dispose()
		{
			using (asyncResult)
			{
				asyncResult = null;
			}
		}
	}
}