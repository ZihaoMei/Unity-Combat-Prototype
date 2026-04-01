using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;            //
    public Transform playerPivot;       //

    [Header("Settings")]
    public float followSpeed = 10f;     //
    public float rotationSpeed = 100f;  //
    public float verticalOffset = 1.5f; //

    [Header("Orbit Settings")]
    public float distance = 5f;          //
    public float minVerticalAngle = -20f; //
    public float maxVerticalAngle = 60f; //

    // --- 追加：衝突回避設定 ---
    [Header("Collision Settings")]
    public bool checkCollision = true;  // 衝突検知を有効にするか
    public LayerMask collisionLayers;   // 衝突を検知するレイヤー（Environmentなど）
    public float cameraRadius = 0.3f;    // カメラの球体サイズ
    public float collisionBuffer = 0.2f; // 壁からの最小隙間
    // ----------------------------

    private float currentX = 0f;        //
    private float currentY = 0f;        //

    void Start()
    {
        Vector3 angles = transform.eulerAngles; //
        currentX = angles.y; //
        currentY = angles.x; //
    }

    void LateUpdate()
    {
        if (target == null) return; //

        // マウス入力を取得
        currentX += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime; //
        currentY -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime; //

        // 垂直角度を制限
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle); //

        // --- 回転の計算 ---
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f); //

        // --- 位置の計算（防壁処理付き）---
        
        // 1. ターゲットの基準点（プレイヤーの頭付近）
        Vector3 targetPivotPosition = target.position + Vector3.up * verticalOffset;

        // 2. 理想的な位置（壁がない場合）
        Vector3 desiredPosition = targetPivotPosition - (rotation * Vector3.forward * distance);

        // 3. 実際のカメラ位置
        Vector3 finalPosition = desiredPosition;

        if (checkCollision)
        {
            // 4. 球形射线でターゲットから理想位置へのパスをチェック
            RaycastHit hit;
            Vector3 rayDirection = desiredPosition - targetPivotPosition;
            float rayLength = desiredPosition.magnitude; // distance でも可だが、より厳密に

            // SphereCast(開始点, 半径, 方向, 結果, 距離, レイヤー)
            if (Physics.SphereCast(targetPivotPosition, cameraRadius, rayDirection.normalized, out hit, distance, collisionLayers))
            {
                // 壁に当たった場合：当たった場所から衝突バッファ分だけ手前に配置
                float adjustedDistance = hit.distance - collisionBuffer;
                
                // 距離が負にならないように保障
                adjustedDistance = Mathf.Max(adjustedDistance, 0.1f); 

                finalPosition = targetPivotPosition - (rotation * Vector3.forward * adjustedDistance);
            }
        }

        // --- 位置と回転の適用 ---
        transform.rotation = rotation; //
        // transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime); // Lerpだと壁めり込みが一瞬見えるため、防壁時は即時移動を推奨
        
        // 防壁時はカクつきを防ぐため即時、それ以外はスムーズ、など調整可能だが、まずは即時が確実
        transform.position = finalPosition; 

        // プレイヤーの向きをカメラに合わせる（既存）
        if (playerPivot != null)
        {
            playerPivot.rotation = Quaternion.Euler(0, currentX, 0); //
        }
    }
}