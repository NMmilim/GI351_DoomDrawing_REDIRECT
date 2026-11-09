using UnityEngine;

public class PlayerMovement : Player
{
    public GameObject bullet;
    public Rigidbody2D rb;

    public void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Update()
    {
        if (Time.timeScale == 0f) return;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        Vector3 direction = (mousePosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    public void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;

        PlayerManagement pm = PlayerManagement.Instance;
        if (pm == null || pm.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");

        // speed = base + permanent + in-run upgrades; speedMultiplier = temporary slow (railgun charge)
        rb.linearVelocity = new Vector2(horizontal, vertical) * (pm.speed * pm.speedMultiplier);
    }
}

