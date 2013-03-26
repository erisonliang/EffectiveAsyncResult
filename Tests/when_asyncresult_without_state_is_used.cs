using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EffectiveAsyncResult;
using Xunit;

namespace Tests
{
	public class when_asyncresult_without_state_is_used : IDisposable
	{
		private AsyncResult<int> asyncResult;
		private int callbackWasInvoked;

		public when_asyncresult_without_state_is_used()
		{
			asyncResult = new AsyncResult<int>(ar => Thread.VolatileWrite(ref callbackWasInvoked, 1), null);
		}

		[Fact]
		public void then_newly_created_asyncresult_waithandle_field_is_null()
		{
			var value = asyncResult.GetPrivateFieldValue<WaitHandle>("asyncWaitHandle");
			Assert.Null(value);
		}

		[Fact]
		public void then_has_value_after_completion()
		{
			var expectedResult = 42;
			asyncResult.Complete(true, expectedResult);
			Assert.Equal(expectedResult, asyncResult.End());
		}

		[Fact]
		public void then_completion_cause_calback_invocation()
		{
			asyncResult.Complete(true, 42);
			// give the thread-pool thread time to invoke the callback
			Thread.Sleep(100);
			Assert.Equal(1, callbackWasInvoked);
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
			// force creation of the WaitHandle;
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

	public class when_asyncresult_is_used_with_operation_throwing_exception
	{
		private AsyncResult<string> asyncResult;

		public when_asyncresult_is_used_with_operation_throwing_exception()
		{
			asyncResult =
				new AsyncResult<string>(ar => { }, null);
			Task.Factory.StartNew(() => asyncResult.Complete(true, new InvalidOperationException()));
		}

		[Fact]
		public void then_exceptions_are_thrown()
		{
			Assert.Throws<InvalidOperationException>(() => asyncResult.End());
		}

	}

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

	public static class Extensions
	{
		public static T GetPrivateFieldValue<T>(this Object obj, string fieldName)
		{
			if (obj == null) throw new ArgumentNullException("obj");
			if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentNullException("fieldName");
			var type = obj.GetType();
			FieldInfo field;
			do
			{
				field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				type = type.BaseType;
			} while (field == null && type != null);
			if (field == null)
				throw new InvalidOperationException(string.Format("{0} not found in type {1}", fieldName, type.FullName));
			if (!typeof(T).IsAssignableFrom(field.FieldType))
				throw
					new InvalidOperationException(string.Format("{0} is not assignable from {1}", typeof(T).FullName,
																field.FieldType.FullName));
			return (T)field.GetValue(obj);
		}
	}

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
