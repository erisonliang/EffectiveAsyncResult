using System;
using System.Threading.Tasks;
using EffectiveAsyncResult;
using Xunit;

namespace Tests
{
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
}