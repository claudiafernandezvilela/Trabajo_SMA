using UnityEngine;

public abstract class Sensores : MonoBehaviour
{
    protected Cerebro cerebro;

    protected virtual void Awake()
    {
        cerebro = GetComponent<Cerebro>();
    }

    protected abstract void Detect();
}