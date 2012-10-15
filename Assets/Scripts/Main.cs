using UnityEngine;
using System.Collections;
using System.Threading;

public class Main : MonoBehaviour {

	// Use this for initialization
	void Start () {
        ComputingThread ct = new ComputingThread(3);
        ct.StartThreads();
        Debug.Log("MAIN Started");

        //ciklus eleje
        ComputingThread.startReading.Reset();
        ComputingThread.startWriting.Reset();

        WaitHandle.WaitAll(ComputingThread.readAndComputeReady);
        Debug.Log("MAIN Waited all readAndCompute");

        ComputingThread.startWriting.Set();
        Debug.Log("MAIN Start writing");

        WaitHandle.WaitAll(ComputingThread.writeReady);
        Debug.Log("MAIN waited all write");

        //ide kell majd az a kód, ami átállítja a GameObject-eket

        ComputingThread.startReading.Set();
        Debug.Log("MAIN Start reading");
        // ciklus vége
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
