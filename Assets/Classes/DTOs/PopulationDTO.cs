using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Classes.DTOs
{
    public class PopulationDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<CarDTO> Cars = new List<CarDTO>();
    }
}
