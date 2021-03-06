﻿using System;
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

        TraceEventType LastEventType;

        public TraceWriterWrapper(ListView view)
        {
            this.view = view;
            LastEventType = TraceEventType.Verbose;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            LastEventType = eventType;
            base.TraceEvent(eventCache, source, eventType, id, message);
            LastEventType = TraceEventType.Verbose;
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(
                  string message)
        {
            ListViewItem item = new ListViewItem(new string[] { message, DateTime.Now.ToString("yyyy/MM/dd-hh:mm:ss") });
            item.ImageKey = LastEventType.ToString();
            
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
