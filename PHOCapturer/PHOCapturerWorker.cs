using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PHOCapturer
{
    public class PHOCapturerWorker
    {
        private Thread _thWorker = null;
        ManualResetEvent _mreWorker = new ManualResetEvent(false);
        ManualResetEvent _mreCapture = new ManualResetEvent(false);

        public PHOCapturerWorker()
        {

        }

        public void Start()
        {
            ThreadStart thStart = new ThreadStart(Worker);
            _thWorker = new Thread(thStart);
            _thWorker.Start();
        }

        public void Stop()
        {
            if(_thWorker != null)
            {
                _thWorker.Join(1000);
                _thWorker.Interrupt();
            }
        }

        public void Capture()
        {

        }

        private void Worker()
        {
            while(true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
