using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class SecretButton_RuleTile : ButtonBase
{
    [Header("Sekretne Tilemapy")]
    public List<SecretTileData> secretTiles = new();

    [Header("Dźwięki sekretu")]
    public AudioClip secretLoopSound;
    public float secretLoopVolume = 1f;

    private AudioSource loopAudioSource;
    private bool hasBeenPressed = false;

    protected override void OnPressed(GameObject unit)
    {
        if (hasBeenPressed) return; // tylko jedno kliknięcie
        hasBeenPressed = true;
        suppressRelease = true;

        SetPressedVisuals(true);
        PlaySound(pressSound);

        // 🔊 rozpocznij dźwięk sekretu (gra przez cały cykl)
        if (secretLoopSound != null)
        {
            loopAudioSource = gameObject.AddComponent<AudioSource>();
            loopAudioSource.clip = secretLoopSound;
            loopAudioSource.volume = secretLoopVolume;
            loopAudioSource.loop = true;
            loopAudioSource.Play();
        }

        // 🔁 uruchom sekwencję zmian kafli
        StartCoroutine(ActivateSecretTilesSequence());
    }

    private IEnumerator ActivateSecretTilesSequence()
    {
        List<Coroutine> runningCoroutines = new();

        foreach (var data in secretTiles)
        {
            if (data.targetTilemap == null) continue;
            runningCoroutines.Add(StartCoroutine(HandleTileLifecycle(data)));
        }

        // 🔹 czekaj aż wszystkie zakończą działanie
        foreach (var c in runningCoroutines)
            yield return c;

        // 🔇 zakończ dźwięk sekretu
        if (loopAudioSource != null)
        {
            loopAudioSource.Stop();
            Destroy(loopAudioSource);
        }
    }

    private IEnumerator HandleTileLifecycle(SecretTileData data)
    {
        if (data.targetTilemap == null) yield break;

        yield return new WaitForSeconds(data.startDelay);

        Tilemap tilemap = data.targetTilemap;
        Vector3Int pos = data.position;
        TileBase originalTile = tilemap.GetTile(pos);

        // 🔹 aktywacja kafla
        if (data.removeOnActivate)
            tilemap.SetTile(pos, null);
        else
            tilemap.SetTile(pos, data.tileToPlace);

        // 🔄 Odśwież logikę kafla w TileLogicManager
        if (TileLogicManager.Instance != null)
        {
            TileLogicManager.Instance.RefreshTileLogic(pos);
        }

        yield return new WaitForSeconds(data.effectDuration);

        // 🔹 powrót do pierwotnego kafla
        tilemap.SetTile(pos, originalTile);

        // 🔄 Odśwież logikę kafla w TileLogicManager
        if (TileLogicManager.Instance != null)
        {
            TileLogicManager.Instance.RefreshTileLogic(pos);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (secretTiles == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.5f);

        foreach (var data in secretTiles)
        {
            if (data.targetTilemap == null) continue;
            Vector3 worldPos = data.targetTilemap.CellToWorld(data.position) + new Vector3(0.5f, 0.5f, 0);
            Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
        }
    }
#endif
}
