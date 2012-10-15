using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour {

	// Use this for initialization
	void Start () {
        ComputingThread ct = new ComputingThread(3);
        ct.StartThreads();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
