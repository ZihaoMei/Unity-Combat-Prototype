using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public int maxHp = 500;
    public int currentHp;

    [Header("Stun Settings")]
    public int stunThreshold = 120; // 累計ダメージでスタン
    private int accumulatedStunDamage = 0;

    [Header("UI Reference")]
    public GameObject victoryTextUI;
    public float uiDisplayTime = 1.0f; // 追加：表示時間（秒） // VICTORY テキスト

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;

    private EnemyAI ai;

    private bool halfHpReached = false;
    private bool stunTriggered = false;
    private bool lastStandTriggered = false;
    private bool isDead = false;

    void Start()
    {
        currentHp = maxHp;
        ai = GetComponent<EnemyAI>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        accumulatedStunDamage += damage;

        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        Debug.Log("Enemy HP: " + currentHp);

        // 1. HP0：LastStand開始（本体は死亡扱い）
        if (currentHp <= 0)
        {
            isDead = true;
            lastStandTriggered = true;

            if (ai != null)
            {
                ai.TriggerLastStand();
            }

            // --- 追加：VICTORY テキストを表示 ---
            if (victoryTextUI != null)
            {
                victoryTextUI.SetActive(true);
                StartCoroutine(DisableUIAfterSeconds(victoryTextUI, uiDisplayTime));
            }

            return;
        }

        // 2. HP50%：PartBreak 一回だけ
        if (!halfHpReached && currentHp <= maxHp * 0.5f)
        {
            halfHpReached = true;

            if (ai != null)
            {
                ai.TriggerPartBreak();
            }

            return;
        }

        // 3. 累計120ダメージ：Stun 一回だけ
        if (!stunTriggered && accumulatedStunDamage >= stunThreshold)
        {
            stunTriggered = true;
            accumulatedStunDamage = 0;

            if (ai != null)
            {
                ai.TriggerStun();
            }

            return;
        }
    }
    // 追加：指定秒後にUIを非表示にするヘルパー
    private IEnumerator DisableUIAfterSeconds(GameObject uiElement, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (uiElement != null) uiElement.SetActive(false);
    }
}