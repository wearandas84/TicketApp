using System.ComponentModel.DataAnnotations;

namespace TicketApp.Models;

public class Ticket
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "El asunto es requerido")]
    public string Asunto { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La descripción es requerida")]
    public string Descripcion { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombre { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
    public string Correo { get; set; } = string.Empty;
    
    public string Telefono { get; set; } = string.Empty;
    
    public string Estatus { get; set; } = "Abierto";
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public string? ResueltoPor { get; set; }
    
    public string Folio => $"TK-{Id:D4}";
}

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; 
}

public class ArchivoAdjunto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string RutaAlmacenamiento { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public bool EsEvidenciaSoporte { get; set; } = false; 
}

public class ComentarioTicket
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string Autor { get; set; } = "Soporte"; // Si tuvieras sesión completa, aquí iría el Username
    public DateTime Fecha { get; set; } = DateTime.Now;
}