using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float regenRate = 33.3f; // 100 / 3s
    
    [Header("Action Costs")]
    public float rollCost = 30f;
    public float attackCost = 25f;
    public float dashCostPerSec = 40f;

    [Header("Penalty Settings")]
    public float penaltyDuration = 2.0f; // スタミナ使い切り時の回復停止時間
    public bool isExhausted = false;

    [Header("UI Reference")]
    public Slider staminaSlider;
    public Image fillImage; // 色を変える演出用（任意）

    private float penaltyTimer = 0f;
    private Animator anim;

    void Start()
    {
        currentStamina = maxStamina;
        anim = GetComponent<Animator>();        
        if (staminaSlider != null) staminaSlider.maxValue = maxStamina;
    }

    void Update()
    {
    AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

    // 回復を停止すべき状態かチェック
    // Tagを使う場合: "Attack", "Rolling", "Run"（ステートにTagを設定しておく）
    // 名前を使う場合: "Run", "Attack1" など
    bool isBusy = stateInfo.IsTag("Attack") || 
                  stateInfo.IsTag("Rolling") || 
                  stateInfo.IsName("Run"); // Animator上のステート名が正確にRunであること

    HandleRegeneration(isBusy);
    UpdateUI();
    }

    private void HandleRegeneration(bool isBusy)
    {
    // 1. ペナルティ中、またはアクション中（isBusy）は回復しない
    if (penaltyTimer > 0 || isBusy)
    {
        if (penaltyTimer > 0) penaltyTimer -= Time.deltaTime;
        return; 
    }

    // 2. ペナルティ終了判定
    if (isExhausted && penaltyTimer <= 0)
    {
        isExhausted = false;
    }

    // 3. 通常回復（満タンでなければ加算）
    if (currentStamina < maxStamina)
    {
        currentStamina += regenRate * Time.deltaTime;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }
    }

    // アクション実行可否の確認と消費
    public bool TryConsumeStamina(float amount)
    {
        // スタミナが0以下、または必要量に足りない場合は不可（ペナルティ中も不可）
        if (currentStamina <= 0 || isExhausted) return false;

        currentStamina -= amount;

        // 使い切った場合のペナルティ発動
        if (currentStamina <= 0)
        {
            TriggerExhaustion();
        }

        return true;
    }

    // ダッシュ用（毎フレーム呼ぶ）
    public void ConsumeDashStamina()
    {
        if (currentStamina > 0 && !isExhausted)
        {
            currentStamina -= dashCostPerSec * Time.deltaTime;
            if (currentStamina <= 0) TriggerExhaustion();
        }
    }

    private void TriggerExhaustion()
    {
        currentStamina = 0f;
        isExhausted = true;
        penaltyTimer = penaltyDuration;
    }

    private void UpdateUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        // 視覚的フィードバック（ペナルティ中は赤くする等）
        if (fillImage != null)
        {
            fillImage.color = isExhausted ? Color.red : Color.yellow;
        }
    }
}