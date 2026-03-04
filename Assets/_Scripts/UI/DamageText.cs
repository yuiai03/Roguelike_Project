using UnityEngine;
using TMPro;

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
    private Vector3 moveDirection; 

    private void Awake()
    {

        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>();
    }

    private void OnEnable()
    {
        isActive = false; 
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

        Color c = textMesh.color;
        c.a = 1f;
        textMesh.color = c;

        float rx = Random.Range(-0.4f, 0.4f);
        float rz = Random.Range(-0.4f, 0.4f);
        moveDirection = new Vector3(rx, 1f, rz).normalized;

        timer = lifeTime;
        isActive = true;
    }

    private void Update()
    {
        if (!isActive) return; 

        if (timer > 0f)
        {

            transform.position += moveDirection * floatSpeed * Time.deltaTime;

            if (Camera.main != null)
            {
                var camRot = Camera.main.transform.rotation;
                transform.LookAt(transform.position + camRot * Vector3.forward, camRot * Vector3.up);
            }

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
