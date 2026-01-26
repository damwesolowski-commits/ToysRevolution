#if UNITY_EDITOR
using UnityEngine;

[ExecuteAlways]
public class EditorOnlyMarker : MonoBehaviour
{
    void Update()
    {
        // Widoczne tylko w edytorze
        gameObject.SetActive(!Application.isPlaying);
    }
}
#endif
