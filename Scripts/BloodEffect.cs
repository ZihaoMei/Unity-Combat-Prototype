using UnityEngine;

public class BloodEffect : MonoBehaviour
{
    public float autoReturnTime = 2.0f;

    void OnEnable()
    {
        CancelInvoke(); 
        Invoke("ReturnToPool", autoReturnTime);
    }

    void ReturnToPool()
    {
        SimpleObjectPool.Instance.ReturnToPool(this.gameObject);
    }
}