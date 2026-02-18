using UnityEngine;
using System.Collections.Generic;

public class cerebro : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Transform transform;
    public List<Transform> destinos;
    public UnityEngine.AI.NavMeshAgent agent;

    int indice = 0;

    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (destinos.Count > 0)
                agent.destination = destinos[indice].position;
    }

    // Update is called once per frame
    void Update()
    {
      if (destinos.Count == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            indice++;

            if (indice >= destinos.Count)
                indice = 0;

            agent.destination = destinos[indice].position;
        }
    }
}