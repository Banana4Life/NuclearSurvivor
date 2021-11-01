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

    public Unit LocateNearestEnemy(Unit self)
    {
        var pos = self.transform.position;

        onSensor.RemoveAll(unit => !unit.activeSelf);

        var enemyUnits = onSensor
            .Where(unit => unit != self.gameObject)
            .Select(unit => unit.GetComponent<Unit>())
            .Where(unitScript => unitScript.team != self.team)
            .ToList();

        Unit enemy = null;
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
}
