using UnityEngine;
using UnityEngine.UI;

public class MinionBorderChooser : MonoBehaviour
{
    private void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            int index = i;

            Button button =
                transform.GetChild(i).GetComponent<Button>();

            if (button == null)
                continue;

            button.onClick.AddListener(
                () => SelectBorder(index)
            );
        }
    }

    private void SelectBorder(int borderIndex)
    {
       
       

        // A már létezõ minionokat is átállítjuk.
        MinionView[] minions =
            FindObjectsByType<MinionView>(
                FindObjectsSortMode.None
            );

        foreach (MinionView minion in minions)
        {
            minion.ChangeBorder(borderIndex);
        }
    }
}