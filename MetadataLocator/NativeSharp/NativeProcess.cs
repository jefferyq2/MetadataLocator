using System;
using static MetadataLocator.NativeSharp.NativeMethods;

namespace MetadataLocator.NativeSharp;

static unsafe class NativeProcess {
	static readonly void* handle = GetCurrentProcess();

	public static bool TryToAddress(Pointer pointer, out void* address) {
		address = default;
		if (pointer is null)
			return false;

		return ToAddressInternal(handle, pointer, out address);
	}

	public static bool TryReadUInt32(void* address, out uint value) {
		return ReadUInt32Internal(handle, address, out value);
	}

	public static bool TryReadIntPtr(void* address, out IntPtr value) {
		return ReadIntPtrInternal(handle, address, out value);
	}

	static bool ToAddressInternal(void* processHandle, Pointer pointer, out void* address) {
		return IntPtr.Size == 8 ? ToAddressPrivate64(processHandle, pointer, out address) : ToAddressPrivate32(processHandle, pointer, out address);
	}

	static bool ToAddressPrivate32(void* processHandle, Pointer pointer, out void* address) {
		address = default;
		uint newAddress = (uint)pointer.BaseAddress;
		var offsets = pointer.Offsets;
		if (offsets.Count > 0) {
			for (int i = 0; i < offsets.Count - 1; i++) {
				newAddress += offsets[i];
				if (!ReadUInt32Internal(processHandle, (void*)newAddress, out newAddress))
					return false;
			}
			newAddress += offsets[offsets.Count - 1];
		}
		address = (void*)newAddress;
		return true;
	}

	static bool ToAddressPrivate64(void* processHandle, Pointer pointer, out void* address) {
		address = default;
		ulong newAddress = (ulong)pointer.BaseAddress;
		var offsets = pointer.Offsets;
		if (offsets.Count > 0) {
			for (int i = 0; i < offsets.Count - 1; i++) {
				newAddress += offsets[i];
				if (!ReadUInt64Internal(processHandle, (void*)newAddress, out newAddress))
					return false;
			}
			newAddress += offsets[offsets.Count - 1];
		}
		address = (void*)newAddress;
		return true;
	}

	static bool ReadUInt32Internal(void* processHandle, void* address, out uint value) {
		fixed (void* p = &value)
			return ReadInternal(processHandle, address, p, 4);
	}

	static bool ReadUInt64Internal(void* processHandle, void* address, out ulong value) {
		fixed (void* p = &value)
			return ReadInternal(processHandle, address, p, 8);
	}

	static bool ReadIntPtrInternal(void* processHandle, void* address, out IntPtr value) {
		fixed (void* p = &value)
			return ReadInternal(processHandle, address, p, (uint)IntPtr.Size);
	}

	static bool ReadInternal(void* processHandle, void* address, void* value, uint length) {
		return ReadProcessMemory(processHandle, address, value, length, null);
	}

	public static void* GetModule(string moduleName) {
		if (string.IsNullOrEmpty(moduleName))
			throw new ArgumentNullException(nameof(moduleName));

		return GetModuleHandle(moduleName);
	}
}
