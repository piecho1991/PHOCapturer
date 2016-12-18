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

namespace PHOCapturer
{
    public class PHOCapturerWorker
    {
        private ManualResetEvent _mreWorkerEnd = new ManualResetEvent(false);
        private ManualResetEvent _mreCaptureEnd = new ManualResetEvent(false);
        private ManualResetEvent _mreCapture = new ManualResetEvent(false);
        private Thread _thScreenWorker = null;
        private Thread _thCaptureWorker = null;
        private Queue<PHOCapturerItem> _screenStack = null;
        private object _oLock = new object();

        public PHOCapturerWorker()
        {
            Debug.WriteLine("Konstruktor PHOCapturerWorker()");
            _screenStack = new Queue<PHOCapturerItem>(10); //TODO: Parametr
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

        public void Capture()
        {
            if (_thScreenWorker != null && _thCaptureWorker != null)
            {
                Debug.WriteLine("-------------CAPTURE !!!!!-----------------");
                _mreCapture.Set();
            }
        }

        private void ScreenWorker()
        {
            while (true)
            {
                Debug.WriteLine("ScreenWorker() Wait 00:00:01");
                bool bEnd = _mreWorkerEnd.WaitOne(TimeSpan.Parse("00:00:01")); // TODO: Parametr

                using (Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
                {
                    using (Graphics graph = Graphics.FromImage(bitmap))
                    {
                        graph.CopyFromScreen(0, 0, 0, 0, bitmap.Size);

                        lock (_oLock)
                        {
                            Debug.WriteLine("Add Screen to Queue");

                            while (_screenStack.Count >= 10)
                                _screenStack.Dequeue();

                            _screenStack.Enqueue(
                                new PHOCapturerItem()
                                {
                                    ScreenBitmap = bitmap,
                                    CreationDate = DateTime.Now
                                }
                            );
                        }
                    }
                }

                if (bEnd)
                {
                    Debug.WriteLine("Break ScreenWorker()");
                    _mreWorkerEnd.Reset();
                    break;
                }
            }
        }

        private void CaptureWorker()
        {
            while (true)
            {
                bool bEnd = _mreCaptureEnd.WaitOne(0);

                Debug.WriteLine("CaptureWorker() Wait 00:00:02");
                if (_mreCapture.WaitOne(TimeSpan.Parse("00:00:02")))
                {
                    PHOCapturerItem[] tmpScreens = new PHOCapturerItem[10];

                    lock (_oLock)
                    {
                        Debug.WriteLine("Copy Queue to Array");
                        _screenStack.CopyTo(tmpScreens, 0);
                    }

                    GifBitmapEncoder gifEncoder = new GifBitmapEncoder();

                    foreach (PHOCapturerItem img in tmpScreens)
                    {
                        if (img == null) continue;

                        Debug.WriteLine("Iterate array - add frames to gif");
                        var bmp = img.ScreenBitmap.GetHbitmap();
                        var bmpSrc = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                        gifEncoder.Frames.Add(BitmapFrame.Create(bmpSrc));
                    }

                    Debug.WriteLine("Save gif");
                    using (FileStream fs = new FileStream("D:\\gif.gif", FileMode.Create))
                    {
                        gifEncoder.Save(fs);
                    }

                    Debug.WriteLine("End Capture");
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
