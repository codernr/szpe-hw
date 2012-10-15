using UnityEngine;
using System.Collections;
using System.Threading;

public class ComputingThread {

    public static int[, ,] values;

    public static Thread[] threads;

    //thread-ek jelz� eventjei
    public static AutoResetEvent[] readAndComputeReady;
    public static AutoResetEvent[] writeReady;

    //azok az eventek, amikre a thread-ek v�rnak
    public static ManualResetEvent startReading = new ManualResetEvent(false);
    public static ManualResetEvent startWriting = new ManualResetEvent(false);

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
            readAndComputeReady[i] = new AutoResetEvent(false);
            writeReady[i] = new AutoResetEvent(false);
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
        int index = (int)num;
        // olvas�si �s sz�mol�si feladatok
        Debug.Log(index + ". thread started");

        readAndComputeReady[index].Set();

        Debug.Log(index + ". thread set its read event");

        startWriting.WaitOne();
        // itt kezd�dik az �r�si szakasz

        Debug.Log(index + ". thread starts to write");

        writeReady[index].Set();
        Debug.Log(index + ". thread write end");

        startReading.WaitOne();
        Debug.Log(index + ".thread terminates");
    }
}
