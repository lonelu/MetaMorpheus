using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Remoting.Messaging;

namespace RealTimeGUI
{
    public class LogWatcher
    {
        private string logContent;


        public event EventHandler Updated;

        public string LogContent
        {
            get { return logContent; }
        }


        public void HandleUpdate(object sender, EventArgs e)
        {


            // Then alert the Updated event that the LogWatcher has been updated

            //Updated?.BeginInvoke(this, new EventArgs());

            //Updated?.Invoke(this, new EventArgs());

            Updated?.BeginInvoke(this, new EventArgs(), callBack, null);
        }

        private void callBack(IAsyncResult asyncResult)
        {
            var syncResult = (AsyncResult)asyncResult;
            var invokedMethod = (EventHandler)syncResult.AsyncDelegate;

            try
            {
                invokedMethod.EndInvoke(asyncResult);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
