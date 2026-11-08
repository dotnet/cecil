//
// Author:
//   Juerg Billeter (j@bitron.ch)
//
// (C) 2008 Juerg Billeter
//
// Licensed under the MIT/X11 license.
//

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
#if NET10_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

using Mono.Cecil.Cil;

// Avoid Mono.Cecil.InterfaceImplementation shadowing the COM generator's file-local interface.
namespace Microsoft.Cci.Pdb {

	[Guid ("0B97726E-9E6D-4f05-9A26-424022093CAA")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
#if NET10_0_OR_GREATER
	[GeneratedComInterface (StringMarshalling = StringMarshalling.Utf16, Options = ComInterfaceOptions.ComObjectWrapper)]
#else
	[ComImport]
#endif
	partial interface ISymUnmanagedWriter2 {

		void DefineDocument (
			[MarshalAs (UnmanagedType.LPWStr)] string url,
			ref Guid langauge,
			ref Guid languageVendor,
			ref Guid documentType,
#if NET10_0_OR_GREATER
			[MarshalUsing (typeof (UniqueComInterfaceMarshaller<ISymUnmanagedDocumentWriter>))] out ISymUnmanagedDocumentWriter pRetVal);
#else
			[MarshalAs (UnmanagedType.Interface)] out ISymUnmanagedDocumentWriter pRetVal);
#endif
		void SetUserEntryPoint (int methodToken);
		void OpenMethod (int methodToken);
		void CloseMethod ();
		void OpenScope (int startOffset, out int pRetVal);
		void CloseScope (int endOffset);
		void SetScopeRange_Placeholder ();
		void DefineLocalVariable_Placeholder ();
		void DefineParameter_Placeholder ();
		void DefineField_Placeholder ();
		void DefineGlobalVariable_Placeholder ();
		void Close ();
		void SetSymAttribute (uint parent, [MarshalAs (UnmanagedType.LPWStr)] string name, uint data, IntPtr signature);
		void OpenNamespace ([MarshalAs (UnmanagedType.LPWStr)] string name);
		void CloseNamespace ();
		void UsingNamespace ([MarshalAs (UnmanagedType.LPWStr)] string fullName);
		void SetMethodSourceRange_Placeholder ();
		void Initialize (
#if NET10_0_OR_GREATER
			IntPtr emitter,
#else
			[In, MarshalAs (UnmanagedType.IUnknown)] object emitter,
#endif
			[MarshalAs (UnmanagedType.LPWStr)] string filename,
#if NET10_0_OR_GREATER
			IntPtr pIStream,
#else
			[In] IStream pIStream,
#endif
			[MarshalAs (UnmanagedType.Bool)] bool fFullBuild);
		void GetDebugInfo (
			out ImageDebugDirectory pIDD,
			int cData,
			out int pcData,
			[In, Out, MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 1)] byte [] data);
		void DefineSequencePoints (
			[MarshalAs (UnmanagedType.Interface)] ISymUnmanagedDocumentWriter document,
			int spCount,
			[In, MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 1)] int [] offsets,
			[In, MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 1)] int [] lines,
			[In, MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 1)] int [] columns,
			[In, MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 1)] int [] endLines,
			[In, MarshalAs (UnmanagedType.LPArray, SizeParamIndex = 1)] int [] endColumns);
		void RemapToken_Placeholder ();
		void Initialize2_Placeholder ();
		void DefineConstant_Placeholder ();
		void Abort_Placeholder ();

		void DefineLocalVariable2 (
			[MarshalAs (UnmanagedType.LPWStr)] string name,
			int attributes,
			int sigToken,
			int addrKind,
			int addr1,
			int addr2,
			int addr3,
			int startOffset,
			int endOffset);

		void DefineGlobalVariable2_Placeholder ();

		void DefineConstant2 (
			[MarshalAs (UnmanagedType.LPWStr)] string name,
#if NET10_0_OR_GREATER
			ComVariant variant,
#else
			[In, MarshalAs (UnmanagedType.Struct)] object variant,
#endif
			int sigToken);
	}
}
