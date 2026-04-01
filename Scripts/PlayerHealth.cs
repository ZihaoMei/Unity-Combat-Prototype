using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public float maxHealth = 100f; //
    public float currentHealth; //
    public bool isInvincible = false; //
    public bool isDead = false; //

    [Header("Heal Settings")]
    public int currentHeals = 3; //
    public float healPercent = 0.4f; //

    [Header("Visuals")]
    public GameObject torchObject;

    [Header("UI Reference")]
    public Slider healthSlider; //
    // --- 追加：UI要素の参照 ---
    public Image healthBarFill;       // HPバーのFill部分のImage
    public GameObject poisonTextUI;   // POISON テキスト
    public GameObject diedTextUI;     // DIED テキスト
    public float uiDisplayTime = 1.0f; // 追加：テキストの表示時間（秒）

    private Animator anim; //
    private Coroutine poisonCoroutine;

    // --- 追加：HPバーの色の定義 ---
    private Color originalHealthColor = Color.red; // 初期の色（Startで取得）
    private Color poisonHealthColor = new Color(0.5f, 0f, 0.5f); // 紫色

    void Start()
    {
        currentHealth = maxHealth; //
        anim = GetComponent<Animator>(); //
        UpdateUI(); //

        // --- 追加：HPバーの初期色を記憶 ---
        if (healthBarFill != null)
        {
            originalHealthColor = healthBarFill.color;
        }
    }

    void Update()
    {
        if (isDead) return; //

        if (Input.GetKeyDown(KeyCode.R)) //
        {
            TryUseHealItem(); //
        }
    }

    public void TakeDamage(float damage, bool isHeavyAttack)
    {
        if (isDead || isInvincible) return; //

        currentHealth -= damage; //
        currentHealth = Mathf.Max(currentHealth, 0); //
        UpdateUI(); //

        if (torchObject != null && !torchObject.activeSelf)
        {
            torchObject.SetActive(true);
        }

        if (currentHealth <= 0) //
        {
            Die(); //
        }
        else
        {
            if (isHeavyAttack) anim.SetTrigger("FlyingBack"); //
            else anim.SetTrigger("React"); //
        }
    }

    public void ApplyPoison(float tickDamage, int ticks, float interval)
    {
        if (isDead) return; //

        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
        }
        
        // --- 修正：毒付与時にUI演出を開始 ---
        poisonCoroutine = StartCoroutine(PoisonRoutine(tickDamage, ticks, interval));
    }

    private IEnumerator PoisonRoutine(float tickDamage, int ticks, float interval)
    {
        // 1. --- 追加：毒状態のUI演出開始 ---
        
        // HPバーを紫にする
        if (healthBarFill != null) healthBarFill.color = poisonHealthColor;

        // "POISON"テキストを1秒間だけ表示
        if (poisonTextUI != null)
        {
            poisonTextUI.SetActive(true);
            StartCoroutine(DisableUIAfterSeconds(poisonTextUI, 1.0f)); // 1秒後に消すヘルパーコルーチン
        }

        // 2. 毒ダメージのループ
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(interval); //
            
            if (isDead) yield break; //

            currentHealth -= tickDamage; //
            currentHealth = Mathf.Max(currentHealth, 0); //
            UpdateUI(); //

            Debug.Log($"毒ダメージ発生: {tickDamage} 残りHP: {currentHealth}"); //

            if (currentHealth <= 0) //
            {
                Die(); //
                yield break; //
            }
        }

        // 3. --- 追加：毒状態終了時のUI復帰 ---
        
        // HPバーの色を元に戻す
        if (healthBarFill != null) healthBarFill.color = originalHealthColor;
        
        poisonCoroutine = null; //
    }

    // --- 追加：指定秒後にUIを非表示にするヘルパー ---
    private IEnumerator DisableUIAfterSeconds(GameObject uiElement, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (uiElement != null) uiElement.SetActive(false);
    }

    public void StartInvincible() { isInvincible = true; } //
    public void EndInvincible() { isInvincible = false; } //

    private void TryUseHealItem()
    {
        if (currentHeals <= 0) return; //

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0); //
        bool canDrink = state.IsName("Idle") || state.IsName("Walk") || state.IsName("Run"); //

        if (canDrink) //
        {
            anim.SetTrigger("Drink"); //
            if (torchObject != null) torchObject.SetActive(false);
        }
    }

    public void HealEvent()
    {
        currentHeals--; //
        float healAmount = maxHealth * healPercent; //
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth); //
        UpdateUI(); //

        // --- 追加：回復したら毒状態を解除する ---
        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
            poisonCoroutine = null;
            if (healthBarFill != null) healthBarFill.color = originalHealthColor; // 色を戻す
        }
        
        Debug.Log("体力回復完了。残りアイテム：" + currentHeals); //
    }

    public void FinishDrinkEvent()
    {
        if (torchObject != null) torchObject.SetActive(true);
    }

    private void Die()
    {
        isDead = true; //
        anim.SetTrigger("PlayerDeath"); //
        
        // DIED テキストを表示し、指定秒数後に消す
        if (diedTextUI != null)
        {
            diedTextUI.SetActive(true);
            StartCoroutine(DisableUIAfterSeconds(diedTextUI, uiDisplayTime));
        }
        
        // --- 追加：死亡したらHPバーの色を元に戻す（毒のまま死なないように） ---
        if (healthBarFill != null) healthBarFill.color = originalHealthColor;

        Debug.Log("Player Dead"); //
    }

    private void UpdateUI()
    {
        if (healthSlider != null) healthSlider.value = currentHealth / maxHealth; //
    }
}