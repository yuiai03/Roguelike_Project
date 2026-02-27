using UnityEngine;
using TMPro;

/// <summary>
/// Quản lý TextMeshPro nội suy số Damage/Heal nổ ra từ enemy/player
/// </summary>
public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float lifeTime = 0.8f;
    
    [Header("Colors")]
    [SerializeField] private Color normalDamageColor = Color.white;
    [SerializeField] private Color critDamageColor = Color.yellow;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color playerDamageColor = Color.red;

    private float timer;
    private bool isActive;
    private Vector3 moveDirection; // hướng bay ngẫu nhiên mỗi lần spawn

    private void Awake()
    {
        // Tự tìm TextMeshPro nếu chưa gán trong Inspector
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>();
    }

    private void OnEnable()
    {
        isActive = false; // chờ Show() xàc nhận trước khi chạy
    }

    public void Show(float amount, bool isHeal = false, bool isPlayer = false, bool isCrit = false)
    {
        if (textMesh == null)
        {
            Debug.LogError("[DamageText] textMesh chưa được gán!");
            return;
        }

        textMesh.text = Mathf.RoundToInt(amount).ToString();

        if (isHeal)
            textMesh.color = healColor;
        else if (isPlayer)
            textMesh.color = playerDamageColor;
        else if (isCrit)
            textMesh.color = critDamageColor;
        else
            textMesh.color = normalDamageColor;

        // ĐReset alpha về 1 trước khi hiển thị
        Color c = textMesh.color;
        c.a = 1f;
        textMesh.color = c;

        // Random hướng bay: chủ yếu lên trên, lchậc nhẹ theo X và Z
        float rx = Random.Range(-0.4f, 0.4f);
        float rz = Random.Range(-0.4f, 0.4f);
        moveDirection = new Vector3(rx, 1f, rz).normalized;

        timer = lifeTime;
        isActive = true;
    }

    private void Update()
    {
        if (!isActive) return; // chưa Show() thì không làm gì

        if (timer > 0f)
        {
            // Bay theo hướng ngẫu nhiên
            transform.position += moveDirection * floatSpeed * Time.deltaTime;

            // Xoay song song màn hình (giống HealthBarUI)
            if (Camera.main != null)
            {
                var camRot = Camera.main.transform.rotation;
                transform.LookAt(transform.position + camRot * Vector3.forward, camRot * Vector3.up);
            }

            // Fade out nửa đời sau
            if (timer < lifeTime / 2f)
            {
                Color c = textMesh.color;
                c.a = timer / (lifeTime / 2f);
                textMesh.color = c;
            }

            timer -= Time.deltaTime;
        }
        else
        {
            isActive = false;
            ObjectPool.Instance.Despawn(gameObject, PoolType.DamageText);
        }
    }
}
