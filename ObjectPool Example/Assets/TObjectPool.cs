using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#region Static Pool
public interface IObjectPoolStaticBase<T>{
    void OnPoolInit(T identity,Action<T,MonoBehaviour> OnRecycle);
    void OnPoolSpawn();
    void OnPoolRecycle();
}
public class CObjectPoolStaticPrefabBase<T> :MonoBehaviour,IObjectPoolStaticBase<T>
{
    public bool m_PoolItemInited { get; private set; }
    public T m_Identity { get; private set; }
    private Action<T,MonoBehaviour> OnSelfRecycle;
    public virtual void OnPoolInit(T _identity,Action<T,MonoBehaviour> _OnSelfRecycle)
    {
        m_Identity = _identity;
        m_PoolItemInited = true;
        OnSelfRecycle = _OnSelfRecycle;
    }
    public void DoRecycle() =>  OnSelfRecycle?.Invoke(m_Identity, this);

    public virtual void OnPoolSpawn() {  }
    public virtual void OnPoolRecycle() { }
}
public class ObjectPoolManager
{
    protected static Transform tf_PoolSpawn { get; private set; } = null;
    public static void Init()
    {
        tf_PoolSpawn= new GameObject("PoolSpawn").transform;
    }
}
public class ObjectPoolManager<T, Y> : ObjectPoolManager where Y : MonoBehaviour, IObjectPoolStaticBase<T>
{
    class ItemPoolInfo
    {
        public Y m_spawnItem;
        public Queue<Y> m_DeactiveQueue = new Queue<Y>();
        public List<Y> m_ActiveList = new List<Y>();

        public Y NewItem(T identity, Action<T, MonoBehaviour> OnRecycle)
        {
            Y item = GameObject.Instantiate(m_spawnItem, tf_PoolSpawn); 
            item.gameObject.SetActive(false);
            item.name = m_spawnItem.name + "_" + (m_DeactiveQueue.Count + m_ActiveList.Count).ToString();
            item.OnPoolInit(identity, OnRecycle);
            return item;
        }

        public void Destroy()
        {
            for (; m_DeactiveQueue.Count > 0;)
                GameObject.Destroy(m_DeactiveQueue.Dequeue().gameObject);
            for (int i = 0; i < m_ActiveList.Count; i++)
                GameObject.Destroy(m_ActiveList[i].gameObject);

            m_DeactiveQueue.Clear();
            m_ActiveList.Clear();
        }
    }

    static Dictionary<T, ItemPoolInfo> d_ItemInfos = new Dictionary<T, ItemPoolInfo>();
    public static bool Registed(T identity)=>d_ItemInfos.ContainsKey(identity);
    public static List<T> GetRegistedList() => d_ItemInfos.Keys.ToList();
    public static Y GetRegistedSpawnItem(T identity)
    {
        if (!Registed(identity))
            Debug.LogError("Identity:" + identity + "Unregisted");
        return d_ItemInfos[identity].m_spawnItem;
    }
    public static void Register(T identity, Y registerItem, int poolStartAmount)
    {
        if (d_ItemInfos.ContainsKey(identity))
        {
            Debug.LogError("Same Element Already Registed:" + identity.ToString() + "/" + registerItem.gameObject.name);
            return;
        }
        d_ItemInfos.Add(identity, new ItemPoolInfo());
        ItemPoolInfo info = d_ItemInfos[identity];
        info.m_spawnItem = registerItem;
        for (int i = 0; i < poolStartAmount; i++)
            info.m_DeactiveQueue.Enqueue(info.NewItem(identity, SelfRecycle));
    }
    public static Y Spawn(T identity, Transform toTrans, Vector3 toPos, Quaternion rot)
    {
        if (!d_ItemInfos.ContainsKey(identity))
        {
            Debug.LogError("PoolManager:" + typeof(T).ToString() + "," + typeof(Y).ToString() + " Error! Null Identity:" + identity + "Registed");
            return null;
        }
        ItemPoolInfo info = d_ItemInfos[identity];
        Y item;
        if (info.m_DeactiveQueue.Count > 0)
            item = info.m_DeactiveQueue.Dequeue();
        else
            item = info.NewItem(identity, SelfRecycle);

        info.m_ActiveList.Add(item);
        item.OnPoolSpawn();
        item.transform.SetParent(toTrans == null ? tf_PoolSpawn : toTrans);
        item.transform.position = toPos;
        item.transform.rotation = rot;
        item.gameObject.SetActive(true);
        return item;
    }
    static void SelfRecycle(T identity, MonoBehaviour obj) => Recycle(identity, obj as Y);
    public static void Recycle(T identity, Y obj)
    {
        if (!d_ItemInfos.ContainsKey(identity))
        {
            Debug.LogWarning("Null Identity Of GameObject:" + obj.name + "/" + identity + " Registed(" + typeof(T).ToString() + "|" + typeof(Y).ToString() + ")");
            return;
        }
        ItemPoolInfo info = d_ItemInfos[identity];
        info.m_ActiveList.Remove(obj);
        obj.OnPoolRecycle();
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(tf_PoolSpawn);
        info.m_DeactiveQueue.Enqueue(obj);
    }

    public static void RecycleAll(T identity)
    {
        ItemPoolInfo info = d_ItemInfos[identity];
        List<Y> activeItems = new List<Y>(info.m_ActiveList);
        foreach(var item in activeItems)
        {
            Recycle(identity, item);
        }
    }
    public static void RecycleAll()
    {
        Dictionary<T, ItemPoolInfo> infos = new Dictionary<T, ItemPoolInfo>(d_ItemInfos);
        foreach(var poolIdentity in infos.Keys)
        {
            RecycleAll(poolIdentity);
        }
    }
}

#endregion

#region Class Pool

public class ObjectPoolListBase<T, Y> 
{
    public Transform transform { get; private set; }
    protected GameObject m_PoolItem;
    public Dictionary<T, Y> m_ActiveItemDic { get; private set; } = new Dictionary<T, Y>();
    public List<Y> m_InactiveItemList { get; private set; } = new List<Y>();
    public int Count => m_ActiveItemDic.Count;
    public ObjectPoolListBase(Transform poolTrans, string itemName)
    {
        transform = poolTrans;
        m_PoolItem = poolTrans.Find(itemName).gameObject;
        m_PoolItem.gameObject.SetActive(false);
    }
    public Y GetOrSpawnItem(T identity)
    {
        if (ContainsItem(identity))
            return Contains(identity);
        return Spawn(identity);
    }
    public bool ContainsItem(T identity) => m_ActiveItemDic.ContainsKey(identity);
    public Y Contains(T identity) => m_ActiveItemDic[identity];

    public virtual Y Spawn(T identity)
    {
        Y targetItem;
        if (m_InactiveItemList.Count > 0)
        {
            targetItem = m_InactiveItemList[0];
            m_InactiveItemList.Remove(targetItem);
        }
        else
        {
            targetItem = CreateInstance(UnityEngine.Object.Instantiate(m_PoolItem, transform).transform);
        }
        if (m_ActiveItemDic.ContainsKey(identity)) Debug.LogError(identity + "Already Exists In Grid Dic");
        else m_ActiveItemDic.Add(identity, targetItem);
        Transform trans = GetInstanceTransform(targetItem);
        trans.name = identity.ToString();
        trans.gameObject.SetActive(true);
        return targetItem;
    }

    public virtual void Recycle(T identity)
    {
        Y item = m_ActiveItemDic[identity];
        m_InactiveItemList.Add(item);
        GetInstanceTransform(item).gameObject.SetActive(false);
        m_ActiveItemDic.Remove(identity);
    }

    public void Sort(Comparison<KeyValuePair<T,Y>> Compare)
    {
        List<KeyValuePair<T, Y>> list = m_ActiveItemDic.ToList();
        list.Sort(Compare);
        m_ActiveItemDic.Clear();
        foreach(var pair in list)
        {
            GetInstanceTransform(pair.Value).SetAsLastSibling();
            m_ActiveItemDic.Add(pair.Key, pair.Value);
        }
    }

    public void Clear()
    {
        Dictionary<T, Y> itemDic = new Dictionary<T, Y>(m_ActiveItemDic);
        foreach(var key in itemDic.Keys)
        {
            Recycle(key);
        }
    } 

    protected virtual Y CreateInstance(Transform instantiateTrans)
    {
        Debug.LogError("Override This Please");
        return default(Y);
    }
    protected virtual Transform GetInstanceTransform(Y targetItem)
    {
        Debug.LogError("Override This Please");
        return null;
    }
}
#region Component
public class ObjectPoolListComponent<T, Y> : ObjectPoolListBase<T, Y> where Y : Component
{
    public ObjectPoolListComponent(Transform poolTrans, string itemName) : base(poolTrans, itemName)
    {
    }
    protected override Y CreateInstance(Transform instantiateTrans)=>instantiateTrans.GetComponent<Y>();
    protected override Transform GetInstanceTransform(Y targetItem) => targetItem.transform;
}
#endregion
#region Class
public interface IObjectPoolItemBase<T>
{
    void OnInitItem();
    void OnAddItem(T identity);
    void OnRemoveItem();
    Transform GetTransform();
}

public class ObjectPoolListItem<T,Y>:ObjectPoolListBase<T,Y> where Y:IObjectPoolItemBase<T>
{
    public ObjectPoolListItem(Transform poolTrans, string itemName) : base(poolTrans, itemName) { }
    public override Y Spawn(T identity)
    {
        Y item = base.Spawn(identity);
        item.OnAddItem(identity);
        return item;
    }

    public override void Recycle(T identity)
    {
        Contains(identity).OnRemoveItem();
        base.Recycle(identity);
    }
}

public class CObjectPoolClass<T>:IObjectPoolItemBase<T>
{
    public Transform transform { get; private set; }
    public T m_Identity { get; private set; }
    public Transform GetPoolItemTransform() => transform;
    public CObjectPoolClass(Transform _transform)
    {
        transform = _transform;
    }


    public virtual void OnInitItem()
    {
    }

    public virtual void OnAddItem(T identity)
    {
        m_Identity = identity;
    }

    public virtual void OnRemoveItem()
    {
    }

    public Transform GetTransform() => transform;
}


public class ObjectPoolListClass<T, Y> : ObjectPoolListItem<T, Y> where Y : CObjectPoolClass<T>
{
    public ObjectPoolListClass(Transform poolTrans, string itemName) : base(poolTrans, itemName) { }

    static readonly Type type = typeof(Y);
    protected override Y CreateInstance(Transform instantiateTrans)
    {
        Y item = Activator.CreateInstance(type, instantiateTrans) as Y;
        item.OnInitItem();
        return item;
    } 
    protected override Transform GetInstanceTransform(Y targetItem) => targetItem.transform;

}
#endregion
#region Monobehaviour
public class CObjectPoolMono<T> : MonoBehaviour, IObjectPoolItemBase<T>
{
    public T m_Identity { get; private set; }
    public Transform GetTransform() => transform;

    public virtual void OnInitItem()
    {
    }

    public virtual void OnAddItem(T identity)
    {
        m_Identity = identity;
    }
    public virtual void OnRemoveItem()
    {
    }
}

public class ObjectPoolListMonobehaviour<T, Y> : ObjectPoolListItem<T, Y> where Y : CObjectPoolMono<T>
{
    public ObjectPoolListMonobehaviour(Transform poolTrans, string itemName) : base(poolTrans, itemName)
    {
    }
    protected override Y CreateInstance(Transform instantiateTrans)
    {
        Y item = instantiateTrans.GetComponent<Y>();
        item.OnInitItem();
        return item;
    }
    protected override Transform GetInstanceTransform(Y targetItem) => targetItem.transform;
}
#endregion
#endregion
