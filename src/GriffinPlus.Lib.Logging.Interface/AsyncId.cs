///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Threading;

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Helper that manages an integer identifier that remains constant along an asynchronous control flow (TPL).
	/// This identifier can help to track asynchronous controls flows across multiple threads.
	/// </summary>
	public static class AsyncId
	{
		private static readonly AsyncLocal<uint> sAsyncId        = new AsyncLocal<uint>();
		private static          int              sAsyncIdCounter = 0;

		/// <summary>
		/// Gets an id that is valid for the entire asynchronous control flow.
		/// It should be queried the first time where the asynchronous path starts.
		/// It starts with 1. When wrapping around it skips 0, so 0 can be safely used to indicate an invalid/unassigned id.
		/// </summary>
		public static uint Current
		{
			get
			{
				unchecked
				{
					uint id = sAsyncId.Value;

					if (id == 0)
					{
						id = (uint)Interlocked.Increment(ref sAsyncIdCounter);
						if (id == 0) id = (uint)Interlocked.Increment(ref sAsyncIdCounter); // handles overflow
						sAsyncId.Value = id;
					}

					return id;
				}
			}
		}
	}

}
