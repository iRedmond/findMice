using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using sysDraw = System.Drawing;


namespace findMice
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        static extern bool GetCursorPos(out sysDraw.Point pt);
        LinkedList<(double a, uint d)> past = new LinkedList<(double, uint)>();
        void push(double a, uint d)
        {
            past.AddLast((a, d));

            foreach (var item in past)
            {
                a += item.a;
                d += item.d;
            }
            if (d < 100) return;

            if (a < 30)
                Hide();
            else
            {
                a = Math.Pow(a, 1.05);

                var matrix = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;

                if (GetCursorPos(out var mp))
                {
                    shape.Width = shape.MinWidth * a;
                    shape.Height = shape.MinHeight * a;

                    var st = shape.StrokeThickness;
                    var pt = new Point(.2929 * st, .7071 * st);

                    var offset = shape.TranslatePoint(pt, this);

                    Left = mp.X * matrix.M11 - offset.X;
                    Top = mp.Y * matrix.M22 - offset.Y;
                }
                Show();
            }

            Trace.WriteLine(past.Count);

            past.RemoveFirst();
        }

        const int WH_MOUSE_LL = 14;
        enum WH : int
        {
            MOUSE_LL = WH_MOUSE_LL,
        }
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern HHOOK SetWindowsHookEx(WH type, Delegate callback, HANDLE hMod, uint threadId);

        enum WM : uint
        {
            MOUSEMOVE = 0x200,
        }
        enum LLMHF : uint
        {
            INJECTED = 1,
            LOWER_IL_INJECTED = 2,
        }
        unsafe struct MSLLHOOKSTRUCT
        {
#pragma warning disable CS0649
            public sysDraw.Point pt;
            public uint mouseData;
            public LLMHF flags;
            public uint ts;
            public void* ex;
#pragma warning restore CS0649
        }
        delegate LRESULT LowLevelMouseEvent(int code, WPARAM wparam, in MSLLHOOKSTRUCT info);
        static HHOOK SetLLMouseHook(LowLevelMouseEvent proc) => SetWindowsHookEx(WH.MOUSE_LL, proc, default, default);

        [DllImport("user32.dll", EntryPoint = "CallNextHookEx", ExactSpelling = true)]
        static extern LRESULT CallNextLLMH(HANDLE ignore, int code, WPARAM wparam, in MSLLHOOKSTRUCT info);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        static extern bool UnhookWindowsHookEx(HHOOK hHook);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        static extern uint GetTickCount();

        struct Echo
        {
            public sysDraw.Point pt;
            public uint ts;
            public Point v;
        }
        Echo last;
        LRESULT proc(int code, WPARAM wparam, in MSLLHOOKSTRUCT info)
        {
            if ((WM)wparam.Data == WM.MOUSEMOVE)
            {
                var td = info.ts - last.ts;
                if (td != 0)
                {
                    last.ts = info.ts;

                    var pt = info.pt;
                    double x = pt.X - last.pt.X;
                    double y = pt.Y - last.pt.Y;
                    last.pt = pt;

                    var invt = 1d / td;
                    var v = new Point(x * invt, y * invt);
                    x = v.X - last.v.X;
                    y = v.Y - last.v.Y;
                    last.v = v;

                    var a = Math.Sqrt(x * x + y * y);
                    push(a, td);
                }
            }
            return CallNextLLMH(default, code, wparam, in info);
        }
        LowLevelMouseEvent callback;
        HandleRef hook;
        private DispatcherTimer dt;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            hook = new HandleRef(callback = proc, SetLLMouseHook(callback).IntPtr);

            dt = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.0625) };
            dt.Tick += delegate { push(0, GetTickCount()); };
            dt.IsEnabled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            UnhookWindowsHookEx(hook);
            hook = default;

            base.OnClosed(e);
        }
    }
}
