using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SelectableHighlight : MonoBehaviour
{
    public Color selectedColor = new Color(0.2f, 1f, 0.2f, 1f);
    private Color original;
    private SpriteRenderer sr;

    public bool IsSelected { get; private set; } = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        original = sr.color;
        // Upewnij się, że zawsze startujemy jako niezaznaczony
        IsSelected = false;
        sr.color = original;
    }

    public void SetSelected(bool value)
    {
        IsSelected = value;
        sr.color = IsSelected ? selectedColor : original;
    }
}
