using UnityEngine;

public class MirrorPosition : MonoBehaviour
{
    public GameObject objectToMirror;


    public Vector3 positionOffset = Vector3.zero;

    private Transform _original;
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _original = objectToMirror.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = _original.position + positionOffset;
    }
}
