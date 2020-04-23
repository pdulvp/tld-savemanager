using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaveManager
{
    public class TraceWriterWrapper : TraceListener
    {
        ListView view;

        public TraceWriterWrapper(ListView view)
        {
            this.view = view;
        }

        public override void Write(string message)
        {
            WriteLine(message + "\n");
        }

        public override void WriteLine(
                  string message)
        {
            ListViewItem item = new ListViewItem(new string[] { message, DateTime.Now.ToString("yyyy/MM/dd-hh:mm:ss") });

            if (view.InvokeRequired)
            {
                view.Invoke(new MethodInvoker(delegate
                {
                    view.Items.Add(item);
                }));
            }
            else
            {
                view.Items.Add(item);
            }
        }
    }

}
