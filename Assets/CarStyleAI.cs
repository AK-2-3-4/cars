using UnityEngine;
using TMPro;

public class CarStyleAI : MonoBehaviour
{
    public TMP_InputField themeInput;

    public void ApplyStyle()
    {
        string theme = themeInput.text.ToLower();
        Debug.Log("Theme typed: " + theme);

        Color color = Color.white;

        if (theme.Contains("sport"))
            color = new Color(0.8f, 0.1f, 0.1f);
        else if (theme.Contains("luxury"))
            color = new Color(0.05f, 0.05f, 0.05f);
        else if (theme.Contains("cyberpunk"))
            color = new Color(0.5f, 0f, 0.8f);
        else if (theme.Contains("nature"))
            color = new Color(0.1f, 0.5f, 0.2f);
        else if (theme.Contains("sunset"))
            color = new Color(1f, 0.4f, 0.1f);
        else
            color = Random.ColorHSV();

        GameObject activeCar = null;
        GameObject[] cars = GameObject.FindGameObjectsWithTag("car");

        foreach (GameObject car in cars)
        {
            if (car.activeInHierarchy)
            {
                activeCar = car;
                break;
            }
        }

        if (activeCar == null)
        {
            Debug.LogError("No active car found!");
            return;
        }

        Renderer[] renderers = activeCar.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            Debug.Log("Renderer found on: " + r.gameObject.name);

            foreach (Material m in r.materials)
            {
                Debug.Log("Material: " + m.name);

                if (m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", color);

                if (m.HasProperty("_Color"))
                    m.SetColor("_Color", color);
            }
        }

        Debug.Log("Color applied: " + color);
    }
}