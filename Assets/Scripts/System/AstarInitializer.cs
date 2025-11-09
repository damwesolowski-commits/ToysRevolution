using UnityEngine;
using System.Collections;

public class AstarInitializer : MonoBehaviour
{
    IEnumerator Start()
    {
        // poczekaj 1 klatkę aż wszystko (tilemapy, bloki, mosty) się ustawi
        yield return null;

        if (AstarPath.active != null)
        {
            // 1) Pełny scan grafu
            AstarPath.active.Scan();

            // 2) Po scanie ponownie narzuć stan bloków na graf A*
            var blocks = FindObjectsOfType<ColorBlock>();
            foreach (var b in blocks)
                b.ReapplyForAstar();

            // (opcjonalnie) natychmiastowa synchronizacja
            AstarPath.active.FlushGraphUpdates();
        }
    }
}
