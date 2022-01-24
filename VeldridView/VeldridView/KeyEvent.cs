using System;
using System.Collections.Generic;
using System.Text;

namespace VeldridView
{
    public struct KeyEvent
    {
        public string Key { get; }
        public bool Down { get; }

        public KeyEvent(string key, bool down)
        {
            Key = key;
            Down = down;
        }

    }


}
