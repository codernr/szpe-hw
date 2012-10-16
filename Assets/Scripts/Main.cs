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

        WaitHandle.WaitAll(ComputingThread.readAndComputeReady);
        Debug.Log("MAIN Waited all readAndCompute");
        //itt reseteljük a readinget, amit az elõzõ ciklus végén seteltünk, mert itt az összes háttér szál várakozik
        ComputingThread.startReading.Reset();

        ComputingThread.startWriting.Set();
        Debug.Log("MAIN Start writing");

        WaitHandle.WaitAll(ComputingThread.writeReady);
        Debug.Log("MAIN waited all write");
        // itt kell megcsinálni a resetet, mivel itt az összes többi szál várakozik
        ComputingThread.startWriting.Reset();

        //ide kell majd az a kód, ami átállítja a GameObject-eket

        ComputingThread.startReading.Set();
        Debug.Log("MAIN Start reading");
        // ciklus vége
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
