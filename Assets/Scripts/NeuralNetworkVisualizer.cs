using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Classes;

public class NeuralNetworkVisualizer : MonoBehaviour
{
    public GameObject neuronPrefab;
    public GameObject linePrefab;
    public RectTransform networkContainer;  // Change Transform to RectTransform
    public float neuronSpacing = 0.1f;  // Fraction of parent height
    public float layerSpacing = 0.2f;  // Fraction of parent width
    private NeuralNetwork car;

    public void DrawNetwork(NeuralNetwork network)
    {
        ClearNetwork();

        if (network == null || network.neuralLayers == null) return;

        Vector2 parentSize = networkContainer.rect.size;

        // Loop over layers
        for (int layerIndex = 0; layerIndex < network.neuralLayers.Count; layerIndex++)
        {
            Layer layer = network.neuralLayers[layerIndex];

            // Loop over neurons
            for (int neuronIndex = 0; neuronIndex < layer.neurons.Count; neuronIndex++)
            {
                Neuron neuron = layer.neurons[neuronIndex];
                Vector2 position = new Vector2(layerIndex * layerSpacing * parentSize.x, neuronIndex * neuronSpacing * parentSize.y);
                GameObject neuronObject = Instantiate(neuronPrefab, networkContainer);
                neuronObject.GetComponent<RectTransform>().anchoredPosition = position;
                neuronObject.GetComponentInChildren<Text>().text = neuron.activationValue.ToString("F2");

                // Draw connections to the next layer, if it exists
                if (layerIndex < network.neuralLayers.Count - 1)
                {
                    Layer nextLayer = network.neuralLayers[layerIndex + 1];
                    for (int nextNeuronIndex = 0; nextNeuronIndex < nextLayer.neurons.Count; nextNeuronIndex++)
                    {
                        Neuron nextNeuron = nextLayer.neurons[nextNeuronIndex];
                        Vector2 nextPosition = new Vector2((layerIndex + 1) * layerSpacing * parentSize.x, nextNeuronIndex * neuronSpacing * parentSize.y);

                        GameObject lineObject = Instantiate(linePrefab, networkContainer);
                        RectTransform lineRectTransform = lineObject.GetComponent<RectTransform>();

                        // Calculate direction and position
                        Vector2 direction = nextPosition - position;
                        lineRectTransform.sizeDelta = new Vector2(direction.magnitude, 1);
                        lineRectTransform.pivot = new Vector2(0, 0.5f);
                        lineRectTransform.anchoredPosition = position;

                        // Calculate rotation
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        lineRectTransform.rotation = Quaternion.Euler(0, 0, angle);

                        var lineRenderer = lineObject.GetComponent<Image>();
                        lineRenderer.color = Color.blue;

                        // Set line thickness based on weight (you could also set color)
                        if (neuronIndex < nextNeuron.weights.Count)
                        {
                            float weight = nextNeuron.weights[neuronIndex];

                            if (weight < 0) lineRenderer.color = Color.red;

                            lineRectTransform.localScale = new Vector3(1, Mathf.Abs(weight) * 6, 1);
                        }
                        else
                        {
                            lineRectTransform.localScale = new Vector3(1, 0, 1);
                        }
                    }
                }
            }
        }
    }

    public void ClearNetwork()
    {
        foreach (Transform child in networkContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void SetCar(NeuralNetwork newCar)
    {
        car = newCar;
    }

    void Update()
    {
        if (car != null)
        {
            DrawNetwork(car);
        }
    }
}
