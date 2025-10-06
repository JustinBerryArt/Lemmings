using UnityEngine;
using Lemmings;


public class KeyboardController : MonoBehaviour
{
    public BeBodBopMachine instrument;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }

        if (Input.GetKey("space"))
        {
            instrument.SwapSilence();
        }
    }
}
