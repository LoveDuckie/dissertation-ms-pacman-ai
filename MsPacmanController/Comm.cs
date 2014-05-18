using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Pacman.Simulator;

namespace MsPacmanController
{
	public static class Comm
	{
		public const int COLOR_BLACK = 0x000000;
		public const int COLOR_PILL = 0xDAFFDA;
		public const int COLOR_PACMAN = 0xFFFF00;
		public const int COLOR_BLUE = 0x00FFFF;
		public const int COLOR_BROWN = 0xFFB655;
		public const int COLOR_PINK = 0xFFB6FF;
		public const int COLOR_RED = 0xFF0000;
		public const int COLOR_EDIBLE = 0x2424FF;
		public const int COLOR_EDIBLE_WHITE = 0xDADAFF;

		[DllImport("user32.dll")]
		public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

		public const int HWND_TOP = 0;
		public const int HWND_TOPMOST = -1;
		public const int HWND_NOTTOPMOST = -2;
		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

		public const byte VK_LEFT = 0x25;  //    LEFT ARROW key
		public const byte VK_LEFT_SCAN = 0x04B;
		public const byte VK_UP = 0x26; //     UP ARROW key
		public const byte VK_UP_SCAN = 0x048;
		public const byte VK_RIGHT = 0x27; //     RIGHT ARROW key
		public const byte VK_RIGHT_SCAN = 0x04D;
		public const byte VK_DOWN = 0x28; //      DOWN ARROW key 
		public const byte VK_DOWN_SCAN = 0x050;
		public const byte VK_F2 = 0x71; //      F2 key 
		public const byte VK_F2_SCAN = 0x3C;
		public const byte VK_F3 = 0x72; //      F3 key 
		public const byte VK_F3_SCAN = 0x3D;

		public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
		public const uint KEYEVENTF_KEYUP = 0x0002;
		public const uint KEYEVENTF_SCANCODE = 0x0008;

		public static void AddCredit() {
			keybd_event(VK_F2, VK_F2_SCAN, 0, 0);
			System.Threading.Thread.Sleep(100);
			keybd_event(VK_F2, VK_F2_SCAN, KEYEVENTF_KEYUP, 0);
			System.Threading.Thread.Sleep(100);
		}

		public static void StartGame() {
			keybd_event(VK_F3, VK_F3_SCAN, 0, 0);
			System.Threading.Thread.Sleep(100);
			keybd_event(VK_F3, VK_F3_SCAN, KEYEVENTF_KEYUP, 0);
			System.Threading.Thread.Sleep(100);
		}

		public static void MoveLeft() {
			keybd_event(VK_LEFT, VK_LEFT_SCAN, 0, 0);
			System.Threading.Thread.Sleep(30);
			keybd_event(VK_LEFT, VK_LEFT_SCAN, KEYEVENTF_KEYUP, 0);
		}

		public static void MoveRight() {
			keybd_event(VK_RIGHT, VK_RIGHT_SCAN, 0, 0);
			System.Threading.Thread.Sleep(30);
			keybd_event(VK_RIGHT, VK_RIGHT_SCAN, KEYEVENTF_KEYUP, 0);
		}

		public static void MoveUp() {
			keybd_event(VK_UP, VK_UP_SCAN, 0, 0);
			System.Threading.Thread.Sleep(30);
			keybd_event(VK_UP, VK_UP_SCAN, KEYEVENTF_KEYUP, 0);
		}

		public static void MoveDown() {
			keybd_event(VK_DOWN, VK_DOWN_SCAN, 0, 0);
			System.Threading.Thread.Sleep(30);
			keybd_event(VK_DOWN, VK_DOWN_SCAN, KEYEVENTF_KEYUP, 0);
		}

		private static Direction lastSentKey = Direction.None;
		public static void SendKey(Direction key) {
			if( lastSentKey != Direction.None ) {
				switch( lastSentKey ) {
					case Direction.Left: keybd_event(VK_LEFT, VK_LEFT_SCAN, KEYEVENTF_KEYUP, 0); break;
					case Direction.Right: keybd_event(VK_RIGHT, VK_RIGHT_SCAN, KEYEVENTF_KEYUP, 0); break;
					case Direction.Up: keybd_event(VK_UP, VK_UP_SCAN, KEYEVENTF_KEYUP, 0); break;
					case Direction.Down: keybd_event(VK_DOWN, VK_DOWN_SCAN, KEYEVENTF_KEYUP, 0); break;
				}
			} else {
				keybd_event(VK_LEFT, VK_LEFT_SCAN, KEYEVENTF_KEYUP, 0);
				keybd_event(VK_RIGHT, VK_RIGHT_SCAN, KEYEVENTF_KEYUP, 0);
				keybd_event(VK_UP, VK_UP_SCAN, KEYEVENTF_KEYUP, 0);
				keybd_event(VK_DOWN, VK_DOWN_SCAN, KEYEVENTF_KEYUP, 0);
			}
			if( key != Direction.None ) {
				switch( key ) {
					case Direction.Left: keybd_event(VK_LEFT, VK_LEFT_SCAN, 0, 0); break;
					case Direction.Right: keybd_event(VK_RIGHT, VK_RIGHT_SCAN, 0, 0); break;
					case Direction.Up: keybd_event(VK_UP, VK_UP_SCAN, 0, 0); break;
					case Direction.Down: keybd_event(VK_DOWN, VK_DOWN_SCAN, 0, 0); break;
				}
			}
			lastSentKey = key;
		}

		public static class Message
		{
			public const int WM_ACTIVATE = 0x0006;
			public const int WM_ACTIVATEAPP = 0x001C;
			public const int WM_AFXFIRST = 0x0360;
			public const int WM_AFXLAST = 0x037F;
			public const int WM_APP = 0x8000;
			public const int WM_ASKCBFORMATNAME = 0x030C;
			public const int WM_CANCELJOURNAL = 0x004B;
			public const int WM_CANCELMODE = 0x001F;
			public const int WM_CAPTURECHANGED = 0x0215;
			public const int WM_CHANGECBCHAIN = 0x030D;
			public const int WM_CHAR = 0x0102;
			public const int WM_CHARTOITEM = 0x002F;
			public const int WM_CHILDACTIVATE = 0x0022;
			public const int WM_CLEAR = 0x0303;
			public const int WM_CLOSE = 0x0010;
			public const int WM_COMMAND = 0x0111;
			public const int WM_COMPACTING = 0x0041;
			public const int WM_COMPAREITEM = 0x0039;
			public const int WM_CONTEXTMENU = 0x007B;
			public const int WM_COPY = 0x0301;
			public const int WM_COPYDATA = 0x004A;
			public const int WM_CREATE = 0x0001;
			public const int WM_CTLCOLORBTN = 0x0135;
			public const int WM_CTLCOLORDLG = 0x0136;
			public const int WM_CTLCOLOREDIT = 0x0133;
			public const int WM_CTLCOLORLISTBOX = 0x0134;
			public const int WM_CTLCOLORMSGBOX = 0x0132;
			public const int WM_CTLCOLORSCROLLBAR = 0x0137;
			public const int WM_CTLCOLORSTATIC = 0x0138;
			public const int WM_CUT = 0x0300;
			public const int WM_DEADCHAR = 0x0103;
			public const int WM_DELETEITEM = 0x002D;
			public const int WM_DESTROY = 0x0002;
			public const int WM_DESTROYCLIPBOARD = 0x0307;
			public const int WM_DEVICECHANGE = 0x0219;
			public const int WM_DEVMODECHANGE = 0x001B;
			public const int WM_DISPLAYCHANGE = 0x007E;
			public const int WM_DRAWCLIPBOARD = 0x0308;
			public const int WM_DRAWITEM = 0x002B;
			public const int WM_DROPFILES = 0x0233;
			public const int WM_ENABLE = 0x000A;
			public const int WM_ENDSESSION = 0x0016;
			public const int WM_ENTERIDLE = 0x0121;
			public const int WM_ENTERMENULOOP = 0x0211;
			public const int WM_ENTERSIZEMOVE = 0x0231;
			public const int WM_ERASEBKGND = 0x0014;
			public const int WM_EXITMENULOOP = 0x0212;
			public const int WM_EXITSIZEMOVE = 0x0232;
			public const int WM_FONTCHANGE = 0x001D;
			public const int WM_GETDLGCODE = 0x0087;
			public const int WM_GETFONT = 0x0031;
			public const int WM_GETHOTKEY = 0x0033;
			public const int WM_GETICON = 0x007F;
			public const int WM_GETMINMAXINFO = 0x0024;
			public const int WM_GETOBJECT = 0x003D;
			public const int WM_GETTEXT = 0x000D;
			public const int WM_GETTEXTLENGTH = 0x000E;
			public const int WM_HANDHELDFIRST = 0x0358;
			public const int WM_HANDHELDLAST = 0x035F;
			public const int WM_HELP = 0x0053;
			public const int WM_HOTKEY = 0x0312;
			public const int WM_HSCROLL = 0x0114;
			public const int WM_HSCROLLCLIPBOARD = 0x030E;
			public const int WM_ICONERASEBKGND = 0x0027;
			public const int WM_IME_CHAR = 0x0286;
			public const int WM_IME_COMPOSITION = 0x010F;
			public const int WM_IME_COMPOSITIONFULL = 0x0284;
			public const int WM_IME_CONTROL = 0x0283;
			public const int WM_IME_ENDCOMPOSITION = 0x010E;
			public const int WM_IME_KEYDOWN = 0x0290;
			public const int WM_IME_KEYLAST = 0x010F;
			public const int WM_IME_KEYUP = 0x0291;
			public const int WM_IME_NOTIFY = 0x0282;
			public const int WM_IME_REQUEST = 0x0288;
			public const int WM_IME_SELECT = 0x0285;
			public const int WM_IME_SETCONTEXT = 0x0281;
			public const int WM_IME_STARTCOMPOSITION = 0x010D;
			public const int WM_INITDIALOG = 0x0110;
			public const int WM_INITMENU = 0x0116;
			public const int WM_INITMENUPOPUP = 0x0117;
			public const int WM_INPUTLANGCHANGE = 0x0051;
			public const int WM_INPUTLANGCHANGEREQUEST = 0x0050;
			public const int WM_KEYDOWN = 0x0100;
			public const int WM_KEYFIRST = 0x0100;
			public const int WM_KEYLAST = 0x0108;
			public const int WM_KEYUP = 0x0101;
			public const int WM_KILLFOCUS = 0x0008;
			public const int WM_LBUTTONDBLCLK = 0x0203;
			public const int WM_LBUTTONDOWN = 0x0201;
			public const int WM_LBUTTONUP = 0x0202;
			public const int WM_MBUTTONDBLCLK = 0x0209;
			public const int WM_MBUTTONDOWN = 0x0207;
			public const int WM_MBUTTONUP = 0x0208;
			public const int WM_MDIACTIVATE = 0x0222;
			public const int WM_MDICASCADE = 0x0227;
			public const int WM_MDICREATE = 0x0220;
			public const int WM_MDIDESTROY = 0x0221;
			public const int WM_MDIGETACTIVE = 0x0229;
			public const int WM_MDIICONARRANGE = 0x0228;
			public const int WM_MDIMAXIMIZE = 0x0225;
			public const int WM_MDINEXT = 0x0224;
			public const int WM_MDIREFRESHMENU = 0x0234;
			public const int WM_MDIRESTORE = 0x0223;
			public const int WM_MDISETMENU = 0x0230;
			public const int WM_MDITILE = 0x0226;
			public const int WM_MEASUREITEM = 0x002C;
			public const int WM_MENUCHAR = 0x0120;
			public const int WM_MENUCOMMAND = 0x0126;
			public const int WM_MENUDRAG = 0x0123;
			public const int WM_MENUGETOBJECT = 0x0124;
			public const int WM_MENURBUTTONUP = 0x0122;
			public const int WM_MENUSELECT = 0x011F;
			public const int WM_MOUSEACTIVATE = 0x0021;
			public const int WM_MOUSEFIRST = 0x0200;
			public const int WM_MOUSEHOVER = 0x02A1;
			public const int WM_MOUSELAST = 0x020A;
			public const int WM_MOUSELEAVE = 0x02A3;
			public const int WM_MOUSEMOVE = 0x0200;
			public const int WM_MOUSEWHEEL = 0x020A;
			public const int WM_MOVE = 0x0003;
			public const int WM_MOVING = 0x0216;
			public const int WM_NCACTIVATE = 0x0086;
			public const int WM_NCCALCSIZE = 0x0083;
			public const int WM_NCCREATE = 0x0081;
			public const int WM_NCDESTROY = 0x0082;
			public const int WM_NCHITTEST = 0x0084;
			public const int WM_NCLBUTTONDBLCLK = 0x00A3;
			public const int WM_NCLBUTTONDOWN = 0x00A1;
			public const int WM_NCLBUTTONUP = 0x00A2;
			public const int WM_NCMBUTTONDBLCLK = 0x00A9;
			public const int WM_NCMBUTTONDOWN = 0x00A7;
			public const int WM_NCMBUTTONUP = 0x00A8;
			public const int WM_NCMOUSEMOVE = 0x00A0;
			public const int WM_NCPAINT = 0x0085;
			public const int WM_NCRBUTTONDBLCLK = 0x00A6;
			public const int WM_NCRBUTTONDOWN = 0x00A4;
			public const int WM_NCRBUTTONUP = 0x00A5;
			public const int WM_NEXTDLGCTL = 0x0028;
			public const int WM_NEXTMENU = 0x0213;
			public const int WM_NOTIFY = 0x004E;
			public const int WM_NOTIFYFORMAT = 0x0055;
			public const int WM_NULL = 0x0000;
			public const int WM_PAINT = 0x000F;
			public const int WM_PAINTCLIPBOARD = 0x0309;
			public const int WM_PAINTICON = 0x0026;
			public const int WM_PALETTECHANGED = 0x0311;
			public const int WM_PALETTEISCHANGING = 0x0310;
			public const int WM_PARENTNOTIFY = 0x0210;
			public const int WM_PASTE = 0x0302;
			public const int WM_PENWINFIRST = 0x0380;
			public const int WM_PENWINLAST = 0x038F;
			public const int WM_POWER = 0x0048;
			public const int WM_PRINT = 0x0317;
			public const int WM_PRINTCLIENT = 0x0318;
			public const int WM_QUERYDRAGICON = 0x0037;
			public const int WM_QUERYENDSESSION = 0x0011;
			public const int WM_QUERYNEWPALETTE = 0x030F;
			public const int WM_QUERYOPEN = 0x0013;
			public const int WM_QUEUESYNC = 0x0023;
			public const int WM_QUIT = 0x0012;
			public const int WM_RBUTTONDBLCLK = 0x0206;
			public const int WM_RBUTTONDOWN = 0x0204;
			public const int WM_RBUTTONUP = 0x0205;
			public const int WM_RENDERALLFORMATS = 0x0306;
			public const int WM_RENDERFORMAT = 0x0305;
			public const int WM_SETCURSOR = 0x0020;
			public const int WM_SETFOCUS = 0x0007;
			public const int WM_SETFONT = 0x0030;
			public const int WM_SETHOTKEY = 0x0032;
			public const int WM_SETICON = 0x0080;
			public const int WM_SETREDRAW = 0x000B;
			public const int WM_SETTEXT = 0x000C;
			public const int WM_SETTINGCHANGE = 0x001A;
			public const int WM_SHOWWINDOW = 0x0018;
			public const int WM_SIZE = 0x0005;
			public const int WM_SIZECLIPBOARD = 0x030B;
			public const int WM_SIZING = 0x0214;
			public const int WM_SPOOLERSTATUS = 0x002A;
			public const int WM_STYLECHANGED = 0x007D;
			public const int WM_STYLECHANGING = 0x007C;
			public const int WM_SYNCPAINT = 0x0088;
			public const int WM_SYSCHAR = 0x0106;
			public const int WM_SYSCOLORCHANGE = 0x0015;
			public const int WM_SYSCOMMAND = 0x0112;
			public const int WM_SYSDEADCHAR = 0x0107;
			public const int WM_SYSKEYDOWN = 0x0104;
			public const int WM_SYSKEYUP = 0x0105;
			public const int WM_TCARD = 0x0052;
			public const int WM_TIMECHANGE = 0x001E;
			public const int WM_TIMER = 0x0113;
			public const int WM_UNDO = 0x0304;
			public const int WM_UNINITMENUPOPUP = 0x0125;
			public const int WM_USER = 0x0400;
			public const int WM_USERCHANGED = 0x0054;
			public const int WM_VKEYTOITEM = 0x002E;
			public const int WM_VSCROLL = 0x0115;
			public const int WM_VSCROLLCLIPBOARD = 0x030A;
			public const int WM_WINDOWPOSCHANGED = 0x0047;
			public const int WM_WINDOWPOSCHANGING = 0x0046;
			public const int WM_WININICHANGE = 0x001A;
		}

		public static WINDOWINFO GetWindowInfoEasy(IntPtr handle) {
			WINDOWINFO info = new WINDOWINFO();
			info.cbSize = (uint)Marshal.SizeOf(info);
			GetWindowInfo(handle, ref info);
			return info;
		}
	}

	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public RECT(int left_, int top_, int right_, int bottom_) {
			Left = left_;
			Top = top_;
			Right = right_;
			Bottom = bottom_;
		}

		public int Height { get { return Bottom - Top; } }
		public int Width { get { return Right - Left; } }
		public Size Size { get { return new Size(Width, Height); } }

		public Point Location { get { return new Point(Left, Top); } }

		// Handy method for converting to a System.Drawing.Rectangle
		public Rectangle ToRectangle() { return Rectangle.FromLTRB(Left, Top, Right, Bottom); }

		public static RECT FromRectangle(Rectangle rectangle) {
			return new RECT(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
		}

		public override int GetHashCode() {
			return Left ^ ((Top << 13) | (Top >> 0x13))
			  ^ ((Width << 0x1a) | (Width >> 6))
			  ^ ((Height << 7) | (Height >> 0x19));
		}

		#region Operator overloads

		public static implicit operator Rectangle(RECT rect) {
			return rect.ToRectangle();
		}

		public static implicit operator RECT(Rectangle rect) {
			return FromRectangle(rect);
		}

		#endregion
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WINDOWINFO
	{
		public uint cbSize;
		public RECT rcWindow;
		public RECT rcClient;
		public uint dwStyle;
		public uint dwExStyle;
		public uint dwWindowStatus;
		public uint cxWindowBorders;
		public uint cyWindowBorders;
		public ushort atomWindowType;
		public ushort wCreatorVersion;
	}

	/// <summary>Enumeration of the different ways of showing a window using
	/// ShowWindow</summary>
	public enum WindowShowStyle : uint
	{
		/// <summary>Hides the window and activates another window.</summary>
		/// <remarks>See SW_HIDE</remarks>
		Hide = 0,
		/// <summary>Activates and displays a window. If the window is minimized
		/// or maximized, the system restores it to its original size and
		/// position. An application should specify this flag when displaying
		/// the window for the first time.</summary>
		/// <remarks>See SW_SHOWNORMAL</remarks>
		ShowNormal = 1,
		/// <summary>Activates the window and displays it as a minimized window.</summary>
		/// <remarks>See SW_SHOWMINIMIZED</remarks>
		ShowMinimized = 2,
		/// <summary>Activates the window and displays it as a maximized window.</summary>
		/// <remarks>See SW_SHOWMAXIMIZED</remarks>
		ShowMaximized = 3,
		/// <summary>Maximizes the specified window.</summary>
		/// <remarks>See SW_MAXIMIZE</remarks>
		Maximize = 3,
		/// <summary>Displays a window in its most recent size and position.
		/// This value is similar to "ShowNormal", except the window is not
		/// actived.</summary>
		/// <remarks>See SW_SHOWNOACTIVATE</remarks>
		ShowNormalNoActivate = 4,
		/// <summary>Activates the window and displays it in its current size
		/// and position.</summary>
		/// <remarks>See SW_SHOW</remarks>
		Show = 5,
		/// <summary>Minimizes the specified window and activates the next
		/// top-level window in the Z order.</summary>
		/// <remarks>See SW_MINIMIZE</remarks>
		Minimize = 6,
		/// <summary>Displays the window as a minimized window. This value is
		/// similar to "ShowMinimized", except the window is not activated.</summary>
		/// <remarks>See SW_SHOWMINNOACTIVE</remarks>
		ShowMinNoActivate = 7,
		/// <summary>Displays the window in its current size and position. This
		/// value is similar to "Show", except the window is not activated.</summary>
		/// <remarks>See SW_SHOWNA</remarks>
		ShowNoActivate = 8,
		/// <summary>Activates and displays the window. If the window is
		/// minimized or maximized, the system restores it to its original size
		/// and position. An application should specify this flag when restoring
		/// a minimized window.</summary>
		/// <remarks>See SW_RESTORE</remarks>
		Restore = 9,
		/// <summary>Sets the show state based on the SW_ value specified in the
		/// STARTUPINFO structure passed to the CreateProcess function by the
		/// program that started the application.</summary>
		/// <remarks>See SW_SHOWDEFAULT</remarks>
		ShowDefault = 10,
		/// <summary>Windows 2000/XP: Minimizes a window, even if the thread
		/// that owns the window is hung. This flag should only be used when
		/// minimizing windows from a different thread.</summary>
		/// <remarks>See SW_FORCEMINIMIZE</remarks>
		ForceMinimized = 11
	}
}
