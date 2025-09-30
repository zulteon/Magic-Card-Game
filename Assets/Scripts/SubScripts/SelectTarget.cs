using UnityEngine;
using System.Collections;
public class SelectTarget : MonoBehaviour
{
    

    // Update is called once per frame
    public static SelectTarget instance;
    bool ready = false;
    private void Awake()
    {
        instance = this;
    }
    void Update()
    {
        if (ready)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mousePos, -Vector2.up   );
                print(hit);

                if (hit.collider == null)
                {
                    CancelAttack();
                    return;
                }

                LiveMinion target = hit.transform.GetComponent<LiveMinion>();
                if (target != null )//&& target.validTarget)
                {
                    EndAttack(target);
                }
            }
        }
    }
    public  void Ready(bool b)
    {
        StartCoroutine(PrepReady(b));
    }
    public void CancelAttack()
    {
        print("cancel attack");
        ready = false;
    }
    public void EndAttack(LiveMinion target)
    {
        //  Here the attack method
        GameManager.instance.GetLocalPlayerController().EndAttack(target);
        
    }
    public IEnumerator PrepReady(bool b)
    {
        float t = -0.3f;
        while (t < 0)
        {
            yield return null;
            t += Time.deltaTime;
        }
        ready = b;

    }
}
