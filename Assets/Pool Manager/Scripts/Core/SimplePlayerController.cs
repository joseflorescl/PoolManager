using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [SerializeField] private int firesPerSecond = 20;
    [SerializeField] private float speed;
    [SerializeField] private BulletController bulletControllerPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10;

    float fireRate;
    float nextFire;

    bool GetFireButtonDown => Input.GetKeyDown(KeyCode.LeftControl)
        || Input.GetKeyDown(KeyCode.RightControl)
        || Input.GetButtonDown("Fire1");

    private void Start()
    {
        fireRate = 1f / firesPerSecond;
        nextFire = 0f;
    }

    private void Update()
    {
        if (GetFireButtonDown && Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;
            FireBullet();
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        UpdateMovement(horizontal, vertical);
    }

    private void FireBullet()
    {
        //BulletController bullet = Instantiate(bulletControllerPrefab, firePoint.position, firePoint.rotation);
        BulletController bullet = PoolManager.Instance.Get(bulletControllerPrefab, firePoint.position, firePoint.rotation);
        bullet.SetKinematicVelocity(Vector2.up, bulletSpeed);
    }

    void UpdateMovement(float horizontal, float vertical)
    {
        if (horizontal == 0 && vertical == 0) return;

        Vector2 movement = new Vector2(horizontal, vertical);        
        movement.Normalize();
        movement *= speed;        
        Move(movement);
        
    }

    private void Move(Vector3 movement)
    {
        transform.Translate(Time.deltaTime * movement);
    }
}
