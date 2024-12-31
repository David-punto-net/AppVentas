﻿using System.ComponentModel.DataAnnotations;

namespace AppVentas.Models.Entidades
{
    public class Categoria
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe ingresar el {0}")]
        [StringLength(50, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres")]
        [Display(Name = "Categoría")]
        public string Nombre { get; set; }

        public ICollection<ProductCategory> ProductCategories { get; set; }
    }
}