using UnityEngine;
using System.Collections;

public class CameraAnimation : MonoBehaviour {

    private Main main;
    public float rotationSpeed = 10;

    public void Start()
    {
        this.main = this.GetComponent<Main>();
    }

    public void Update()
    {
        if (this.main.state["running"])
        {
            this.transform.RotateAround(Vector3.zero, Vector3.up, Time.deltaTime * this.rotationSpeed);
        }
    }
}
