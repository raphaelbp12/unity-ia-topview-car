using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BarChartController : MonoBehaviour
{
    public BarController barPrefab;
    public float[] inputValues;

    List<BarController> bars = new List<BarController>();

    float chartHeight;

    // Start is called before the first frame update
    void Start()
    {
        chartHeight = GetComponent<RectTransform>().rect.height;

        DisplayGraph(inputValues);
    }

    // Update is called once per frame
    void DisplayGraph(float[] vals)
    {
        float maxValue = vals.Max();
        for (int i = 0; i < vals.Length; i++)
        {
            BarController newBar = Instantiate(barPrefab) as BarController;
            newBar.transform.SetParent(transform);
            RectTransform rt = newBar.bar.GetComponent<RectTransform>();
            float normalizedValue = (float)vals[i] / maxValue * 0.95f;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, chartHeight * normalizedValue);
        }
    }
}
