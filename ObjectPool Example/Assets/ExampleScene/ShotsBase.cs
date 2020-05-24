using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotsBase : CObjectPoolMono<int> {
    int m_Idenity;
    Action<int> DoRecycle;
    float m_DurationCheck;
    Rigidbody m_Rigidbody;
    public override void OnInitItem()
    {
        base.OnInitItem();
        m_Rigidbody = GetComponent<Rigidbody>();

    }
    public void Play(int identity,Vector3 strengthenDirection,Action<int> DoRecycle)
    {
        m_Idenity = identity;
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.AddForce(strengthenDirection);
        this.DoRecycle = DoRecycle;
        m_DurationCheck = 2f;
    }

    void Update()
    {
        if (m_DurationCheck < 0)
            return;
        m_DurationCheck -= Time.deltaTime;
        if (m_DurationCheck < 0)
            DoRecycle(m_Idenity);
    }
}
