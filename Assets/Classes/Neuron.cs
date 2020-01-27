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
        public List<Neuron> neuronsPreviousLayer;

        public Neuron (float? input, List<Neuron> previousNeurons, List<float> receivedWeights)
        {
            neuronsPreviousLayer = previousNeurons;
            generateWeights(neuronsPreviousLayer.Count, receivedWeights);
            calcActivationValue(input, neuronsPreviousLayer);
        }

        public Neuron DeepCopy()
        {
            Neuron other = (Neuron)this.MemberwiseClone();
            other.weights = new List<float>();
            foreach (float weight in weights)
            {
                other.weights.Add(weight);
            }

            other.neuronsPreviousLayer = new List<Neuron>();
            if(neuronsPreviousLayer.Count > 0)
            {
                foreach(Neuron neuron in neuronsPreviousLayer)
                {
                    other.neuronsPreviousLayer.Add(neuron.DeepCopy());
                }
            }
            return other;
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
                    weights.Add(random);
                }
                return weights;
            }
        }

        public static float Sigmoid(double value) {
            return 1.0f / (1.0f + (float) Math.Exp(-value));
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

                activationValue = (float)Sigmoid(value) - 0.5f;
                return activationValue;
            }
        }

        public string GetWeightsToDebug()
        {
            string retorno = "";

            foreach (float weight in weights)
            {
                retorno += ", " + weight;
            }

            return retorno;
        }
    }
}
