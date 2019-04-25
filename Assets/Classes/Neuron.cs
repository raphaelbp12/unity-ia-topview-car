using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Classes
{
    public class Neuron
    {
        public float activationValue;
        public List<float> weights;
        private List<Neuron> neuronsPreviousLayer;

        public Neuron (float? input, List<Neuron> previousNeurons, List<float> receivedWeights)
        {
            neuronsPreviousLayer = previousNeurons;
            generateWeights(neuronsPreviousLayer.Count, receivedWeights);
            calcActivationValue(input, neuronsPreviousLayer);
        }

        public List<float> generateWeights(int weightsLength, List<float> receivedWeights)
        {
            if (receivedWeights.Count > 0)
            {
                weights = receivedWeights;
                return weights;
            } else
            {
                weights = new List<float>();
                for(int i = 0; i < neuronsPreviousLayer.Count; i++)
                {
                    float random = UnityEngine.Random.Range(-1.0f, 1.0f);
                    float randomNormalized = random / neuronsPreviousLayer.Count;
                    weights.Add(randomNormalized);
                }
                return weights;
            }
        }

        public float calcActivationValue(float? input, List<Neuron> previousNeurons)
        {
            if (input != null)
            {
                activationValue = input.Value;
                return input.Value;
            } else
            {
                float value = 0;

                for(int i = 0; i < neuronsPreviousLayer.Count; i++)
                {
                    value = value + neuronsPreviousLayer[i].activationValue * weights[i];
                }

                activationValue = value;
                return value;
            }
        }
    }
}
