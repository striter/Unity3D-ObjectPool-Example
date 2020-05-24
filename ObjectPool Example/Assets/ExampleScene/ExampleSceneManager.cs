using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum enum_Drops
{
    Invalid = -1,
    Cube = 1,
    Spehre = 2,
    Cyliner = 3,
}
public class ExampleSceneManager : MonoBehaviour {
    Transform m_DropsParent;
    ObjectPoolListMonobehaviour<int, ShotsBase> m_ShotsPool;
    int m_ShotCount = 0;
    float m_DropCheck;
    float m_ShotCheck;
    [Range(.1f,2f)]
    public float m_DropDuration=.2f;
    [Range(.1f,2f)]
    public float m_ShotDuration=.3f;
    void Awake()
    {
        ObjectPoolManager.Init();
        foreach(DropsBase drops in Resources.LoadAll<DropsBase>(""))
        {
            ObjectPoolManager<enum_Drops, DropsBase>.Register((enum_Drops)(int.Parse(drops.name)),drops,3);
        }
        m_DropsParent = transform.Find("Drops");
        m_ShotsPool = new ObjectPoolListMonobehaviour<int, ShotsBase>(transform.Find("ShotsPool"), "ShotsItem");
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            m_ShotsPool.Clear();
            ObjectPoolManager<enum_Drops, DropsBase>.RecycleAll();
        }
        m_ShotCheck -= Time.deltaTime;
        if (Input.GetKey(KeyCode.Mouse0))
        {
            if(m_ShotCheck < 0)
            {
                int shotIdentity = m_ShotCount++;
                ShotsBase shot = m_ShotsPool.Spawn(shotIdentity);
                shot.transform.position = Camera.main.transform.position + Vector3.down;
                shot.Play(shotIdentity, Camera.main.ScreenPointToRay(Input.mousePosition).direction * 3000f, m_ShotsPool.Recycle);
                m_ShotCheck = m_ShotDuration;
            }
        }

        m_DropCheck -= Time.deltaTime;
        if(m_DropCheck<0)
        {
            DropsBase drop=null;
            switch(UnityEngine. Random.Range(0,3))
            {
                case 0: drop=ObjectPoolManager<enum_Drops, DropsBase>.Spawn(enum_Drops.Cube, m_DropsParent, m_DropsParent.position, m_DropsParent.rotation); break;
                case 1: drop=ObjectPoolManager<enum_Drops, DropsBase>.Spawn(enum_Drops.Spehre, m_DropsParent, m_DropsParent.position, m_DropsParent.rotation); break;
                case 2: drop= ObjectPoolManager<enum_Drops, DropsBase>.Spawn(enum_Drops.Cyliner, m_DropsParent, m_DropsParent.position, m_DropsParent.rotation); break;
            }
            drop.transform.position = drop.transform.position + Vector3.right * UnityEngine.Random.Range(-5f, 5f);
            m_DropCheck = m_DropDuration;
        }

    }


}
