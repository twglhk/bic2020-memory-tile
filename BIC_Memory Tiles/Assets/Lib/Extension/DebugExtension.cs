
namespace Lib
{
    namespace Extension
    {
        using System.Collections;
        using System.Collections.Generic;
        using UnityEngine;

        public static class DebugExtension
        {
            public static void Assert(this Object _obj)
            {
                Debug.Assert(_obj);
            }
        }
    }
}

