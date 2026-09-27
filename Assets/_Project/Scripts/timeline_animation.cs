using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class timeline_animation : MonoBehaviour
{
    
    public BoidManager bm;
    public WorldCoordinatesVisualizer wcv;

    void Start()
    {
        StartCoroutine(routine());
    }

    
    private IEnumerator routine(){

        yield return new WaitForSeconds(4f);

        wcv.showAxes = true;

        yield return new WaitForSeconds(5f);

        bm.showLinePosition = true;

        yield return new WaitForSeconds(1f);

        bm.showLineVelocity = true;

        yield return new WaitForSeconds(2f);

        bm.showBoundRadius = true;

    }
}
