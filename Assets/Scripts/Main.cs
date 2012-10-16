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
        //itt resetelj�k a readinget, amit az el�z� ciklus v�g�n setelt�nk, mert itt az �sszes h�tt�r sz�l v�rakozik
        ComputingThread.startReading.Reset();

        ComputingThread.startWriting.Set();
        Debug.Log("MAIN Start writing");

        WaitHandle.WaitAll(ComputingThread.writeReady);
        Debug.Log("MAIN waited all write");
        // itt kell megcsin�lni a resetet, mivel itt az �sszes t�bbi sz�l v�rakozik
        ComputingThread.startWriting.Reset();

        //ide kell majd az a k�d, ami �t�ll�tja a GameObject-eket

        ComputingThread.startReading.Set();
        Debug.Log("MAIN Start reading");
        // ciklus v�ge
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
