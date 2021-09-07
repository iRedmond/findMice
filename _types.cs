using System;
using System.Runtime.InteropServices;

unsafe struct HANDLE
{
    void* handle;
    public IntPtr Handle => (IntPtr)handle;
    public HANDLE(IntPtr handle)
    {
        this.handle = (void*)handle;
    }
    public HANDLE(HandleRef handleRef) : this(handleRef.Handle) { }
    public static implicit operator HANDLE(HandleRef handle) => new HANDLE(handle);
}
struct HHOOK
{
    public HANDLE Handle { get; }
    public IntPtr IntPtr => Handle.Handle;
    public HHOOK(HANDLE handle)
    {
        Handle = handle;
    }

    public static implicit operator HHOOK(HandleRef handle) => new HHOOK(handle);
}
struct WPARAM
{
    public UIntPtr Data;
}

struct LRESULT
{
    public IntPtr Result;
}
