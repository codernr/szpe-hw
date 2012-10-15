using UnityEngine;
using System.Collections;
using System.Threading;

public class ComputingThread {

    public static int[, ,] values;

    public static Thread[] threads;

    //thread-ek jelzõ eventjei
    public static AutoResetEvent[] readAndComputeReady;
    public static AutoResetEvent[] writeReady;

    //azok az eventek, amikre a thread-ek várnak
    public static AutoResetEvent startReading = new AutoResetEvent(false);
    public static AutoResetEvent statWriting = new AutoResetEvent(false);

    public ComputingThread(int size)
    {
        values = new int[size, size, size];

        //teszt
        int total = size * size * size;
        threads = new Thread[total];
        readAndComputeReady = new AutoResetEvent[total];
        writeReady = new AutoResetEvent[total];

        for (int i = 0; i < total; i++)
        {
            threads[i] = new Thread(this.ComputeGeneration);
        }
    }

    public void StartThreads()
    {
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i].Start(i);
        }
    }

    public void ComputeGeneration(object num)
    {
        Debug.Log((int)num + ". thread started");
    }
}
