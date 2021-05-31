using Assets.Classes.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace Assets.Classes
{
    public class SaveJson
    {
        public static void Population(PopulationDTO populationDTO, string pathToTheFile)
        {
            string fileName = "pop";
            string path = pathToTheFile + fileName + ".txt";

            try
            {
                File.Exists(path);
            }
            catch (Exception e)
            {
                File.Create(path);
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(populationDTO));
        }

        public static void SaveCar(NeuralNetwork car, string pathToTheFile, InputField carFileName)
        {
            string path = pathToTheFile + "car.txt";

            if (carFileName.text != "")
            {
                path = pathToTheFile + carFileName.text + ".txt";
            }

            if (!File.Exists(path))
            {
                File.Create(path);
            }

            var json = File.ReadAllText(path);
            var carsParsed = JsonConvert.DeserializeObject<List<List<List<float>>>>(json);
            List<List<List<float>>> neuralLayerWeights = new List<List<List<float>>>() { };

            foreach (Layer layer in car.neuralLayers)
            {
                neuralLayerWeights.Add(layer.GetWeights());
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(neuralLayerWeights));
        }
    }
}
