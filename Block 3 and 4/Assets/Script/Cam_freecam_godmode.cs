using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private float Speed;
    // Update is called once per frame
    void Update()
    {
        float forwards = Input.GetAxis("Vertical");
        float sidewards = Input.GetAxis("Horizontal");

        Debug.Log($"{forwards}, {sidewards}");

        transform.position += transform.forward * forwards * Time.deltaTime * Speed;
        transform.position += transform.right * sidewards * Time.deltaTime * Speed;

        transform.Rotate(0, Input.GetAxis("Mouse X"), 0);

        if (Input.GetKey(KeyCode.E))
            transform.position += transform.up * Time.deltaTime * Speed;

        if (Input.GetKey(KeyCode.Q))
            transform.position -= transform.up * Time.deltaTime * Speed;
    }
}
