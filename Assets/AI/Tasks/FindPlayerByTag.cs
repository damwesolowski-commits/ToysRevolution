using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class FindPlayerByTag : Conditional
{
    // Zmienna współdzielona – tutaj zapiszemy gracza
    public SharedGameObject player;

    // Nazwa taga, którego szukamy (domyślnie "Player")
    public string tagName = "Player";

    public override TaskStatus OnUpdate()
    {
        // Znajdź wszystkie obiekty z zadanym tagiem (np. "Player")
        GameObject[] players = GameObject.FindGameObjectsWithTag(tagName);

        if (players == null || players.Length == 0)
        {
            // Nie znaleziono żadnego gracza
            return TaskStatus.Failure;
        }

        // Szukamy najbliższego gracza względem tego Enemego
        GameObject closest = null;
        float closestSqrDist = Mathf.Infinity;
        Vector3 myPos = transform.position;

        foreach (var p in players)
        {
            if (p == null) continue;

            float sqrDist = (p.transform.position - myPos).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = p;
            }
        }

        if (closest == null)
        {
            return TaskStatus.Failure;
        }

        // Zapisujemy najbliższego gracza do zmiennej współdzielonej
        player.Value = closest;
        return TaskStatus.Success;
    }
}
