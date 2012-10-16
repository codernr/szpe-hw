using UnityEngine;
using System.Collections;
using System.Threading;

public class ComputingThread {

    public int[, ,] values;

    private int size;

    private object lockObject = new Object();

    public Thread[] threads;

    //thread-ek jelzõ eventjei
    public AutoResetEvent[] readAndComputeReady;
    public AutoResetEvent[] writeReady;

    //azok az eventek, amikre a thread-ek várnak
    public ManualResetEvent startReading = new ManualResetEvent(false);
    public ManualResetEvent startWriting = new ManualResetEvent(false);

    // szabályok az itt leírtak szerint: http://www.ibiblio.org/e-notes/Life/Game.htm
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
        // olvasási és számolási feladatok

        // visszafejtjük a sorszámból a 3D vektort
        int i = index / (this.size * this.size);
        int j = index / this.size;
        int k = index % this.size;

        Debug.Log(index + ". thread: [" + i + "," + j + "," + k + "] kocka");

        // ide majd a generációk száma jön, egyelõre hardcode
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
            //// itt kezdõdik az írási szakasz
            //Debug.Log(">>>"+index + ". thread nextval:" + actualValue);

            //this.values[i, j, k] = actualValue;

            //writeReady[index].Set();

            //startReading.WaitOne();
        
    }

    // visszaad egy véletlen 1/0 értékekkel feltöltött 3D int tömböt
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

    // Sum: egy tórusz felépítést valósít meg, megadja az (i,j,k) koordinátákra,
    // hogy a szomszédos 26 kockából hányban van egyes
    private int Sum(int i, int j, int k)
    {
        int sum = 0;

        for (int x = i - 1; x < i + 2; x++)
        {
            for (int y = j - 1; y < j + 2; y++)
            {
                for (int z = k - 1; z < k + 2; z++)
                {
                    // a középsõ elemet nem számoljuk bele
                    if (x == i && y == j && z == k) continue;

                    // itt jön létre a (4D) tórusz, ha pl. 10 elemû a dimenzió,
                    // a 9-es indexre 9-et kapunk, de a 10-re már 0-t
                    sum += this.values[this.Mod(x, this.size), this.Mod(y, this.size), this.Mod(z, this.size)];
                }
            }
        }

        return sum;
    }

    // x mod-dal való osztási maradékát adja meg
    // nem egyezik a mûködés a remainder (%) operátorral, mert az negatívra nem mûködik
    // csak a Sum egyszerûsítéséhez kell
    private int Mod(int x, int mod)
    {
        return (mod + x) % mod;
    }
}
