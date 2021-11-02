using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sensors : MonoBehaviour
{

    private List<GameObject> onSensor = new();
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Unit>())
        {
            onSensor.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        onSensor.Remove(other.gameObject);
    }

    public GameObject LocateNearestEnemy(Unit self)
    {
        var pos = self.transform.position;
        var enemyUnits = onSensor
            .Where(unit => unit != self.gameObject)
            .Where(unit => unit.GetComponent<Unit>().team != self.team)
            .ToList();

        GameObject enemy = null;
        float sqrEnemyDistance = float.MaxValue;
        foreach (var enemyUnit in enemyUnits)
        {
            var d = (pos - enemyUnit.transform.position).sqrMagnitude;
            if (sqrEnemyDistance > d)
            {
                sqrEnemyDistance = d;
                enemy = enemyUnit;
            }
        }

        return enemy;
    }

    public void CleanupInactiveUnits()
    {
        onSensor.RemoveAll(unit => !unit.activeSelf);
    }

    public List<Unit> LocateNearbyUnits(Unit self, float radius, bool ignoreY = true)
    {
        var pos = self.transform.position;
        var enemyUnits = onSensor
            .Where(unit => unit != self.gameObject)
            .Select(unit => unit.GetComponent<Unit>())
            .ToList();

        enemyUnits.RemoveAll(unit =>
        {
            var distance = (unit.transform.position - pos);
            if (ignoreY)
            {
                distance = new Vector3(distance.x, 0, distance.z);
            }
            return distance.sqrMagnitude > radius * radius;
        });
        return enemyUnits;
    }
}
