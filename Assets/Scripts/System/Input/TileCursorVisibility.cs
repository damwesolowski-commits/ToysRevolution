using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class TileCursorVisibility : MonoBehaviour
{
    [SerializeField] private InputActionReference clickLeftAction; // przypnij w Inspectorze akcję ClickLeft
    private SpriteRenderer sr;
    private InputAction _action;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _action = clickLeftAction.action;
        _action.started += OnMouseDown;  // wciśnięto
        _action.canceled += OnMouseUp;    // puszczono
        _action.Enable();
    }

    private void OnDisable()
    {
        if (_action != null)
        {
            _action.started -= OnMouseDown;
            _action.canceled -= OnMouseUp;
            _action.Disable();
        }
    }

    private void OnMouseDown(InputAction.CallbackContext ctx)
    {
        sr.enabled = false;
    }

    private void OnMouseUp(InputAction.CallbackContext ctx)
    {
        sr.enabled = true;
    }
}
