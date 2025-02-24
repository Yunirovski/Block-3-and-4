using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    private float hp;
    [SerializeField] private int maxHP;
    public UnityEvent<float> hpUpdated;


    void Start()
    {
        hp = maxHP;
        hpUpdated.Invoke(hp / maxHP);
    }
    private void OnMouseDown()
    {
        Dodamage(10);
    }
    public void Dodamage(int amount)
    {
        hp -= amount;
        Debug.Log(hp);
        hpUpdated.Invoke(hp / maxHP);
    }
}