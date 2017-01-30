using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Drawing.Drawing2D;

namespace PHOCapturer
{
    public class PHOCapturerWorker
    {
        private ManualResetEvent _mreWorkerEnd = new ManualResetEvent(false);
        private ManualResetEvent _mreCaptureEnd = new ManualResetEvent(false);
        private ManualResetEvent _mreCapture = new ManualResetEvent(false);
        private Thread _thScreenWorker = null;
        private Thread _thCaptureWorker = null;
        private Queue<Bitmap> _screenQueue = null;

        private object _oLock = new object();
        private string _filePath = "temp.gif";

        public PHOCapturerWorker()
        {
            Debug.WriteLine("Konstruktor PHOCapturerWorker()");
            _screenQueue = new Queue<Bitmap>(10); //TODO: Parametr
        }

        public void Start()
        {
            Debug.WriteLine("Start PHOCapturerWorker()");
            if (_thScreenWorker == null)
            {
                Debug.WriteLine("Start ScreenWorker()");
                ThreadStart thStartScreenWorker = new ThreadStart(ScreenWorker);
                _thScreenWorker = new Thread(thStartScreenWorker);
                _thScreenWorker.Start();
            }

            if (_thCaptureWorker == null)
            {
                Debug.WriteLine("Start CaptureWorker()");
                ThreadStart thStartCaptureWorker = new ThreadStart(CaptureWorker);
                _thCaptureWorker = new Thread(thStartCaptureWorker);
                _thCaptureWorker.Start();
            }
        }

        public void Stop()
        {
            Debug.WriteLine("Stop PHOCapturerWorker()");

            _mreWorkerEnd.Set();
            _mreCaptureEnd.Set();

            //Thread.Sleep + 1000);
            Debug.WriteLine(TimeSpan.Parse("00:00:02").Milliseconds);
            if (_thScreenWorker != null)
            {
                Debug.WriteLine("Stop ScreenWorker()");
                _thScreenWorker.Join((int)TimeSpan.Parse("00:00:01").TotalMilliseconds * 2);
                _thScreenWorker.Interrupt();
            }

            if (_thCaptureWorker != null)
            {
                Debug.WriteLine("Stop CaptureWorker()");
                _thCaptureWorker.Join((int)TimeSpan.Parse("00:00:02").TotalMilliseconds * 2);
                _thCaptureWorker.Interrupt();
            }

            _thScreenWorker = null;
            _thCaptureWorker = null;
        }

        public void Capture(string filePath)
        {
            if (_thScreenWorker != null && _thCaptureWorker != null)
            {
                Debug.WriteLine("-------------CAPTURE !!!!!-----------------");
                _filePath = filePath;
                _mreCapture.Set();
            }
        }

        private void ScreenWorker()
        {
            while (true)
            {
                Debug.WriteLine("ScreenWorker() Wait 00:00:01");
                bool bEnd = _mreWorkerEnd.WaitOne(TimeSpan.Parse("00:00:01")); // TODO: Parametr

                Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

                using (Graphics graph = Graphics.FromImage(bitmap))
                {
                    graph.CompositingMode = CompositingMode.SourceOver;
                    graph.CompositingQuality = CompositingQuality.HighSpeed;
                    graph.InterpolationMode = InterpolationMode.Low;
                    graph.SmoothingMode = SmoothingMode.HighSpeed;
                    graph.PixelOffsetMode = PixelOffsetMode.HighSpeed;

                    graph.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                    lock (_oLock)
                    {
                        Debug.WriteLine("Add Screen to Queue");

                        while (_screenQueue.Count >= 10)
                            _screenQueue.Dequeue().Dispose();

                        _screenQueue.Enqueue(bitmap);

                        Debug.WriteLine("Screen count: " + _screenQueue.Count);
                    }
                }

                if (bEnd)
                {
                    Debug.WriteLine("Break ScreenWorker()");

                    while (_screenQueue.Count > 0)
                        _screenQueue.Dequeue().Dispose();

                    _mreWorkerEnd.Reset();
                    break;
                }
            }
        }

        [Obsolete]
        private void CaptureWorker()
        {
            while (true)
            {
                bool bEnd = _mreCaptureEnd.WaitOne(25);

                if (_mreCapture.WaitOne(0))
                {
                    Debug.WriteLine("Wait gif");
                    Thread.Sleep(TimeSpan.Parse("00:00:02"));
                    Debug.WriteLine("Create gif");

                    Bitmap[] tmpScreens = new Bitmap[10];
                    GifBitmapEncoder gifEncoder = new GifBitmapEncoder();

                    lock (_oLock)
                    {
                        _screenQueue.CopyTo(tmpScreens, 0);
                    }

                    foreach (Bitmap img in tmpScreens)
                    {
                        if (img == null) continue;

                        // TUTAJ NIE JEST CZYSZCZONA PAMIEć
                        // MECHANIZM TWORZENIA GIFA JEST DO DUPY
                        // BitmapSource i BitmapFrame nie ma Dispose co powoduje wycieki pamieci

                        BitmapSource bmpSrc = Imaging.CreateBitmapSourceFromHBitmap(img.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        gifEncoder.Frames.Add(BitmapFrame.Create(bmpSrc));
                    }

                    using (FileStream fs = new FileStream(_filePath, FileMode.Create))
                    {
                        gifEncoder.Save(fs);
                    }

                    gifEncoder.Frames.Clear(); // czyszczenie ramek gifa

                    foreach (var img in tmpScreens) img.Dispose();

                    GC.SuppressFinalize(gifEncoder);
                    GC.SuppressFinalize(tmpScreens);
                    GC.Collect();
                    _mreCapture.Reset();
                }

                if (bEnd)
                {
                    Debug.WriteLine("Break Capture Worker()");
                    _mreCaptureEnd.Reset();
                    break;
                };
            }
        }
    }
}
