using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Classes
{
    public class Layer
    {
        public List<Neuron> neurons = new List<Neuron>();

        public Layer(List<float> inputs, int neuronsCount, List<Neuron> previousNeurons, List<List<float>> receivedWeights)
        {
            if (inputs.Count > 0)
            {
                GetInputs(inputs);
            } else
            {
                GenerateLayer(neuronsCount, previousNeurons, receivedWeights);
            }
        }

        public void ResetLayer()
        {
            foreach(Neuron neuron in neurons)
            {
                neuron.activationValue = 0;
            }
        }

        private void GetInputs(List<float> inputs)
        {
            for(int i = 0; i < inputs.Count; i++)
            {
                neurons.Add(new Neuron(inputs[i], new List<Neuron>(), new List<float>()));
            }
        }

        private void GenerateLayer(int neuronsCount, List<Neuron> previousNeurons, List<List<float>> receivedWeights)
        {
            for(int i = 0; i < neuronsCount; i++)
            {
                if (receivedWeights.Count > 0)
                {
                    neurons.Add(new Neuron(null, previousNeurons, receivedWeights[i]));
                } else
                {
                    neurons.Add(new Neuron(null, previousNeurons, new List<float>()));
                }
            }
        }

        public List<List<float>> GetWeights()
        {
            List<List<float>> weights = new List<List<float>>();

            foreach (Neuron neuron in neurons)
            {
                weights.Add(neuron.weights);
            }

            return weights;
        }

        public string GetWeightsToDebug()
        {
            List<float> weights = new List<float>();

            foreach (Neuron neuron in neurons)
            {
                weights.AddRange(neuron.weights);
            }

            string retorno = "";
            
            foreach(float weight in weights)
            {
                retorno += ", " + weight;
            }

            return retorno;
        }

        public List<float> GetOutputs()
        {
            List<float> outputs = new List<float>();

            foreach (Neuron neuron in neurons)
            {
                outputs.Add(neuron.activationValue);
            }

            return outputs;
        }
    }
}
