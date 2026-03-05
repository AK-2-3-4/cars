using TMPro;
using UnityEngine;

public class CarSwitcher : MonoBehaviour
{
    public GameObject[] cars;
    [Tooltip("Optional friendly names or categories for each car.")]
    public string[] carLabels;
    [Tooltip("Optional UI text to show current car name/category.")]
    public TextMeshProUGUI carLabelText;

    int currentCar = 0;

    void Start()
    {
        ShowCar(0);
    }

    public void NextCar()
    {
        currentCar++;

        if (currentCar >= cars.Length)
            currentCar = 0;

        ShowCar(currentCar);
    }

    void ShowCar(int index)
    {
        for (int i = 0; i < cars.Length; i++)
        {
            cars[i].SetActive(i == index);
        }

        if (carLabelText != null && cars.Length > 0)
        {
            string label = null;

            if (carLabels != null && index < carLabels.Length)
                label = carLabels[index];

            if (string.IsNullOrEmpty(label))
                label = cars[index].name;

            carLabelText.text = label;
        }
    }
}