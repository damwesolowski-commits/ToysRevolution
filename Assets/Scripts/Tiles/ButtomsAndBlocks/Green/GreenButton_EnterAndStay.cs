using UnityEngine;

public class GreenButton_EnterAndStay : ButtonBase
{
    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<GreenBlockRegistry>();
    }

    // Wejście na przycisk: zawsze przełącz stan
    protected override void OnPressed(GameObject unit)
    {
        if (blockRegistry == null) return;
        blockRegistry.ToggleGroup(groupId);
        // Debug.Log($"[GreenButton_EnterAndStay] Press -> toggle group {groupId}");
    }

    // Zejście z przycisku: ponownie przełącz stan
    protected override void OnReleased(GameObject unit)
    {
        if (blockRegistry == null) return;
        blockRegistry.ToggleGroup(groupId);
        // Debug.Log($"[GreenButton_EnterAndStay] Release -> toggle group {groupId}");
    }
}
