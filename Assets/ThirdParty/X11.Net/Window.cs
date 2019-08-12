using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace X11
{
    public enum PixmapFormat : int { XYBitmap = 0, XYPixmap = 1, ZPixmap = 2 }; 

    public enum Direction: int
    {
        RaiseLowest=0,
        LowerHighest=1,
    }

    public enum ChangeMode: int
    {
        SetModeInsert = 0,
        SetModeDelete = 1,
    }

    public enum PropMode {
        Replace = 0,
        Prepend = 1,
        Append = 2
    }

    public enum RevertFocus: int
    {
        RevertToNone =0,
        RevertToPointerRoot =1,
        RevertToParent=2,
    }

    public enum Atom : ulong {
        XA_PRIMARY = 1,
        XA_SECONDARY = 2,
        XA_ARC = 3,
        XA_ATOM = 4,
        XA_BITMAP = 5,
        XA_CARDINAL = 6,
        XA_COLORMAP = 7,
        XA_CURSOR = 8,
        XA_CUT_BUFFER0 = 9,
        XA_CUT_BUFFER1 = 10,
        XA_CUT_BUFFER2 = 11,
        XA_CUT_BUFFER3 = 12,
        XA_CUT_BUFFER4 = 13,
        XA_CUT_BUFFER5 = 14,
        XA_CUT_BUFFER6 = 15,
        XA_CUT_BUFFER7 = 16,
        XA_DRAWABLE = 17,
        XA_FONT = 18,
        XA_INTEGER = 19,
        XA_PIXMAP = 20,
        XA_POINT = 21,
        XA_RECTANGLE = 22,
        XA_RESOURCE_MANAGER = 23,
        XA_RGB_COLOR_MAP = 24,
        XA_RGB_BEST_MAP = 25,
        XA_RGB_BLUE_MAP = 26,
        XA_RGB_DEFAULT_MAP = 27,
        XA_RGB_GRAY_MAP = 28,
        XA_RGB_GREEN_MAP = 29,
        XA_RGB_RED_MAP = 30,
        XA_STRING = 31,
        XA_VISUALID = 32,
        XA_WINDOW = 33,
        XA_WM_COMMAND = 34,
        XA_WM_HINTS = 35,
        XA_WM_CLIENT_MACHINE = 36,
        XA_WM_ICON_NAME = 37,
        XA_WM_ICON_SIZE = 38,
        XA_WM_NAME = 39,
        XA_WM_NORMAL_HINTS = 40,
        XA_WM_SIZE_HINTS = 41,
        XA_WM_ZOOM_HINTS = 42,
        XA_MIN_SPACE = 43,
        XA_NORM_SPACE = 44,
        XA_MAX_SPACE = 45,
        XA_END_SPACE = 46,
        XA_SUPERSCRIPT_X = 47,
        XA_SUPERSCRIPT_Y = 48,
        XA_SUBSCRIPT_X = 49,
        XA_SUBSCRIPT_Y = 50,
        XA_UNDERLINE_POSITION = 51,
        XA_UNDERLINE_THICKNESS = 52,
        XA_STRIKEOUT_ASCENT = 53,
        XA_STRIKEOUT_DESCENT = 54,
        XA_ITALIC_ANGLE = 55,
        XA_X_HEIGHT = 56,
        XA_QUAD_WIDTH = 57,
        XA_WEIGHT = 58,
        XA_POINT_SIZE = 59,
        XA_RESOLUTION = 60,
        XA_COPYRIGHT = 61,
        XA_NOTICE = 62,
        XA_FONT_NAME = 63,
        XA_FAMILY_NAME = 64,
        XA_FULL_NAME = 65,
        XA_CAP_HEIGHT = 66,
        XA_WM_CLASS = 67,
        XA_WM_TRANSIENT_FOR = 68
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XImage
    {
        public int width, height;
        public int xoffset;
        public int format;
        public IntPtr data;
        public int byte_order;
        public int bitmap_unit;
        public int bitmap_bit_order;
        public int bitmap_pad;
        public int depth;
        public int bytes_per_line;
        public int bits_per_pixel;
        public ulong red_mask;
        public ulong green_mask;
        public ulong blue_mask;
        public IntPtr obdata;
        private struct funcs
        {
            IntPtr create_image;
            IntPtr destroy_image;
            IntPtr get_pixel;
            IntPtr put_pixel;
            IntPtr sub_image;
            IntPtr add_pixel;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XWindowAttributes
    {
        public int x, y;
        public uint width, height;
        public int border_width;
        public int depth;
        public IntPtr visual;
        public Window root;
        public int @class;
        public int bit_gravity;
        public int win_gravity;
        public int backing_store;
        public ulong backing_planes;
        public ulong backing_pixel;
        public bool save_under;
        public Colormap colormap;
        public bool map_installed;
        public int map_state;
        public long all_event_masks;
        public long your_event_masks;
        public long do_not_propagate_mask;
        public bool override_redirect;
        public IntPtr screen;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XSetWindowAttributes
    {
        public Pixmap background_pixmap;   /* background or None or ParentRelative */
        public ulong background_pixel;     /* background pixel */
        public Pixmap border_pixmap;       /* border of the window */
        public ulong border_pixel; /* border pixel value */
        public int bit_gravity;            /* one of bit gravity values */
        public int win_gravity;            /* one of the window gravity values */
        public int backing_store;          /* NotUseful, WhenMapped, Always */
        public ulong backing_planes;/* planes to be preseved if possible */
        public ulong backing_pixel;/* value to use in restoring planes */
        public bool save_under;            /* should bits under be saved? (popups) */
        public EventMask event_mask;            /* set of events that should be saved */
        public EventMask do_not_propagate_mask; /* set of events that should not propagate */
        public bool override_redirect;     /* boolean value for override-redirect */
        public Colormap colormap;          /* color map to be associated with window */
        public Cursor cursor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XWindowChanges
    {
        public int x, y;
        public int width, height;
        public int border_width;
        public Window sibling;
        public int stack_mode;
    }

    public partial class Xlib
    {
        [DllImport("libX11")]
        public static extern int XFree(IntPtr data);

        
        [DllImport("libX11")]
        public static extern ulong XInternAtom(IntPtr display, string atomName, bool onlyIfExists);


        /// <summary>
        /// int XGetWindowProperty(display, w, property, long_offset, long_length, delete, req_type, 
        ///        actual_type_return, actual_format_return, nitems_return, bytes_after_return, 
        ///        prop_return)
        ///        Display *display;
        ///        Window w;
        ///        Atom property;
        ///        long long_offset, long_length;
        ///        Bool delete;
        ///        Atom req_type; 
        ///        Atom *actual_type_return;
        ///        int *actual_format_return;
        ///        unsigned long *nitems_return;
        ///        unsigned long *bytes_after_return;
        ///        unsigned char **prop_return;
        /// </summary>
        /// <param name="display"></param>
        /// <param name="window"></param>
        /// <param name="atom"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="delete"></param>
        /// <param name="reqType"></param>
        /// <param name="actualTypeReturn"></param>
        /// <param name="actualFormatReturn"></param>
        /// <param name="nItemsReturn"></param>
        /// <param name="bytesAfterReturn"></param>
        /// <param name="propReturn"></param>
        /// <returns></returns>
        [DllImport("libX11")]
        public static extern int XGetWindowProperty(IntPtr display, Window window, ulong atom, long offset,
            long length,
            bool delete, Atom reqType, out Atom actualTypeReturn, out int actualFormatReturn, out ulong nItemsReturn,
            out ulong bytesAfterReturn, out IntPtr propReturn);
        
        [DllImport("libX11")]
        public static extern void XChangeProperty(IntPtr display, Window w, ulong property, ulong type, int format,
            PropMode mode, IntPtr data, int nelements);
        
        /// <summary>
        /// The XGetWindowAttributes function returns the current attributes for the specified window to an XWindowAt‐
        /// tributes structure.It returns a nonzero status on success; otherwise, it returns a zero status.
        /// </summary>
        /// <param name="display"></param>
        /// <param name="window"></param>
        /// <param name="attributes"></param>
        [DllImport("libX11")]
        public static extern Status XGetWindowAttributes(IntPtr display, Window window, out XWindowAttributes attributes);

        [DllImport("libX11")]
        public static extern Status XDestroyWindow(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern Status XReparentWindow(IntPtr display, Window window, Window parent, int x, int y);

        [DllImport("libX11")]
        public static extern Status XAddToSaveSet(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern Status XRemoveFromSaveSet(IntPtr dispay, Window window);

        [DllImport("libX11")]
        public static extern Status XChangeSaveSet(IntPtr display, Window window, ChangeMode change_mode);

        /// <summary>
        /// Returns a pointer which can be marshalled to an XImage object for field access in managed code.
        /// This should be freed after use with the XDestroyImage function (from Xutil).
        /// </summary>
        /// <param name="display">Pointer to an open X display</param>
        /// <param name="drawable">Window ID to capture</param>
        /// <param name="x">X-offset for capture region</param>
        /// <param name="y">Y-offset for capture region</param>
        /// <param name="width">Width of capture region</param>
        /// <param name="height">Height of capture region</param>
        /// <param name="plane_mask"></param>
        /// <param name="format">One of XYBitmap, XYPixmap, ZPixmap</param>
        /// <returns></returns>
        [DllImport("libX11")]
        public static extern ref XImage XGetImage(IntPtr display, Window drawable, int x, int y,
            uint width, uint height, ulong plane_mask, PixmapFormat format);


        [DllImport("libX11")]
        public static extern Status XSelectInput(IntPtr display, Window window, EventMask event_mask);

        [DllImport("libX11")]
        private static extern int XQueryTree(IntPtr display, Window window, ref Window WinRootReturn,
            ref Window WinParentReturn, ref IntPtr ChildrenReturn, ref uint nChildren);
        public static int XQueryTree(IntPtr display, Window window, ref Window WinRootReturn,
            ref Window WinParentReturn, out List<Window> ChildrenReturn)
        {
            ChildrenReturn = new List<Window>();
            IntPtr pChildren = new IntPtr();
            uint nChildren = 0;

            var r = Xlib.XQueryTree(display, window, ref WinRootReturn, ref WinParentReturn,
                ref pChildren, ref nChildren);

            for (int i = 0; i < nChildren; i++)
            {
                var ptr = new IntPtr(pChildren.ToInt64() + i * sizeof(Window));
                ChildrenReturn.Add( (Window)Marshal.ReadInt64(ptr) );
            }

            return r;
        }

        [DllImport("libX11")]
        public static extern Window XCreateSimpleWindow(IntPtr display, Window parent, int x, int y,
            uint width, uint height, uint border_width, ulong border_colour, ulong background_colour);

        [DllImport("libX11")]
        public static extern Window XCreateWindow(IntPtr display, Window parent, int x, int y, uint width,
            uint height, uint border_width, int depth, uint @class, IntPtr visual, ulong valuemask,
              ref XSetWindowAttributes attributes);
        
        [DllImport("libX11")]
        public static extern int XMapWindow(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern int XUnmapWindow(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern int XConfigureWindow(IntPtr display, Window window, ulong value_mask, ref XWindowChanges changes);

        [DllImport("libX11")]
        public static extern int XSetWindowBackground(IntPtr display, Window window, ulong pixel);

        [DllImport("libX11")]
        public static extern Status XSetWindowBorder(IntPtr display, Window window, ulong border_pixel);

        [DllImport("libX11")]
        public static extern int XClearWindow(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern int XMoveWindow(IntPtr display, Window window, int x, int y);

        [DllImport("libX11")]
        public static extern Status XResizeWindow(IntPtr display, Window window, uint width, uint height);

        [DllImport("libX11")]
        public static extern Status XMoveResizeWindow(IntPtr display, Window window, int x, int y, uint width, uint height);

        [DllImport("libX11")]
        public static extern Status XSetWindowBorderWidth(IntPtr display, Window window, uint width);

        [DllImport("libX11")]
        public static extern Status XSetInputFocus(IntPtr display, Window focus, RevertFocus revert_to, long time);

        [DllImport("libX11")]
        public static extern Status XGetInputFocus(IntPtr display, ref Window focus_return, ref RevertFocus revert_to_return);

        [DllImport("libX11")]
        public static extern Status XRaiseWindow(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern Status XLowerWindow(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern Status XCirculateSubwindows(IntPtr display, Window window, Direction direction);

        [DllImport("libX11")]
        public static extern Status XCirculateSubwindowsUp(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern Status XCirculateSubwindowsDown(IntPtr display, Window window);

        [DllImport("libX11")]
        public static extern Status XRestackWindows(IntPtr display, IntPtr windows, int nwindows);

        [DllImport("libX11")]
        public static extern Status XFetchName(IntPtr display, Window window, ref String name_return);

        [DllImport("libX11")]
        public static extern Status XStoreName(IntPtr display, Window window, string window_name);

        [DllImport("libX11")]
        public static extern Status XDrawString(IntPtr display, Window drawable, IntPtr gc, int x, int y, string str, int length);
    }

}
