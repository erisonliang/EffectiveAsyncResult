using System;
using System.Threading;
using EffectiveAsyncResult;
using PRI.ProductivityExtensions.ReflectionExtensions;
using Xunit;

namespace Tests
{
	public class when_asyncresult_with_state_is_used : IDisposable
	{
		private AsyncResult<string> asyncResult;
		private int callbackWasInvoked;
		private static readonly object TheStateObject = new object();

		public when_asyncresult_with_state_is_used()
		{
			asyncResult = new AsyncResult<string>(ar => Thread.VolatileWrite(ref callbackWasInvoked, 1), TheStateObject);
		}

		[Fact]
		public void then_has_correct_state_before_completion()
		{
			Assert.Same(TheStateObject, asyncResult.AsyncState);
		}

		[Fact]
		public void then_has_correct_state_after_completion()
		{
			asyncResult.Complete(true);
			Assert.Same(TheStateObject, asyncResult.AsyncState);
		}

		[Fact]
		public void then_has_correct_state_after_completion_with_result()
		{
			var expectedResult = "42";
			asyncResult.Complete(true, expectedResult);
			Assert.Same(TheStateObject, asyncResult.AsyncState);
		}

		[Fact]
		public void then_has_value_after_completion()
		{
			var expectedResult = "42";
			asyncResult.Complete(true, expectedResult);
			Assert.Equal(expectedResult, asyncResult.End());
		}

		[Fact]
		public void then_newly_created_asyncresult_waithandle_field_is_null()
		{
			var value = asyncResult.GetPrivateFieldValue<WaitHandle>("asyncWaitHandle");
			Assert.Null(value);
		}

		[Fact]
		public void then_newly_created_asyncresult_waithandle_property_is_not_set()
		{
			Assert.False(asyncResult.AsyncWaitHandle.WaitOne(0));
		}

		[Fact]
		public void then_completed_asyncresult_waithandle_field_is_null()
		{
			asyncResult.Complete(true);
			var value = asyncResult.GetPrivateFieldValue<WaitHandle>("asyncWaitHandle");
			Assert.Null(value);
		}

		[Fact]
		public void then_completed_asyncresult_waithandle_property_is_set()
		{
			asyncResult.Complete(true);
			Assert.True(asyncResult.AsyncWaitHandle.WaitOne(0));
		}

		[Fact]
		public void then_completed_asyncresult_waithandle_property_is_set_after_waithandle_created()
		{
			var temp = asyncResult.AsyncWaitHandle;
			asyncResult.Complete(true);
			Assert.True(asyncResult.AsyncWaitHandle.WaitOne(0));
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