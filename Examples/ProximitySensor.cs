using System.Collections;
using UnityEngine;
using Surrogates;


public class ProximitySensor : SystemBehaviour<ProximitySensor>
{
    public float range = 100;
    public float refreshPeriod = 1;
    public float lastScanTime;

    Collider[] results = new Collider[32];
    int count;

    public static void UpdateBatch()
    {
        var now = Time.time;
        foreach (var i in Instances)
        {
            if (now - i.lastScanTime > i.refreshPeriod)
            {
                i.count = Physics.OverlapSphereNonAlloc(i.transform.position, i.range, i.results);
                i.lastScanTime = now;
            }
        }
    }
}
