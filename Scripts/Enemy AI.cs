using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Charge,
        Stun,
        PartBreak,
        LastStand
    }

    public EnemyState currentState = EnemyState.Idle; //

    [Header("Distance Settings")]
    public float detectRange = 15f; //
    public float chargeRange = 10f; //
    public float moveSpeed = 1.5f; //
    public float chargeSpeed = 10f; //

    [Header("Action Logic")]
    public float globalCooldown = 1.0f; //
    public float chargeDurationLimit = 2.0f; //

    [Header("Combat Settings")]
    public WeaponHitbox weapon;         //
    public WeaponHitbox bodyHitbox;     //
    public WeaponHitbox parasiteHitbox; //

    [Header("PartBreak Settings")]
    public Transform[] partBreakPieces;      //
    public float partBreakStartDelay = 0.15f; //
    public float partBreakBetweenDelay = 0.12f; //
    public float partBreakRecoverDelay = 0.8f; //
    public float partBreakImpulse = 2.5f; //
    public float partBreakUpImpulse = 1.5f; //

    [Header("Last Stand Settings")]
    public Transform parasiteModel;        // 寄生体のモデル
    public Rigidbody parasiteRb;           // 寄生体のRigidbody
    public Animator parasiteAnim;          // 寄生体のAnimator
    public float fakeDeathAnimDuration = 2.5f; // 本体が倒れるアニメーションの長さ（動画に合わせて調整）
    public float shakeDuration = 3.0f;     // 振動する時間
    public float shakeIntensity = 0.05f;   // 振動の強さ
    public float parasiteSpeed = 15f;      // 寄生体が飛ぶ速度

    private Transform player; //
    private CharacterController controller; //
    private Animator anim; //

    private bool isActing = false; //
    private bool isChargePhysicsActive = false; //
    private bool hasPartBreakTriggered = false; //
    private float lastActionEndTime = -10f; //
    private float lastChargeTime = -20f; //
    private float chargeTimer = 0f; //

    private Coroutine partBreakCoroutine; //

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); //
        if (playerObj != null) player = playerObj.transform; //

        controller = GetComponent<CharacterController>(); //
        anim = GetComponentInChildren<Animator>(); //

        PreparePartBreakPieces(); //
    }

    void Update()
    {
        if (player == null) return; //
        if (currentState == EnemyState.LastStand) return; //

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0); //

        if (stateInfo.IsName("Idle") && !anim.IsInTransition(0)) //
        {
            if (isActing) //
            {
                isActing = false; //
                isChargePhysicsActive = false; //
                lastActionEndTime = Time.time; //

                if (currentState == EnemyState.Stun || currentState == EnemyState.PartBreak || currentState == EnemyState.Attack || currentState == EnemyState.Charge) //
                {
                    currentState = EnemyState.Idle; //
                }
            }
        }

        if (stateInfo.IsTag("Charge")) //
        {
            if (isActing && currentState == EnemyState.Charge) //
            {
                HandleChargePhysics(); //
            }
        }
        else //
        {
            isChargePhysicsActive = false; //
        }

        if (stateInfo.IsTag("Turning")) //
        {
            FacePlayer(10f); //
        }

        if (isActing) return; //

        if (Time.time < lastActionEndTime + globalCooldown) //
        {
            anim.SetFloat("Speed", 0f); //
            return; //
        }

        HandleLogicByDistance(); //
    }

    void HandleLogicByDistance()
    {
        float distance = Vector3.Distance(transform.position, player.position); //

        if (distance > detectRange) //
        {
            currentState = EnemyState.Idle; //
            anim.SetFloat("Speed", 0f); //
        }
        else if (distance > chargeRange && Time.time > lastChargeTime + 20f) //
        {
            if (Random.value < 0.8f) StartChargeSequence(); //
            else { currentState = EnemyState.Chase; MoveTowardsPlayer(); } //
        }
        else if (distance > 3f) //
        {
            currentState = EnemyState.Chase; //
            MoveTowardsPlayer(); //
        }
        else //
        {
            currentState = EnemyState.Attack; //
            StartAttack(); //
        }

        anim.SetFloat("Speed", (currentState == EnemyState.Chase) ? 0.5f : 0f); //
    }

    void StartChargeSequence()
    {
        isActing = true; //
        currentState = EnemyState.Charge; //
        chargeTimer = 0f; //
        lastChargeTime = Time.time; //

        StopAllAttackHitboxes(); //
        anim.SetTrigger("Charge"); //
    }

    void HandleChargePhysics()
    {
        isChargePhysicsActive = true; //
        chargeTimer += Time.deltaTime; //

        if (chargeTimer >= chargeDurationLimit) //
        {
            EndCharge(false); //
            return; //
        }

        if (controller != null && controller.enabled) //
        {
            controller.Move(transform.forward * chargeSpeed * Time.deltaTime); //
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!isChargePhysicsActive) return; //

        if (hit.gameObject.CompareTag("Environment")) EndCharge(true); //
        else if (hit.gameObject.CompareTag("Player")) EndCharge(false); //
    }

    void EndCharge(bool hitWall)
    {
        isChargePhysicsActive = false; //
        chargeTimer = 0f; //

        if (hitWall) { currentState = EnemyState.Charge; anim.SetTrigger("Fall"); } //
        else { currentState = EnemyState.Idle; anim.SetTrigger("Idle"); } //
    }

    void StartAttack()
    {
        isActing = true; //
        Vector3 directionToPlayer = (player.position - transform.position).normalized; //
        float angle = Vector3.Angle(transform.forward, directionToPlayer); //
        float spinChance = (angle > 60f) ? 0.6f : 0.3f; //

        if (Random.value < spinChance) anim.SetTrigger("SpinAttack"); //
        else anim.SetTrigger("NormalAttack"); //
    }

    void MoveTowardsPlayer()
    {
        FacePlayer(5f); //
        if (controller != null && controller.enabled) //
        {
            controller.Move(transform.forward * moveSpeed * Time.deltaTime); //
        }
    }

    void FacePlayer(float rotationSpeed)
    {
        Vector3 direction = (player.position - transform.position).normalized; //
        direction.y = 0; //

        if (direction != Vector3.zero) //
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime); //
        }
    }

    public void TriggerStun()
    {
        if (currentState == EnemyState.LastStand) return; //

        isActing = true; //
        isChargePhysicsActive = false; //
        currentState = EnemyState.Stun; //

        StopAllAttackHitboxes(); //
        anim.SetTrigger("Stun"); //
    }

    public void TriggerPartBreak()
    {
        if (hasPartBreakTriggered || currentState == EnemyState.LastStand) return; //

        hasPartBreakTriggered = true; //
        isActing = true; //
        isChargePhysicsActive = false; //
        currentState = EnemyState.PartBreak; //

        StopAllAttackHitboxes(); //
        anim.SetTrigger("PartBreak"); //

        if (partBreakCoroutine != null) StopCoroutine(partBreakCoroutine); //
        partBreakCoroutine = StartCoroutine(PartBreakRoutine()); //
    }

    public void TriggerLastStand()
    {
        isActing = true; //
        isChargePhysicsActive = false; //
        currentState = EnemyState.LastStand; //

        StopAllAttackHitboxes(); //

        if (controller != null) controller.enabled = false; //

        anim.SetTrigger("FakeDeath"); //
        
        // 寄生体のシーケンスを開始
        StartCoroutine(LastStandSequence());
    }

private IEnumerator LastStandSequence()
    {
        // 1. 本体の死亡アニメーションが終わるまで待つ
        yield return new WaitForSeconds(fakeDeathAnimDuration);

        // 2. 予兆：死体を3秒間振動させる
        float timer = 0f;
        Transform modelTransform = anim.transform;
        Vector3 originalLocalPos = modelTransform.localPosition;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            modelTransform.localPosition = originalLocalPos + Random.insideUnitSphere * shakeIntensity;
            yield return null;
        }
        modelTransform.localPosition = originalLocalPos; 

        // ★修正1：振動終了後、本体のAnimatorを無効化して死体を凍結する（棒立ちを防ぐ）
        anim.enabled = false;

        // 3. 寄生体の分離と射出
        if (parasiteModel != null && parasiteRb != null)
        {
            parasiteModel.SetParent(null, true);

            // ★修正2：Rigidbodyの物理移動ではなく、座標を強制移動させるためKinematicにする
            parasiteRb.isKinematic = true; 
            parasiteRb.useGravity = false;

            Vector3 targetPos = player != null ? player.position + Vector3.up * 1.0f : parasiteModel.position + transform.forward;
            Vector3 flyDirection = (targetPos - parasiteModel.position).normalized;
            parasiteModel.rotation = Quaternion.LookRotation(flyDirection);

            StartParasiteHitbox();

            // ★修正3：Updateのように毎フレーム強制的に前へ移動させる（確実に飛ぶ）
            float flyTimer = 0f;
            while (flyTimer < 3.0f)
            {
                flyTimer += Time.deltaTime;
                parasiteModel.position += flyDirection * parasiteSpeed * Time.deltaTime;
                yield return null;
            }

            // 4. 寄生体の落下と死亡 (3秒間飛ばなかった/当たらなかった場合)
            StopParasiteHitbox();

            // 重力を有効化して地面に落とす
            parasiteRb.isKinematic = false;
            parasiteRb.useGravity = true;

            if (parasiteAnim != null)
            {
                parasiteAnim.SetTrigger("Die");
            }
            
            Destroy(parasiteModel.gameObject, 3f);
        }
    }

    private IEnumerator PartBreakRoutine()
    {
        yield return new WaitForSeconds(partBreakStartDelay); //
        if (partBreakPieces != null) //
        {
            for (int i = 0; i < partBreakPieces.Length; i++) //
            {
                DetachPart(partBreakPieces[i]); //
                yield return new WaitForSeconds(partBreakBetweenDelay); //
            }
        }
        yield return new WaitForSeconds(partBreakRecoverDelay); //
        if (currentState == EnemyState.PartBreak) { currentState = EnemyState.Idle; isActing = false; } //
        partBreakCoroutine = null; //
    }

    private void DetachPart(Transform part)
    {
        if (part == null) return; //
        part.SetParent(null, true); //
        Rigidbody rb = part.GetComponent<Rigidbody>(); //
        Collider col = part.GetComponent<Collider>(); //
        if (col != null) col.enabled = true; //
        if (rb != null) //
        {
            rb.isKinematic = false; //
            rb.useGravity = true; //
            rb.linearVelocity = Vector3.zero; //
            rb.angularVelocity = Vector3.zero; //
            Vector3 forceDir = (transform.forward + Vector3.up * partBreakUpImpulse).normalized; //
            rb.AddForce(forceDir * partBreakImpulse, ForceMode.Impulse); //
            rb.AddTorque(Random.insideUnitSphere * partBreakImpulse, ForceMode.Impulse); //
        }
    }

    private void PreparePartBreakPieces()
    {
        if (partBreakPieces == null) return; //
        foreach (Transform part in partBreakPieces) //
        {
            if (part == null) continue; //
            Rigidbody rb = part.GetComponent<Rigidbody>(); //
            Collider col = part.GetComponent<Collider>(); //
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; } //
            if (col != null) col.enabled = false; //
        }
    }

    public void StartHitbox() { if (weapon != null) weapon.StartHitbox(); } //
    public void StopHitbox() { if (weapon != null) weapon.StopHitbox(); } //
    public void StartBodyHitbox() { if (bodyHitbox != null) bodyHitbox.StartHitbox(); } //
    public void StopBodyHitbox() { if (bodyHitbox != null) bodyHitbox.StopHitbox(); } //
    public void StartParasiteHitbox() { if (parasiteHitbox != null) parasiteHitbox.StartHitbox(); } //
    public void StopParasiteHitbox() { if (parasiteHitbox != null) parasiteHitbox.StopHitbox(); } //
    
    // このメソッドは不要になった可能性がありますが、アニメーションイベントから呼ばれるエラーを防ぐために残しています
    public void FinishLastStand() { } 

    private void StopAllAttackHitboxes()
    {
        if (weapon != null) weapon.StopHitbox(); //
        if (bodyHitbox != null) bodyHitbox.StopHitbox(); //
        if (parasiteHitbox != null) parasiteHitbox.StopHitbox(); //
    }
}