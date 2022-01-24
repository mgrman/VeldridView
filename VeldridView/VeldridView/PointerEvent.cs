using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace VeldridView
{
    public struct PointerEvent
    {
        public Vector2 PointerTranslation { get; }

        public float Scale { get; }
        public float WheelDelta { get; }

        public PointerEvent(Vector2 pointerTranslation, float scale, float wheelDelta)
        {
            PointerTranslation = pointerTranslation;
            Scale = scale;
            WheelDelta = wheelDelta;
        }
    }


}
