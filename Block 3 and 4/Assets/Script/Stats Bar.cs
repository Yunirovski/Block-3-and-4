using UnityEngine;
using UnityEngine.UI;



[RequireComponent(typeof(Image))]
public class StatsBar : MonoBehaviour
{
    private Image statsBar;
    [SerializeField] private Gradient colors;

    void Start()
    {
        statsBar = GetComponent<Image>();
        //Test one two three
    }

    public void UpdateBar(float width)
    {
        //make sure that the image's 
        //width is changed to match the incoming variable
        statsBar.fillAmount = width;
        statsBar.material.color = colors.Evaluate(width);
    }
}
