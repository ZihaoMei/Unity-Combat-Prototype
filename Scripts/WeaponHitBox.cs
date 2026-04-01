using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour
{
    [Header("Settings")]
    public string targetTag; //
    public float damage = 10f; //
    public bool isHeavyAttack = false; //

    [Header("Hit Feedback")]
    public float stopDuration = 0.05f; //
    public float stopScale = 0.05f; //

    // --- 追加：毒設定 ---
    [Header("Poison Settings")]
    public bool isPoisonous = false;     // 毒を持つ攻撃かどうか
    public float poisonTickDamage = 5f;  // 1回あたりの毒ダメージ
    public int poisonTicks = 3;          // 毒ダメージの発生回数
    public float poisonInterval = 1.0f;  // 毒が発生する間隔（秒）

    private Collider weaponCollider; //
    private List<GameObject> hitList = new List<GameObject>(); //

    void Awake()
    {
        weaponCollider = GetComponent<Collider>(); //
        if (weaponCollider == null) weaponCollider = GetComponentInChildren<Collider>(); //

        if (weaponCollider != null) //
        {
            weaponCollider.enabled = false; //
            weaponCollider.isTrigger = true; //
        }
    }

    public void StartHitbox() 
    {
        if (weaponCollider == null) return; //
        hitList.Clear(); //
        weaponCollider.enabled = true; //
    }

    public void StopHitbox() 
    {
        if (weaponCollider == null) return; //
        weaponCollider.enabled = false; //
    }

    public void SetDamage(float val) { damage = val; } //

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag) || hitList.Contains(other.gameObject)) return; //

        hitList.Add(other.gameObject); //
        
        if (targetTag == "Player") //
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>(); //
            if (health != null) 
            {
                // 通常の即時ダメージ
                health.TakeDamage(damage, isHeavyAttack); //

                // --- 追加：毒属性なら毒を付与 ---
                if (isPoisonous)
                {
                    health.ApplyPoison(poisonTickDamage, poisonTicks, poisonInterval);
                }
            }
        }
        else if (targetTag == "Enemy") //
        {
            EnemyHealth health = other.GetComponent<EnemyHealth>(); //
            if (health != null) health.TakeDamage((int)damage); //
        }

        TriggerEffects(other); //
    }

    private void TriggerEffects(Collider other)
    {
        HitStopController.TriggerHitStop(stopDuration, stopScale); //
        Vector3 hitPoint = other.ClosestPoint(transform.position); //
        if (SimpleObjectPool.Instance != null) //
        {
            SimpleObjectPool.Instance.GetBlood(hitPoint, Quaternion.identity); //
        }
    }
}