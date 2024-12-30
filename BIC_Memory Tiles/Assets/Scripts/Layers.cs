
namespace Lib
{
    namespace Utility
    {
        using System.Collections;
        using System.Collections.Generic;
        using UnityEngine;

        public static class Layers
        {
            public const int DefaultLayer = 1 << 0;
            public const int Tile = 1 << 8;
            public const int Player = 1 << 9;

            public static bool Compare(int currentLayer, int anotherLayer)
            {
                if (currentLayer == anotherLayer) return true;
                if (1 << currentLayer == anotherLayer) return true;

                return false;
            }
            public static bool Compare(GameObject gobj, int anotherLayer)
            {
                if (gobj == null) return false;

                int currentLayer = gobj.layer;
                return Compare(currentLayer, anotherLayer);
            }
            public static bool Compare(Transform transform, int anotherLayer)
            {
                if (transform == null) return false;

                int currentLayer = transform.gameObject.layer;
                return Compare(currentLayer, anotherLayer);
            }
        }
    }
}