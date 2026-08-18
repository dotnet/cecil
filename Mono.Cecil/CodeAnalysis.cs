#if !NET
using System;

namespace System.Diagnostics.CodeAnalysis {

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
	sealed class RequiresUnreferencedCodeAttribute : Attribute {

		public RequiresUnreferencedCodeAttribute (string message)
		{
			Message = message;
		}

		public string Message { get; }

		public string Url { get; set; }
	}
}
#endif
