using UnityEngine;
using UnityEngine.UI;

public class BackgroundChooser : MonoBehaviour
{
    [SerializeField] private Transform gridLayout;
    [SerializeField] private Slider slider;

    private void Start()
    {
        for (int i = 0; i < gridLayout.childCount; i++)
        {
            int index = i;

            Button button =
                gridLayout.GetChild(i).GetComponent<Button>();

            if (button == null)
                continue;

            button.onClick.AddListener(() =>
            {
                ChangeBackground.instance.Change(
                    index,
                    slider.value
                );
            });
        }

        // Slider mozgatásakor a preview képek fényereje is változik.
        slider.onValueChanged.AddListener(ChangePreviewIMGLight);

        // Kezdõérték beállítása.
        ChangePreviewIMGLight(slider.value);
    }

    public void ChangePreviewIMGLight(float sliderValue)
    {
        foreach (Transform child in gridLayout)
        {
            Image image = child.GetComponent<Image>();

            if (image == null)
                continue;

            image.color = new Color(
                sliderValue,
                sliderValue,
                sliderValue,
                1f
            );
        }
    }
}