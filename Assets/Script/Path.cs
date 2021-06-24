using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{

    // Color of the line that will draw the path
    public Color lineColor;

    // List to hold all the path nodes
    private List<Transform> pathNodes = new List<Transform>();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = lineColor;

        // Gets all the pathnodes location in the scene
        Transform[] pathTransforms = GetComponentsInChildren<Transform>();
        // Make sure the list is empty at the start
        pathNodes = new List<Transform>();

        // Adds the waypoints to the pathNodes
        for (int i = 0; i < pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != transform)
            {
                pathNodes.Add(pathTransforms[i]);
            }
        }

        // Draws the path from start to finish
        for (int i = 0; i < pathNodes.Count; i++)
        {
            Vector3 currentNode = pathNodes[i].position;
            Vector3 previousNode = Vector3.zero;
            if (i > 0)
            {
                previousNode = pathNodes[i - 1].position;
            }
            else if (i == 0 && pathNodes.Count > 1)
            {
                previousNode = pathNodes[pathNodes.Count - 1].position;
            }

            Gizmos.DrawLine(previousNode, currentNode);
            Gizmos.DrawWireSphere(currentNode, 0.3f);
        }
    }
}
