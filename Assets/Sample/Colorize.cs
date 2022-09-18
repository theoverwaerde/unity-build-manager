using UnityEngine;
using UnityEngine.UI;

public class Colorize : MonoBehaviour
{
    private void Start()
    {
        Image image = GetComponent<Image>();
        
        #if RED
        Color c = Color.red;
        #elif BLUE
        Color c = Color.blue;
        #else
        Color c = Color.magenta;
        #endif
        
        image.color = c;
    }
}
