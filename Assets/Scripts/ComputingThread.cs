using UnityEngine;
using System.Collections;
using System.Threading;

public class ComputingThread {

    // ebben tároljuk a tényleges tömböt, amiben az 1-0-k vannak
    public int[, ,] values;

    private int size;

    private object lockObject = new Object();

    // ez dönti el, hogy kell-e a szálaknak futnia még
    public bool terminated = false;

    // a szálakat tároló tömb
    public Thread[] threads;
    public int threadCount = 4;

    // az a tömb, ami azt tárolja, hogy melyik szálnak milyen adattartománnyal kell foglalkoznia
    // a values tömb linearizált indextartományát és offsetjét tárolja thread-enként
    // [threadCount, 2] forma: azt tárolja, hogy melyik thread hol kezdje és meddig az adatok feldolgozását
    // pl. decomposedValues[0] = {0, 100} : a 0-dik thread a values tömb elsõ 100 elemével foglalkozik
    private int[,] decomposedValues;

    //thread-ek jelzõ eventjei
    public AutoResetEvent[] readAndComputeReady;
    public AutoResetEvent[] writeReady;

    //azok az eventek, amikre a thread-ek várnak
    public ManualResetEvent startWriting = new ManualResetEvent(false);
    public ManualResetEvent startGeneration = new ManualResetEvent(false);

    // flag, amit a unity-s szál folyamatosan pollingol, hogy kész-e a számítás
    private bool _threadComputeReady;
    public bool threadComputeReady
    {
        get {
            bool ret;
            lock (lockObject) {
                ret = _threadComputeReady;
            }
            return ret;
        }

        set {
            lock (lockObject) {
                _threadComputeReady = value;
            }
        }
    }

    // szabályok az itt leírtak szerint: http://www.ibiblio.org/e-notes/Life/Game.htm
    // "a new ball will appear if the number of neighbors (Sum) is equal or more than r1 and equal or less than r2. 
    // a ball will die if the Sum is more than r3 or less than r4."
    //   if ( (L[p]==0)&&(Sum[i][j][k]>=r1)&&(Sum[i][j][k]<=r2) ) L[p]=1;
    //   else if ( (L[p]!=0)&&((Sum[i][j][k]>r3)||(Sum[i][j][k]<r4)) ) L[p]=0;
    private int[] rules = new int[4] { 5, 5, 13, 7 };

    // konstruktor
    public ComputingThread(int size)
    {
        this.size = size;

        this.GenerateRandomValues();

        this.DecomposeData();

        this.threads = new Thread[this.threadCount];
        this.readAndComputeReady = new AutoResetEvent[this.threadCount];
        this.writeReady = new AutoResetEvent[this.threadCount];

        for (int i = 0; i < this.threadCount; i++)
        {
            this.readAndComputeReady[i] = new AutoResetEvent(false);
            this.writeReady[i] = new AutoResetEvent(false);

            this.threads[i] = new Thread(this.ComputeGeneration);
            this.threads[i].Start(i);
        }

        new Thread(ComputingMainThread).Start();
    }

    public void ComputingMainThread()
    {
        while (true)
        {
            WaitHandle.WaitAll(this.readAndComputeReady);
            this.startGeneration.Reset();
            this.startWriting.Set();

            WaitHandle.WaitAll(this.writeReady);
            this.startWriting.Reset();

            this.threadComputeReady = true;
            if (this.terminated) break;
        }
        Debug.Log("Main computing thread dies");
    }

    public void ComputeGeneration(object num)
    {
        int index = (int)num;

        while (true)
        {
            // megvárja, míg elkezdheti a számolást
            // a Main objektum jelez neki
            this.startGeneration.WaitOne();

            // olvasási és számolási feladatok

            int offset = this.decomposedValues[index, 0];
            int length = this.decomposedValues[index, 1];

            int[] actualValue = new int[length];
            int[][] indices = new int[length][];

            for (int i = 0; i < length; i++)
            {
                indices[i] = this.Get3DIndex(i + offset, this.size);

                actualValue[i] = this.values[indices[i][0], indices[i][1], indices[i][2]];

                int sum = this.Sum(indices[i][0], indices[i][1], indices[i][2]);

                if (actualValue[i] == 0)
                {
                    if (sum >= this.rules[0] && sum <= this.rules[1]) actualValue[i] = 1;
                }
                else
                {
                    if (sum > this.rules[2] || sum < this.rules[3]) actualValue[i] = 0;
                }
            }

            Debug.Log("TH" + index + " read ready");
            this.readAndComputeReady[index].Set();
            startWriting.WaitOne();

            lock (this.lockObject)
            {
                for (int i = 0; i < length; i++)
                {
                    this.values[indices[i][0], indices[i][1], indices[i][2]] = actualValue[i];
                }
            }

            Debug.Log("TH" + index + " write ready");
            writeReady[index].Set();

            if (this.terminated) break;
        }

        Debug.Log("TH " + index + " dies");
    }

    // legenerálja a values tömb értékeit
    private void GenerateRandomValues()
    {
        this.values = new int[this.size, this.size, this.size];
        
        for (int i = 0; i < this.size; i++)
        {
            for (int j = 0; j < this.size; j++)
            {
                for (int k = 0; k < this.size; k++)
                {
                    this.values[i, j, k] = Random.Range(0, 2);
                }
            }
        }
    }

    // beállítja a decomposedValues offset és counter értékeket
    private void DecomposeData()
    {
        this.decomposedValues = new int[this.threadCount, 2];

        // kiszámoljuk, mennyi adat jut egy thread-re
        // mivel nem feltétlenül osztható a thread-ek számával, az utolsóba tesszük a maradékot
        int total = this.size * this.size * this.size;
        int dataPerThread = total / (this.threadCount - 1);

        for (int i = 0; i < this.threadCount - 1; i++)
        {
            // offset
            this.decomposedValues[i,0] = i * dataPerThread;
            // adatosszúság
            this.decomposedValues[i,1] = dataPerThread;
        }

        // utolsó elem
        this.decomposedValues[this.threadCount - 1, 0] = (this.threadCount - 1) * dataPerThread;
        this.decomposedValues[this.threadCount - 1, 1] = total % this.threadCount;
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

    // egy lineáris indexbõl visszafejti a 3D indexet
    // gyakorlatilag ugyanaz a feladat, mintha size alapú számrendszerben kéne felírni az index számot
    // pl: linear = 859; size = 10 (10x10x10 kocka) -> { 8, 5, 9 }
    private int[] Get3DIndex(int linear, int size)
    {
        int[] ret = new int[3];

        ret[0] = (linear / (size * size)) % size;
        ret[1] = (linear / size) % size;
        ret[2] = linear % size;

        return ret;
    }
}
