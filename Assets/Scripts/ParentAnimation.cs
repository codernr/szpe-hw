using UnityEngine;
using System.Collections;

public class ParentAnimation : MonoBehaviour {

    private Main main;
    // mennyivel növekszik
    public float growth = 0.1f;

    public void Initialize(Main _main)
    {
        this.main = _main;
        //this.main.starterEvent += new Main.StartAnimation(this.StartAnimation);
    }

    public void StartAnimation()
    {
        this.StartCoroutine(this.Animation());
    }

    private IEnumerator Animation()
    {
        if (this.main.size > 10) yield break;

        float dur = this.main.animationDuration / 2f;
        float time = 0;

        while (time < dur)
        {
            float s = (time / dur) * this.growth;
            this.transform.localScale = new Vector3(1 + s, 1 + s, 1 + s);

            time += Time.deltaTime;
            yield return null;
        }

        time = 0;
        while (time < dur)
        {
            float s = (1 - (time / dur)) * this.growth;
            this.transform.localScale = new Vector3(1 + s, 1 + s, 1 + s);
            time += Time.deltaTime;
            yield return null;
        }

        this.transform.localScale = new Vector3(1, 1, 1);
    }
}
