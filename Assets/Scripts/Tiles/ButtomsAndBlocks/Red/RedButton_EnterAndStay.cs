using UnityEngine;

public class RedButton_EnterAndStay : ButtonBase
{
    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<RedBlockRegistry>();
    }

    // Wejście na przycisk: zawsze przełącz stan
    protected override void OnPressed(GameObject unit)
    {
        if (blockRegistry == null) return;
        blockRegistry.ToggleGroup(groupId);
        // Debug.Log($"[RedButton_EnterAndStay] Press -> toggle group {groupId}");
    }

    // Zejście z przycisku: ponownie przełącz stan
    protected override void OnReleased(GameObject unit)
    {
        if (blockRegistry == null) return;
        blockRegistry.ToggleGroup(groupId);
        // Debug.Log($"[RedButton_EnterAndStay] Release -> toggle group {groupId}");
    }
}
