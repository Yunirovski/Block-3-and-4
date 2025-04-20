using UnityEngine;

public abstract class BaseItem : ScriptableObject
{
    public string itemName;  // ��Ʒ���ƣ�������ʾ���ʶ

    public virtual void OnSelect(GameObject model) { }   // ��Ʒ��ѡ��ʱ����
    public virtual void OnDeselect() { }                 // ��Ʒ��ȡ��ѡ��ʱ����
    public virtual void OnReady() { }                    // ��Ʒ׼��ʹ��ʱ����
    public virtual void OnUnready() { }                  // ȡ��׼��ʹ��ʱ����
    public abstract void OnUse();                        // ʹ����Ʒʱ���ã���׼��״̬�����ʱ��
}
