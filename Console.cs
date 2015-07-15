using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hunters
{
    class Console
    {
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutput(
            SafeFileHandle hConsoleOutput,
            CharInfo[] lpBuffer,
            Coord dwBufferSize,
            Coord dwBufferCoord,
            ref SmallRect lpWriteRegion);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        [StructLayout(LayoutKind.Sequential)]
        private struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)]
            public char UnicodeChar;
            [FieldOffset(0)]
            public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)]
            public CharUnion Char;
            [FieldOffset(2)]
            public short Attributes;

            public void Set(char c, short a)
            {
                Char.UnicodeChar = c;
                Attributes = a;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        private static SafeFileHandle handle;
        private static int w, h, frames;
        private static SmallRect rect;
        private static Coord buf_size;
        private static Coord buf_pos;
        private static long timer;
        private static float freq;

        public static CharInfo[] buf;

        public static int Width
        {
            get
            {
                return w;
            }
        }

        public static int Height
        {
            get
            {
                return h;
            }
        }

        public static Pos Size
        {
            get
            {
                return new Pos(w, h);
            }
        }

        public static void Init(string title, int _w, int _h, int _frames)
        {
            w = _w;
            h = _h;
            frames = _frames;
            handle = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            if (handle.IsInvalid)
                throw new Exception("Failed to init console.");
            buf = new CharInfo[w * h];
            rect = new SmallRect() { Left = 0, Top = 0, Right = (short)w, Bottom = (short)h };
            buf_size = new Coord((short)w, (short)h);
            buf_pos = new Coord(0, 0);

            long freql;
            if (!QueryPerformanceFrequency(out freql))
                throw new Exception("Failed to init timer.");
            freq = (float)freql;
            QueryPerformanceCounter(out timer);

            System.Console.CursorVisible = false;
            System.Console.SetWindowSize(w, h);
            System.Console.SetBufferSize(w, h);
            System.Console.Title = title;
        }

        public static void Clear()
        {
            for(int i=0; i<w*h; ++i)
            {
                buf[i].Char.AsciiChar = (byte)' ';
                buf[i].Attributes = (short)ConsoleColor.Gray;
            }
        }

        public static void Draw()
        {
            WriteConsoleOutput(handle, buf, buf_size, buf_pos, ref rect);
        }

        private static float Tick()
        {
            long new_time;
            QueryPerformanceCounter(out new_time);
            float dt = (float)(new_time - timer) / freq;
            timer = new_time;
            return dt;
        }

        public static float Update()
        {
            float dt = 0;
            while(true)
            {
                dt += Tick();
                if (dt >= 1.0f / frames)
                    break;
                Thread.Sleep(0);
            }
            return dt;
        }

        public static ConsoleKeyInfo ReadKey()
        {
            return System.Console.ReadKey(true);
        }

        public static void WriteText(string text, Pos pos)
        {
            for (int x = 0; x < text.Length; ++x)
                Console.buf[x + pos.x + pos.y * Console.Width].Char.UnicodeChar = text[x];
        }

        public static void WriteText(string text, Pos pos, short color)
        {
            for (int x = 0; x < text.Length; ++x)
                Console.buf[x + pos.x + pos.y * Console.Width].Set(text[x], color);
        }

        public static short MakeColor(ConsoleColor front, ConsoleColor back)
        {
            return (short)(((int)front) | (((int)back) << 4));
        }
    }
}
