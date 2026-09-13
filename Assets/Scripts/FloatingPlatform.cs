using UnityEngine;

public class FloatingPlatform : MonoBehaviour
{
    public Transform platform; // ตัวแท่นจริง

    public float moveHeight = 3f;
    public float speed = 2f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isActivated = false;

    bool HasEnemy()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length > 0;
    }

    void Start()
    {
        startPos = platform.position;
        targetPos = startPos + new Vector3(0, moveHeight, 0);
    }

    void Update()
    {
        if (isActivated)
        {
            platform.position = Vector3.Lerp(platform.position, targetPos, Time.deltaTime * speed);
        }
        else
        {
            platform.position = Vector3.Lerp(platform.position, startPos, Time.deltaTime * speed);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (HasEnemy())
            {
                Debug.Log("ยังมีศัตรูอยู่! ใช้ลิฟต์ไม่ได้");
                return;
            }

            isActivated = true;
            other.transform.SetParent(platform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isActivated = false;
        }
    }
}