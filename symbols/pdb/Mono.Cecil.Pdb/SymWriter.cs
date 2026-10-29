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
#if NET10_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Microsoft.Cci.Pdb;

namespace Mono.Cecil.Pdb
{
	internal partial class SymWriter
	{
#if NET10_0_OR_GREATER
		[LibraryImport ("ole32.dll")]
		private static partial int CoCreateInstance (ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid riid, out IntPtr ppv);

		static readonly StrategyBasedComWrappers com_wrappers = new StrategyBasedComWrappers ();
#else
		[DllImport("ole32.dll")]
		static extern int CoCreateInstance (
			[In] ref Guid rclsid,
			[In, MarshalAs (UnmanagedType.IUnknown)] object pUnkOuter,
			[In] uint dwClsContext,
			[In] ref Guid riid,
			[Out, MarshalAs (UnmanagedType.Interface)] out object ppv);
#endif

		static Guid s_symUnmangedWriterIID = new Guid("0b97726e-9e6d-4f05-9a26-424022093caa");
		static Guid s_CorSymWriter_SxS_ClassID = new Guid ("108296c1-281e-11d3-bd22-0000f80849bd");

		readonly ISymUnmanagedWriter2 writer;
		readonly Collection<ISymUnmanagedDocumentWriter> documents;
		bool closed = false;

		public SymWriter ()
		{
#if NET10_0_OR_GREATER
			IntPtr pointer;
			Marshal.ThrowExceptionForHR (CoCreateInstance (ref s_CorSymWriter_SxS_ClassID, IntPtr.Zero, 1, ref s_symUnmangedWriterIID, out pointer));
			try {
				writer = (ISymUnmanagedWriter2) com_wrappers.GetOrCreateObjectForComInstance (pointer, CreateObjectFlags.UniqueInstance);
			} finally {
				Marshal.Release (pointer);
			}
#else
			object objWriter;
			Marshal.ThrowExceptionForHR (CoCreateInstance (ref s_CorSymWriter_SxS_ClassID, null, 1, ref s_symUnmangedWriterIID, out objWriter));

			writer = (ISymUnmanagedWriter2) objWriter;
#endif
			documents = new Collection<ISymUnmanagedDocumentWriter> ();
		}

		public byte[] GetDebugInfo (out ImageDebugDirectory idd)
		{
			int size;

			// get size of debug info
			writer.GetDebugInfo (out idd, 0, out size, null);

			byte[] debug_info = new byte[size];
			writer.GetDebugInfo (out idd, size, out size, debug_info);

			return debug_info;
		}

		public void DefineLocalVariable2 (
			string name,
			VariableAttributes attributes,
			int sigToken,
			int addr1,
			int addr2,
			int addr3,
			int startOffset,
			int endOffset)
		{
			writer.DefineLocalVariable2 (name, (int)attributes, sigToken, 1 /* ILOffset*/, addr1, addr2, addr3, startOffset, endOffset);
		}

		public void DefineConstant2 (string name, object value, int sigToken)
		{
#if NET10_0_OR_GREATER
			using (var variant = ComVariantMarshaller.ConvertToUnmanaged (value ?? 0))
				writer.DefineConstant2 (name, variant, sigToken);
#else
			if (value == null) {
				writer.DefineConstant2 (name, 0, sigToken);
				return;
			}

			writer.DefineConstant2 (name, value, sigToken);
#endif
		}

		public void Close ()
		{
			if (closed)
				return;

			closed = true;
			try {
				writer.Close ();
			} finally {
#if NET10_0_OR_GREATER
				((ComObject) (object) writer).FinalRelease ();
				foreach (var document in documents)
					((ComObject) (object) document).FinalRelease ();
#else
#pragma warning disable CA1416 // Validate platform compatibility
				Marshal.ReleaseComObject (writer);

				foreach (var document in documents)
					Marshal.ReleaseComObject (document);
#pragma warning restore CA1416 // Validate platform compatibility
#endif
			}
		}

		public void CloseMethod ()
		{
			writer.CloseMethod ();
		}

		public void CloseNamespace ()
		{
			writer.CloseNamespace ();
		}

		public void CloseScope (int endOffset)
		{
			writer.CloseScope (endOffset);
		}

		public SymDocumentWriter DefineDocument (string url, Guid language, Guid languageVendor, Guid documentType)
		{
			ISymUnmanagedDocumentWriter doc_writer;
			writer.DefineDocument (url, ref language, ref languageVendor, ref documentType, out doc_writer);

			documents.Add (doc_writer);
			return new SymDocumentWriter (doc_writer);
		}

		public void DefineSequencePoints (SymDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns)
		{
			writer.DefineSequencePoints (document.Writer, offsets.Length, offsets, lines, columns, endLines, endColumns);
		}

		public unsafe void Initialize (ModuleMetadata emitter, string filename, bool fFullBuild)
		{
#if NET10_0_OR_GREATER
			var pointer = ComInterfaceMarshaller<IMetaDataEmit>.ConvertToUnmanaged (emitter);
			try {
				writer.Initialize ((IntPtr) pointer, filename, IntPtr.Zero, fFullBuild);
			} finally {
				ComInterfaceMarshaller<IMetaDataEmit>.Free (pointer);
			}
#else
			writer.Initialize (emitter, filename, null, fFullBuild);
#endif
		}

		public void SetUserEntryPoint (int methodToken)
		{
			writer.SetUserEntryPoint (methodToken);
		}

		public void OpenMethod (int methodToken)
		{
			writer.OpenMethod (methodToken);
		}

		public void OpenNamespace (string name)
		{
			writer.OpenNamespace (name);
		}

		public int OpenScope (int startOffset)
		{
			int result;
			writer.OpenScope (startOffset, out result);
			return result;
		}

		public void UsingNamespace (string fullName)
		{
			writer.UsingNamespace (fullName);
		}

		public void DefineCustomMetadata (string name, byte [] metadata)
		{
			var handle = GCHandle.Alloc (metadata, GCHandleType.Pinned);
			writer.SetSymAttribute (0, name, (uint) metadata.Length, handle.AddrOfPinnedObject ());
			handle.Free ();
		}
	}
}
