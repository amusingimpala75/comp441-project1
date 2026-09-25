using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BlockScript : MonoBehaviour
{
    public MainSceneManagerScript manager = null;
    public int column = 0;
    private bool falling = true;
    private Rigidbody2D _rbody = null;

    private void Start()
    {
        if (manager == null)
        {
            manager = FindFirstObjectByType<MainSceneManagerScript>();
        }
        _rbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (falling)
        {
            _rbody.linearVelocityY = -1 * manager.gameSpeed;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (falling) {
            StopFalling();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (falling) {
            // Hit bottom of column without any blocks
            StopFalling();
        }
    }

    private void StopFalling()
    {
        Debug.Log("destroying block");
        falling = false;
        _rbody.bodyType = RigidbodyType2D.Static;
        manager.ColumnAddBlock(gameObject, column);
    }
}
