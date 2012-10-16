using UnityEngine;
using System.Collections;
using System.Threading;

public class ComputingThread {

    public int[, ,] values;

    private int size;

    private object lockObject = new Object();

    public Thread[] threads;

    //thread-ek jelz� eventjei
    public AutoResetEvent[] readAndComputeReady;
    public AutoResetEvent[] writeReady;

    //azok az eventek, amikre a thread-ek v�rnak
    public ManualResetEvent startReading = new ManualResetEvent(false);
    public ManualResetEvent startWriting = new ManualResetEvent(false);

    // szab�lyok az itt le�rtak szerint: http://www.ibiblio.org/e-notes/Life/Game.htm
    // "a new ball will appear if the number of neighbors (Sum) is equal or more than r1 and equal or less than r2. 
    // a ball will die if the Sum is more than r3 or less than r4."
    //   if ( (L[p]==0)&&(Sum[i][j][k]>=r1)&&(Sum[i][j][k]<=r2) ) L[p]=1;
    //   else if ( (L[p]!=0)&&((Sum[i][j][k]>r3)||(Sum[i][j][k]<r4)) ) L[p]=0;
    private int[] rules = new int[4] { 5, 5, 7, 7 };

    public ComputingThread(int size)
    {
        this.values = this.GenerateRandomValues(size);

        this.size = size;

        int total = size * size * size;
        this.threads = new Thread[total];
        this.readAndComputeReady = new AutoResetEvent[total];
        this.writeReady = new AutoResetEvent[total];

        for (int i = 0; i < total; i++)
        {
            this.threads[i] = new Thread(this.ComputeGeneration);
            this.readAndComputeReady[i] = new AutoResetEvent(false);
            this.writeReady[i] = new AutoResetEvent(false);
        }
    }

    public void StartThreads()
    {
        for (int i = 0; i < threads.Length; i++)
        {
            Debug.Log("Starting " + i + ".thread");
            threads[i].Start(i);
        }
    }

    public void ComputeGeneration(object num)
    {
        int index = (int)num;
        // olvas�si �s sz�mol�si feladatok

        // visszafejtj�k a sorsz�mb�l a 3D vektort
        int i = index / (this.size * this.size);
        int j = index / this.size;
        int k = index % this.size;

        Debug.Log(index + ". thread: [" + i + "," + j + "," + k + "] kocka");

        // ide majd a gener�ci�k sz�ma j�n, egyel�re hardcode
        Debug.Log("#" + index + "-1 debug");
            int actualValue = this.values[i, j, k];
            int sum = this.Sum(i, j, k);
            Debug.Log("#" + index + "-2 debug");
            if (actualValue == 0)
            {
                if (sum >= this.rules[0] && sum <= this.rules[1]) actualValue = 1;
            }
            else
            {
                if (sum >= this.rules[2] || sum <= this.rules[3]) actualValue = 0;
            }
            Debug.Log("#" + index + "-3 debug");
            //Debug.Log(">>>" + index + ". thread val:" + this.values[i, j, k]);
            //Debug.Log(">>>" + index + ". thread nextval:" + actualValue);

            
            //readAndComputeReady[index].Set();

            //startWriting.WaitOne();
            //// itt kezd�dik az �r�si szakasz
            //Debug.Log(">>>"+index + ". thread nextval:" + actualValue);

            //this.values[i, j, k] = actualValue;

            //writeReady[index].Set();

            //startReading.WaitOne();
        
    }

    // visszaad egy v�letlen 1/0 �rt�kekkel felt�lt�tt 3D int t�mb�t
    private int[, ,] GenerateRandomValues(int size)
    {
        int[, ,] ret = new int[size, size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    ret[i, j, k] = Random.Range(0, 2);
                }
            }
        }

        return ret;
    }

    // Sum: egy t�rusz fel�p�t�st val�s�t meg, megadja az (i,j,k) koordin�t�kra,
    // hogy a szomsz�dos 26 kock�b�l h�nyban van egyes
    private int Sum(int i, int j, int k)
    {
        int sum = 0;

        for (int x = i - 1; x < i + 2; x++)
        {
            for (int y = j - 1; y < j + 2; y++)
            {
                for (int z = k - 1; z < k + 2; z++)
                {
                    // a k�z�ps� elemet nem sz�moljuk bele
                    if (x == i && y == j && z == k) continue;

                    // itt j�n l�tre a (4D) t�rusz, ha pl. 10 elem� a dimenzi�,
                    // a 9-es indexre 9-et kapunk, de a 10-re m�r 0-t
                    sum += this.values[this.Mod(x, this.size), this.Mod(y, this.size), this.Mod(z, this.size)];
                }
            }
        }

        return sum;
    }

    // x mod-dal val� oszt�si marad�k�t adja meg
    // nem egyezik a m�k�d�s a remainder (%) oper�torral, mert az negat�vra nem m�k�dik
    // csak a Sum egyszer�s�t�s�hez kell
    private int Mod(int x, int mod)
    {
        return (mod + x) % mod;
    }
}
