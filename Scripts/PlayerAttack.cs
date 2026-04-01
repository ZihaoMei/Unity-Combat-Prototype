using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public WeaponHitbox weapon;
    private Animator anim;
    private PlayerStamina stamina;
    private PlayerHealth health;

    void Start()
    {
        anim = GetComponent<Animator>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<PlayerHealth>();

        if (weapon == null) weapon = GetComponentInChildren<WeaponHitbox>();
    }

    void Update()
    {
        if (health != null && health.isDead) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isLocked = stateInfo.IsTag("Lock");
        bool isAttacking = stateInfo.IsTag("Attack");
        bool isRolling = stateInfo.IsTag("Rolling");

        if (Input.GetMouseButtonDown(0) && !isAttacking && !isRolling && !isLocked)
        {
            if (stamina.TryConsumeStamina(stamina.attackCost))
            {
                anim.SetTrigger("Attack");
            }
        }
    }

    public void StartHitbox()
    {
        if (weapon != null) weapon.StartHitbox();
    }

    public void StopHitbox()
    {
        if (weapon != null) weapon.StopHitbox();
    }
}