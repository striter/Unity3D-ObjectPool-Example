using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropsBase : CObjectPoolStaticPrefabBase<enum_Drops>
{
    Rigidbody m_Rigidbody;
    float m_RecycleTimer;
    public override void OnPoolInit(enum_Drops _identity, Action<enum_Drops, MonoBehaviour> _OnSelfRecycle)
    {
        base.OnPoolInit(_identity, _OnSelfRecycle);
        m_Rigidbody = GetComponent<Rigidbody>();
    }
    public override void OnPoolSpawn()
    {
        base.OnPoolSpawn();
        m_Rigidbody.velocity = Vector3.zero;
        m_RecycleTimer = 3f;
    }
    void Update()
    {
        m_RecycleTimer -= Time.deltaTime;
        if (m_RecycleTimer < 0)
            DoRecycle();
    }
}
