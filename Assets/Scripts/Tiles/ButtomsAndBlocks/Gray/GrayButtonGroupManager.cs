using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Globalny menedżer grup przycisków dla wielu postaci.
/// Monitoruje, które przyciski są wciśnięte i uruchamia efekt tylko,
/// gdy liczba aktywnych = liczba wymaganych.
/// </summary>
public class GrayButtonGroupManager : MonoBehaviour
{
    private static GrayButtonGroupManager instance;
    private static Dictionary<int, GroupData> groups = new();

    private class GroupData
    {
        public List<GrayButton> buttons = new();
        public int requiredPlayers = 2;
        public bool isActive = false;
    }

    // ===================================================
    // 🧩 Singleton
    // ===================================================
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    // ===================================================
    // 🧱 Rejestracja przycisków
    // ===================================================
    public static void RegisterButton(GrayButton button)
    {
        if (button == null) return;

        if (!groups.ContainsKey(button.groupId))
            groups[button.groupId] = new GroupData();

        GroupData data = groups[button.groupId];
        if (!data.buttons.Contains(button))
            data.buttons.Add(button);

        // ✅ Zamiast brać z Inspektora – wymagamy tylu, ile jest przycisków w grupie:
        data.requiredPlayers = data.buttons.Count;

        //Debug.Log($"[GrayButtonManager] ➕ Dodano przycisk GID={button.groupId}. " +
                  //$"W grupie jest {data.buttons.Count} przycisków → required={data.requiredPlayers}");
    }

    // ===================================================
    // 🟢 Gdy przycisk zostanie wciśnięty
    // ===================================================
    public static void NotifyButtonPressed(int groupId, GrayButton button)
    {
        if (!groups.ContainsKey(groupId)) return;
        GroupData data = groups[groupId];

        int pressedCount = CountPressed(data);
        Debug.Log($"[GrayButtonManager] Press G{groupId}: pressed={pressedCount}/{data.requiredPlayers}");

        if (!data.isActive && pressedCount >= data.requiredPlayers)
        {
            data.isActive = true;
            ActivateGroupEffect(groupId);
        }
    }

    // ===================================================
    // 🔴 Gdy przycisk zostanie zwolniony
    // ===================================================
    public static void NotifyButtonReleased(int groupId, GrayButton button)
    {
        if (!groups.ContainsKey(groupId)) return;
        GroupData data = groups[groupId];

        int pressedCount = CountPressed(data);
        //Debug.Log($"[GrayButtonManager] Release G{groupId}: pressed={pressedCount}/{data.requiredPlayers}");

        if (data.isActive && pressedCount < data.requiredPlayers)
        {
            data.isActive = false;
            DeactivateGroupEffect(groupId);
        }
    }

    // ===================================================
    // ⚙️ Pomocnicze
    // ===================================================
    private static int CountPressed(GroupData data)
    {
        int count = 0;
        foreach (var btn in data.buttons)
        {
            var field = btn.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null && (bool)field.GetValue(btn))
                count++;
        }
        return count;
    }

    // ===================================================
    // 🎬 Aktywacja / dezaktywacja efektu
    // ===================================================
    private static void ActivateGroupEffect(int groupId)
    {
        // Pobierz TYLKO rejestr szarych bloków
        var registries = GameObject.FindObjectsOfType<GrayBlockRegistry>(true);
        if (registries.Length == 0)
        {
            Debug.LogWarning("[GrayButtonManager] ❌ Nie znaleziono GrayBlockRegistry!");
            return;
        }

        foreach (var r in registries)
            r.ToggleGroup(groupId);

        //Debug.Log($"[GrayButtonManager] ✅ Aktywowano efekt GRAY group {groupId}");
    }

    private static void DeactivateGroupEffect(int groupId)
    {
        var registries = GameObject.FindObjectsOfType<GrayBlockRegistry>(true);
        if (registries.Length == 0)
        {
            Debug.LogWarning("[GrayButtonManager] ❌ Nie znaleziono GrayBlockRegistry!");
            return;
        }

        foreach (var r in registries)
            r.ToggleGroup(groupId);

        //Debug.Log($"[GrayButtonManager] ⛔ Dezaktywowano efekt GRAY group {groupId}");
    }
}
