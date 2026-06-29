using System.Runtime.InteropServices;

namespace Float.UI.Platform;

internal static partial class MacDockHelper
{
    [LibraryImport("/usr/lib/libobjc.A.dylib", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint objc_getClass(string name);

    [LibraryImport("/usr/lib/libobjc.A.dylib", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint sel_registerName(string name);

    [LibraryImport("/usr/lib/libobjc.A.dylib")]
    private static partial nint objc_msgSend(nint receiver, nint selector);

    [LibraryImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static partial void objc_msgSend_void_int(nint receiver, nint selector, int arg);

    public static void SetShowInDock(bool show)
    {
        var cls = objc_getClass("NSApplication");
        var app = objc_msgSend(cls, sel_registerName("sharedApplication"));
        // 0 = NSApplicationActivationPolicyRegular (Dock), 1 = Accessory (menu bar only)
        objc_msgSend_void_int(app, sel_registerName("setActivationPolicy:"), show ? 0 : 1);
    }
}
