#if NET10_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Microsoft.Cci.Pdb;
using NUnit.Framework;

[assembly: DisableRuntimeMarshalling]

namespace Mono.Cecil.Tests {

	[TestFixture]
	public unsafe class ComInteropTests {

		[Test]
		public void MetadataCallbacks ()
		{
			using (var module = ModuleDefinition.CreateModule ("Symbols", ModuleKind.Dll)) {
				var type = new TypeDefinition ("Symbols", "Program", TypeAttributes.Public);
				type.MetadataToken = new MetadataToken (TokenType.TypeDef, 2);
				module.Types.Add (type);
				var method = new MethodDefinition ("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
				method.MetadataToken = new MetadataToken (TokenType.Method, 1);
				type.Methods.Add (method);

				var emitter = ComInterfaceMarshaller<IMetaDataEmit>.ConvertToUnmanaged (new ModuleMetadata (module));
				IntPtr import = IntPtr.Zero;
				try {
					var iid = typeof (IMetaDataImport).GUID;
					Assert.AreEqual (0, Marshal.QueryInterface ((IntPtr) emitter, in iid, out import));
					var vtable = *(void***) import;
					var get_type = (delegate* unmanaged[Stdcall]<IntPtr, uint, char*, uint, uint*, uint*, uint*, uint>) vtable [12];
					uint attributes;
					uint extends;
					Assert.AreEqual (0, get_type (import, type.MetadataToken.ToUInt32 (), null, 0, null, &attributes, &extends));
					Assert.AreEqual ((uint) type.Attributes, attributes);
					Assert.AreEqual (0, extends);
					Assert.AreEqual (0x80004005u, get_type (import, 0x0200ffff, null, 0, null, null, null));

					var get_method = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint*, char*, uint, uint*, uint*, IntPtr, IntPtr, uint*, uint*, uint>) vtable [30];
					uint declaring_type;
					Assert.AreEqual (0, get_method (import, method.MetadataToken.ToUInt32 (), &declaring_type, null, 0, null, &attributes, IntPtr.Zero, IntPtr.Zero, null, null));
					Assert.AreEqual (type.MetadataToken.ToUInt32 (), declaring_type);
					Assert.AreEqual ((uint) method.Attributes, attributes);
				} finally {
					if (import != IntPtr.Zero)
						Marshal.Release (import);
					ComInterfaceMarshaller<IMetaDataEmit>.Free (emitter);
				}
			}
		}

		[Test]
		public void WriterBuffersAndConstants ()
		{
			// A native vtable exercises the generated RCW without requiring the Windows PDB writer.
			var vtable = stackalloc void* [30];
			vtable [0] = (delegate* unmanaged[Stdcall]<NativeWriter*, Guid*, void**, int>) &QueryInterface;
			vtable [1] = (delegate* unmanaged[Stdcall]<NativeWriter*, uint>) &AddRef;
			vtable [2] = (delegate* unmanaged[Stdcall]<NativeWriter*, uint>) &Release;
			vtable [3] = (delegate* unmanaged[Stdcall]<NativeWriter*, char*, Guid*, Guid*, Guid*, NativeWriter**, int>) &DefineDocument;
			vtable [21] = (delegate* unmanaged[Stdcall]<NativeWriter*, ImageDebugDirectory*, int, int*, byte*, int>) &GetDebugInfo;
			vtable [29] = (delegate* unmanaged[Stdcall]<NativeWriter*, char*, NativeVariant, int, int>) &DefineConstant;
			var document_vtable = stackalloc void* [5];
			document_vtable [0] = vtable [0];
			document_vtable [1] = vtable [1];
			document_vtable [2] = vtable [2];
			document_vtable [3] = (delegate* unmanaged[Stdcall]<NativeWriter*, uint, byte*, int>) &SetSource;
			document_vtable [4] = (delegate* unmanaged[Stdcall]<NativeWriter*, Guid, uint, byte*, int>) &SetCheckSum;
			var document = new NativeWriter { VTable = document_vtable, References = 1 };
			var native = new NativeWriter { VTable = vtable, References = 1, Document = &document };
			var wrappers = new StrategyBasedComWrappers ();
			var writer = (ISymUnmanagedWriter2) wrappers.GetOrCreateObjectForComInstance ((IntPtr) (&native), CreateObjectFlags.UniqueInstance);
			try {
				ImageDebugDirectory directory;
				int size;
				writer.GetDebugInfo (out directory, 0, out size, null);
				Assert.AreEqual (3, size);
				var buffer = new byte [size];
				writer.GetDebugInfo (out directory, buffer.Length, out size, buffer);
				Assert.AreEqual (new byte [] { 1, 2, 3 }, buffer);
				Assert.AreEqual (3, directory.SizeOfData);
				Assert.Throws<COMException> (() => writer.GetDebugInfo (out directory, -1, out size, null));

				var language = Guid.NewGuid ();
				var vendor = Guid.NewGuid ();
				var document_type = Guid.NewGuid ();
				ISymUnmanagedDocumentWriter document_writer;
				writer.DefineDocument ("source.cs", ref language, ref vendor, ref document_type, out document_writer);
				try {
					document_writer.SetSource (3, new byte [] { 5, 6, 7 });
					Assert.AreEqual (3, document.Token);
					Assert.AreEqual (7, document.Value);
					document_writer.SetCheckSum (language, 2, new byte [] { 8, 9 });
					Assert.AreEqual (language, document.Algorithm);
					Assert.AreEqual (2, document.Token);
					Assert.AreEqual (9, document.Value);
				} finally {
					((ComObject) (object) document_writer).FinalRelease ();
				}
				Assert.AreEqual (1, document.References);

				using (var variant = ComVariantMarshaller.ConvertToUnmanaged (42))
					writer.DefineConstant2 ("answer", variant, 0x11000001);
				Assert.AreEqual (VarEnum.VT_I4, (VarEnum) native.VariantType);
				Assert.AreEqual (42, native.Value);
				Assert.AreEqual (0x11000001, native.Token);
				Assert.AreEqual ('a', native.Name);

				using (var variant = ComVariantMarshaller.ConvertToUnmanaged ("symbols"))
					writer.DefineConstant2 ("text", variant, 0x11000002);
				Assert.AreEqual (VarEnum.VT_BSTR, (VarEnum) native.VariantType);
				Assert.AreEqual ('s', native.Value);
			} finally {
				((ComObject) (object) writer).FinalRelease ();
			}
			Assert.AreEqual (1, native.References);
		}

		struct NativeWriter {
			public void** VTable;
			public uint References;
			public ushort VariantType;
			public int Value;
			public int Token;
			public char Name;
			public NativeWriter* Document;
			public Guid Algorithm;
		}

		[StructLayout (LayoutKind.Explicit)]
		struct NativeVariant {
			[FieldOffset (0)] public ushort Type;
			[FieldOffset (8)] public int Integer;
			[FieldOffset (8)] public IntPtr Pointer;
			// VARIANT's record member determines its architecture-dependent size.
			[FieldOffset (8)] public NativeRecord Record;
		}

		readonly struct NativeRecord {
			readonly IntPtr data;
			readonly IntPtr info;

			public NativeRecord (IntPtr data, IntPtr info)
			{
				this.data = data;
				this.info = info;
			}
		}

		[UnmanagedCallersOnly (CallConvs = new [] { typeof (CallConvStdcall) })]
		static int QueryInterface (NativeWriter* writer, Guid* iid, void** result)
		{
			if (*iid != typeof (ISymUnmanagedWriter2).GUID && *iid != typeof (ISymUnmanagedDocumentWriter).GUID && *iid != new Guid ("00000000-0000-0000-C000-000000000046")) {
				*result = null;
				return unchecked ((int) 0x80004002);
			}
			*result = writer;
			writer->References++;
			return 0;
		}

		[UnmanagedCallersOnly (CallConvs = new [] { typeof (CallConvStdcall) })]
		static uint AddRef (NativeWriter* writer)
		{
			return ++writer->References;
		}

		[UnmanagedCallersOnly (CallConvs = new [] { typeof (CallConvStdcall) })]
		static uint Release (NativeWriter* writer)
		{
			return --writer->References;
		}

		[UnmanagedCallersOnly (CallConvs = new [] { typeof (CallConvStdcall) })]
		static int DefineDocument (NativeWriter* writer, char* name, Guid* language, Guid* vendor, Guid* type, NativeWriter** document)
		{
			*document = writer->Document;
			(*document)->References++;
			return 0;
		}

		[UnmanagedCallersOnly (CallConvs = new [] { typeof (CallConvStdcall) })]
		static int SetSource (NativeWriter* writer, uint size, byte* source)
		{
			writer->Token = (int) size;
			writer->Value = source [size - 1];
			return 0;
		}

		[UnmanagedCallersOnly (CallConvs = new [] { typeof (CallConvStdcall) })]
		static int SetCheckSum (NativeWriter* writer, Guid algorithm, uint size, byte* source)
		{
			writer->Algorithm = algorithm;
			writer->Token = (int) size;
			writer->Value = source [size - 1];
			return 0;
		}

		[UnmanagedCallersOnly (CallConvs = new [] { typeof (CallConvStdcall) })]
		static int GetDebugInfo (NativeWriter* writer, ImageDebugDirectory* directory, int capacity, int* size, byte* buffer)
		{
			if (capacity < 0)
				return unchecked ((int) 0x80004005);
			*directory = new ImageDebugDirectory { SizeOfData = 3 };
			*size = 3;
			if (capacity >= 3) {
				buffer [0] = 1;
				buffer [1] = 2;
				buffer [2] = 3;
			}
			return 0;
		}

		[UnmanagedCallersOnly (CallConvs = new [] { typeof (CallConvStdcall) })]
		static int DefineConstant (NativeWriter* writer, char* name, NativeVariant value, int token)
		{
			writer->VariantType = value.Type;
			writer->Value = value.Type == (ushort) VarEnum.VT_BSTR ? *(char*) value.Pointer : value.Integer;
			writer->Token = token;
			writer->Name = *name;
			return 0;
		}
	}
}
#endif
