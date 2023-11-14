using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomsMaze : MonoBehaviour
{
    //singleton
    public static RoomsMaze RoomsInstance { get; private set; }

    private void Awake()
    {
        if (RoomsInstance == null)
            RoomsInstance = this;
        else
            Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
