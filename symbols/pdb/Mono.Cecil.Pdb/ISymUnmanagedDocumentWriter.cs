// Author:
//   Juerg Billeter (j@bitron.ch)
//
// (C) 2008 Juerg Billeter
//
// Licensed under the MIT/X11 license.
//

using System;
using System.Runtime.InteropServices;
#if NET10_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace Microsoft.Cci.Pdb {

	[Guid ("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
#if NET10_0_OR_GREATER
	[GeneratedComInterface (Options = ComInterfaceOptions.ComObjectWrapper)]
#else
	[ComImport]
#endif
	partial interface ISymUnmanagedDocumentWriter {
		void SetSource (uint sourceSize, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 0)] byte [] source);
		void SetCheckSum (Guid algorithmId, uint checkSumSize, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 1)] byte [] checkSum);
	}
}
